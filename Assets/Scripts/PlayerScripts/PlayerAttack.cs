using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttack : MonoBehaviour
{
    private PlayerInput _thisPlayerInput;

    private InputAction _button1Action;
    private InputAction _button2Action;
    private InputAction _button3Action;

    [SerializeField] private AttackData _attack1Data;
    [SerializeField] private AttackData _attack2Data;
    [SerializeField] private AttackData _attack3Data;
    [SerializeField] private AttackData _debugAttackData;

    private bool _button1Pending;
    private bool _button2Pending;
    private bool _button3Pending;
    private bool _debugAttackPending;

    private bool _hasHit;

    private AttackData _currAttack;
    private int _currFrame;
    private bool _currAttackHit;
    private AttackPhase _lastPhase; // for logging

    private Fighter _fighter;

    public AttackData CurrentAttack => _currAttack;

    private void Awake()
    {
        _thisPlayerInput = GetComponent<PlayerInput>();

        _button1Action = _thisPlayerInput.actions[InputManager.button1];
        _button2Action = _thisPlayerInput.actions[InputManager.button2];
        _button3Action = _thisPlayerInput.actions[InputManager.button3];

        _currAttack = null;

        _fighter = GetComponent<Fighter>();
    }

    private void Update()
    {
        if (_button1Action.WasPressedThisFrame()) _button1Pending = true;
        if (_button2Action.WasPressedThisFrame()) _button2Pending = true;
        if (_button3Action.WasPressedThisFrame()) _button3Pending = true;

        // Read straight from the keyboard, so it has to check for itself whether input is on
        Key debugKey = InputManager.debugAttackKeys[_thisPlayerInput.playerIndex];
        if (_thisPlayerInput.inputIsActive && Keyboard.current != null && Keyboard.current[debugKey].wasPressedThisFrame) _debugAttackPending = true;
    }

    public void Tick()
    {
        bool button1 = _button1Pending;
        bool button2 = _button2Pending;
        bool button3 = _button3Pending;
        bool debugAttack = _debugAttackPending;
        _button1Pending = _button2Pending = _button3Pending = _debugAttackPending = false; // Apparently you can just do this and sets all the bools to false, crazy stuff

        if (_currAttack != null)
        {
            _currFrame++;
            if (_currFrame >= _currAttack.TotalFrames)
            {
                Debug.Log(_currAttack.name + ": Finished currFrame " + _currFrame);
                _currAttack = null;
            }
            else
            {
                AttackPhase phase = _currAttack.GetPhase(_currFrame);
                if (phase != _lastPhase)
                {
                    Debug.Log(_currAttack.name + ": " + phase + ", currFrame " + _currFrame);
                    _lastPhase = phase;
                }

                if (SFXManager.Instance != null)
                    SFXManager.Instance.OnAttackFrame(_currAttack, _currFrame);
            }
        }

        if (_currAttack == null)
        {
            if (debugAttack) StartAttack(_debugAttackData);
            else if (button3) StartAttack(_attack3Data);
            else if (button2) StartAttack(_attack2Data);
            else if (button1) StartAttack(_attack1Data);
        }
    }

    // An attack can only hit once, on any of its active frames
    public bool TryHit(Vector3 targetPosition, out int damage)
    {
        damage = 0;

        if (_currAttack == null || _currAttackHit || _currAttack.GetPhase(_currFrame) != AttackPhase.Active) return false;
        if (Mathf.Abs(targetPosition.x - transform.position.x) > _currAttack.Range) return false;

        _currAttackHit = true;
        damage = _currAttack.Damage;
        Debug.Log(_currAttack.name + ": Hit for " + damage + ", currFrame " + _currFrame);
        return true;
    }

    private void StartAttack(AttackData attack)
    {
        _hasHit = false;
        _currAttack = attack;
        _currFrame = 0;
        _currAttackHit = false;
        _lastPhase = AttackPhase.Startup;
        Debug.Log(attack.name + ": Started, currFrame " + _currFrame);

        if (SFXManager.Instance != null)
            SFXManager.Instance.OnAttackFrame(attack, _currFrame);
    }

    public bool TryGetHitbox(out Rect box)
    {
        if (_currAttack != null && _currAttack.GetPhase(_currFrame) == AttackPhase.Active && !_hasHit)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(_currAttack.HitboxOffset.x * _fighter.Facing, _currAttack.HitboxOffset.y);
            box = new Rect(Vector2.zero, _currAttack.HitboxSize);
            box.center = center;

            return true;
        }
        else
        {
            box = default;
            return false;
        }
    } 

    public void MarkHit()
    {
        _hasHit = true;
    }
}
