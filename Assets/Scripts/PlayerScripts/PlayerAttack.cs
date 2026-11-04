using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private const int InputBufferTicks = 8;   // How long an early press is remembered

    private PlayerInput _thisPlayerInput;

    private InputAction _button1Action;
    private InputAction _button2Action;
    private InputAction _button3Action;

    [SerializeField] private AttackData _attack1Data;
    [SerializeField] private AttackData _attack2Data;
    [SerializeField] private AttackData _attack3Data;

    [Tooltip("Trail on the fist that throws the first attack")]
    [SerializeField] private TrailRenderer _attack1Trail;

    private bool _button1Pending;
    private bool _button2Pending;
    private bool _button3Pending;

    private AttackData _bufferedAttack;
    private int _bufferTicksRemaining;

    private bool _hasHit;

    private AttackData _currAttack;
    private TrailRenderer _currTrail;
    private int _currFrame;
    private AttackPhase _lastPhase; // for logging

    private Fighter _fighter;
    private Animator _animator;

    public AttackData CurrentAttack => _currAttack;

    private void Awake()
    {
        _thisPlayerInput = GetComponent<PlayerInput>();

        _button1Action = _thisPlayerInput.actions[InputManager.button1];
        _button2Action = _thisPlayerInput.actions[InputManager.button2];
        _button3Action = _thisPlayerInput.actions[InputManager.button3];

        _fighter = GetComponent<Fighter>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (_button1Action.WasPressedThisFrame()) _button1Pending = true;
        if (_button2Action.WasPressedThisFrame()) _button2Pending = true;
        if (_button3Action.WasPressedThisFrame()) _button3Pending = true;
    }

    public void Tick(bool canAct)
    {
        bool button1 = _button1Pending;
        bool button2 = _button2Pending;
        bool button3 = _button3Pending;
        _button1Pending = _button2Pending = _button3Pending = false;

        if (_currAttack != null)
        {
            _currFrame++;
            if (_currFrame >= _currAttack.TotalFrames)
            {
                Debug.Log(_currAttack.name + ": Finished currFrame " + _currFrame);
                ReturnToIdle();
                _currAttack = null;
            }
            else
            {
                AttackPhase phase = _currAttack.GetPhase(_currFrame);
                if (phase != _lastPhase)
                {
                    Debug.Log(_currAttack.name + ": " + phase + ", currFrame " + _currFrame);
                    _lastPhase = phase;

                    // Active frames are over, so this is the first moment we know the attack missed
                    if (phase == AttackPhase.Recovery && !_hasHit && SFXManager.Instance != null)
                        SFXManager.Instance.PlayAttackWhiff();

                    if (phase == AttackPhase.Active && _currAttack.PlaysStrikeArc && HitEffects.Instance != null)
                        HitEffects.Instance.PlayStrikeArc(_fighter.PlayerIndex, transform.position, _fighter.Facing);
                }
            }
        }

        AttackData pressed = GetPressedAttack(button1, button2, button3);

        // A press is remembered for a few ticks, so it still comes out if it was slightly early
        if (pressed != null)
        {
            _bufferedAttack = pressed;
            _bufferTicksRemaining = InputBufferTicks;
        }
        else if (_bufferTicksRemaining > 0)
        {
            _bufferTicksRemaining--;
            if (_bufferTicksRemaining == 0) _bufferedAttack = null;
        }

        // Presses during stun are thrown away, not saved for after it
        if (!canAct) ClearBuffer();

        AttackData wanted = _bufferedAttack;

        // Cancel: an attack that connected (hit or block) can be cut short by a higher tier one during its window
        if (_currAttack != null && canAct && _hasHit && wanted != null
            && _currAttack.IsInCancelWindow(_currFrame)
            && wanted.CancelTier > _currAttack.CancelTier)
        {
            Debug.Log(_currAttack.name + ": Cancelled into " + wanted.name + " on currFrame " + _currFrame);
            StopTrail();
            if (string.IsNullOrEmpty(wanted.AnimationState)) ReturnToIdle();
            StartAttack(wanted);
        }

        if (_currAttack == null && canAct && wanted != null)
            StartAttack(wanted);

        UpdateAnimation();
        UpdateTrail();
    }

    // Heaviest pressed button wins when more than one lands on the same tick
    private AttackData GetPressedAttack(bool button1, bool button2, bool button3)
    {
        if (button3) return _attack3Data;
        if (button2) return _attack2Data;
        if (button1) return _attack1Data;
        return null;
    }

    private void ClearBuffer()
    {
        _bufferedAttack = null;
        _bufferTicksRemaining = 0;
    }

    private void ReturnToIdle()
    {
        if (!string.IsNullOrEmpty(_currAttack.AnimationState))
            _animator.Play("Idle");
    }

    private void StopTrail()
    {
        if (_currTrail == null) return;

        _currTrail.emitting = false;
        _currTrail = null;
    }

    // Trails run through the swing and cut off when recovery starts, then fade out on their own
    private void UpdateTrail()
    {
        if (_currTrail == null) return;

        _currTrail.emitting = _currAttack != null && _currAttack.GetPhase(_currFrame) != AttackPhase.Recovery;
        if (!_currTrail.emitting) _currTrail = null;
    }

    public void SetTrailColor(Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.Lerp(color, Color.white, 0.3f), 0f), new GradientColorKey(color, 0.5f) },
            new[] { new GradientAlphaKey(0.85f, 0f), new GradientAlphaKey(0f, 1f) });

        _attack1Trail.colorGradient = gradient;
    }

    // Attack states don't play on their own, every tick sets exactly where in the clip they should be
    private void UpdateAnimation()
    {
        if (_currAttack == null || string.IsNullOrEmpty(_currAttack.AnimationState)) return;

        _animator.Play(_currAttack.AnimationState, 0, _currAttack.GetAnimationTime(_currFrame));
    }

    private void StartAttack(AttackData attack)
    {
        ClearBuffer();   // The press that started this attack is used up

        _hasHit = false;
        _currAttack = attack;
        _currFrame = 0;
        _lastPhase = AttackPhase.Startup;
        Debug.Log(attack.name + ": Started, currFrame " + _currFrame);

        _currTrail = attack == _attack1Data ? _attack1Trail : null;
    }

    // An attack can only hit once, on any of its active frames
    public bool TryGetHitbox(out Rect box)
    {
        box = default;
        return !_hasHit && TryGetActiveHitbox(out box);
    }

    // Stays true for every active frame, so the gizmo still shows after the attack has hit
    public bool TryGetActiveHitbox(out Rect box)
    {
        if (_currAttack != null && _currAttack.GetPhase(_currFrame) == AttackPhase.Active)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(_currAttack.HitboxOffset.x * _fighter.Facing, _currAttack.HitboxOffset.y);
            box = new Rect(Vector2.zero, _currAttack.HitboxSize);
            box.center = center;
            return true;
        }

        box = default;
        return false;
    }

    public void MarkHit()
    {
        _hasHit = true;
    }

    public void CancelAttack()
    {
        if (_currAttack == null) return;

        StopTrail();
        ReturnToIdle();
        _currAttack = null;
    }

    // Drops any presses waiting to be used, so nothing fires when input comes back on
    public void ClearPendingInputs()
    {
        _button1Pending = _button2Pending = _button3Pending = false;
        ClearBuffer();
    }
}