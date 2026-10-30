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

    private bool _button1Pending;
    private bool _button2Pending;
    private bool _button3Pending;

    private bool _hasHit;

    private AttackData _currAttack;
    private int _currFrame;
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
    }

    public void Tick()
    {
        bool button1 = _button1Pending;
        bool button2 = _button2Pending;
        bool button3 = _button3Pending;
        _button1Pending = _button2Pending = _button3Pending = false; // Apparently you can just do this and sets all the bools to false, crazy stuff

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
            }
        }

        if (_currAttack == null)
        {
            if (button3) StartAttack(_attack3Data);
            else if (button2) StartAttack(_attack2Data);
            else if (button1) StartAttack(_attack1Data);
        }
    }

    private void StartAttack(AttackData attack)
    {
        _hasHit = false;
        _currAttack = attack;
        _currFrame = 0;
        _lastPhase = AttackPhase.Startup;
        Debug.Log(attack.name + ": Started, currFrame " + _currFrame);
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
