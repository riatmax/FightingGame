using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("--- Hurtbox ---")]
    [SerializeField] private Vector2 hurtboxOffset;
    [SerializeField] private Vector2 hurtboxSize;

    private PlayerInput _playerInput;
    private Animator _animator;
    private int _facing = 1;
    private Color _bodyColor;

    public PlayerHealth Health { get; private set; }
    public int PlayerIndex => _playerInput.playerIndex;
    public bool IsBlocking => PlayerMovement.IsWalkingBackward(_facing);

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
        PlayerAttack.Tick();
        PlayerMovement.Tick();
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
        _animator.speed = frozen ? 0f : 1f;
        PlayerMovement.SetFrozen(frozen);
    }
}
