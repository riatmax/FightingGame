using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum FighterState
{
    Free,
    Attacking,
    Hitstun,
    Blockstun,
    Dashing
}
public class Fighter : MonoBehaviour
{
    public PlayerAttack PlayerAttack;
    public PlayerMovement PlayerMovement;

    [SerializeField] private Renderer[] bodyRenderers;
    [Tooltip("Seconds the body flashes white when hit")]
    [SerializeField] private float hitFlashDuration = 0.08f;

    [Header("--- Model ---")]
    [Tooltip("The character model, gets turned around whenever Facing changes")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Y rotation that makes the model look to the right")]
    [SerializeField] private float facingRightYaw = 90f;
    [Tooltip("Animator state played while in the air. Attacks take over from it")]
    [SerializeField] private string jumpAnimationState = "Chibi_Rig_Jump";

    [Header("--- Hurtbox ---")]
    [SerializeField] private Vector2 hurtboxOffset;
    [SerializeField] private Vector2 hurtboxSize;

    [Header("--- Pushbox (simulated collision) ---")]
    [SerializeField] private float pushboxHalfWidth = 0.4f;
    [Tooltip("Fighters further apart than this vertically don't push each other, so a jump can go over")]
    [SerializeField] private float pushboxHeight = 1.8f;

    private PlayerInput _playerInput;
    private Animator _animator;
    private int _facing = 1;
    private Color _bodyColor;
    private int _hitstunRemaining;
    private int _blockstunRemaining;
    private float _pushbackPerTick;
    private int _pushbackTicksRemaining;

    private const int PUSHBACK_TICKS = 6;

    public PlayerHealth Health { get; private set; }
    public int PlayerIndex => _playerInput.playerIndex;
    public float PushboxHalfWidth => pushboxHalfWidth;
    public float PushboxHeight => pushboxHeight;

    public bool IsBlocking
    {
        get
        {
            FighterState currState = CurrFighterState;

            if (currState == FighterState.Blockstun) return true;
            if (currState == FighterState.Free) return PlayerMovement.IsWalkingBackward(_facing);
            else return false;
        }
    }

    public FighterState CurrFighterState
    {
        get
        {
            if (_hitstunRemaining > 0) return FighterState.Hitstun;
            if (_blockstunRemaining > 0) return FighterState.Blockstun;
            if (PlayerAttack.CurrentAttack != null) return FighterState.Attacking;
            if (PlayerMovement.IsDashing) return FighterState.Dashing;
            else return FighterState.Free;
        }
    }

    public int Facing
    {
        get => _facing;
        set
        {
            _facing = value;
            visualRoot.localRotation = Quaternion.Euler(0f, _facing > 0 ? facingRightYaw : facingRightYaw + 180f, 0f);
        }
    }

    public Rect Hurtbox
    {
        get
        {
            Vector2 center = (Vector2)transform.position + hurtboxOffset;
            Rect box = new Rect(Vector2.zero, hurtboxSize);
            box.center = center;   
            return box;
        }
    }

    private void Awake()
    {
        PlayerAttack = GetComponent<PlayerAttack>();
        PlayerMovement = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerInput>();
        _animator = GetComponentInChildren<Animator>();
        Health = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        _animator.SetBool("WalkingBack", IsBlocking);
        _animator.SetBool("WalkingForward", PlayerMovement.IsWalkingForward(_facing));
    }

    public void Tick()
    {
        bool stunned = _hitstunRemaining > 0 || _blockstunRemaining > 0;

        if (_hitstunRemaining > 0) _hitstunRemaining--;
        if (_blockstunRemaining > 0) _blockstunRemaining--;

        PlayerAttack.Tick(!stunned && !PlayerMovement.IsDashing);

        bool canMove = !stunned && PlayerAttack.CurrentAttack == null;
        bool wasGrounded = PlayerMovement.IsGrounded;

        PlayerMovement.Tick(canMove);

        UpdateJumpAnimation(wasGrounded);
    }

    // Runs after the attack, so an attack always wins. Once an air attack ends the jump picks back up where the arc is
    private void UpdateJumpAnimation(bool wasGrounded)
    {
        if (PlayerAttack.CurrentAttack != null) return;

        if (!PlayerMovement.IsGrounded)
            _animator.Play(jumpAnimationState, 0, PlayerMovement.JumpProgress);
        else if (!wasGrounded)
            _animator.Play("Idle");
    }

    // Depth is only for the gizmo, hits are still checked in 2D. It has to be wider than the
    // character's body or the box ends up hidden inside it
    public void DrawBox(Rect rect, float depth)
    {
        Vector3 center = new Vector3(rect.center.x, rect.center.y, transform.position.z);
        Vector3 size = new Vector3(rect.width, rect.height, depth);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.25f);
        Gizmos.DrawCube(center, size);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        DrawBox(Hurtbox, 1.2f);

        if (PlayerAttack.TryGetActiveHitbox(out Rect hitbox))
        {
            Gizmos.color = Color.red;
            DrawBox(hitbox, 1.3f);
        }
    }

    public void SetInputEnabled(bool inputEnabled)
    {
        PlayerAttack.ClearPendingInputs();
        if (inputEnabled) _playerInput.ActivateInput();
        else _playerInput.DeactivateInput();
    }

    public void SetBodyColor(Color color)
    {
        _bodyColor = color;
        ApplyBodyColor(color);
    }

    public void FlashBody()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        ApplyBodyColor(Color.white);
        yield return new WaitForSecondsRealtime(hitFlashDuration);
        ApplyBodyColor(_bodyColor);
    }

    private void ApplyBodyColor(Color color)
    {
        foreach (Renderer bodyRenderer in bodyRenderers)
            bodyRenderer.material.color = color;
    }

    // Stops the model and movement for hitstop. Attacks freeze on their own because they run on ticks
    public void SetFrozen(bool frozen)
    {
        /*_animator.speed = frozen ? 0f : 1f;
        PlayerMovement.SetFrozen(frozen);*/
    }


    public void ReceiveHit(AttackData attack, bool blocked)
    {
        if (blocked)
        {
            _blockstunRemaining = attack.BlockstunFrames;
            return;
        }
        PlayerMovement.CancelDash();
        PlayerAttack.CancelAttack();
        _blockstunRemaining = 0;
        _hitstunRemaining = attack.HitstunFrames;
    }

    public void StartPushback(float distance)
    {
        _pushbackPerTick = distance / PUSHBACK_TICKS;
        _pushbackTicksRemaining = PUSHBACK_TICKS;
    }

    public float ConsumePushbackStep()
    {
        if (_pushbackTicksRemaining <= 0) return 0;

        _pushbackTicksRemaining--;
        return _pushbackPerTick;
    }
}
