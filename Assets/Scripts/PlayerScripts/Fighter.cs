using UnityEngine;
using UnityEngine.InputSystem;

public class Fighter : MonoBehaviour
{
    public PlayerAttack PlayerAttack;
    public PlayerMovement PlayerMovement;

    [Header("--- Hurtbox ---")]
    [SerializeField] private Vector2 hurtboxOffset;
    [SerializeField] private Vector2 hurtboxSize;

    public int Facing { get; set; } = 1;

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
    [SerializeField] private Renderer[] hatRenderers;

    private PlayerAttack _playerAttack;
    private PlayerMovement _playerMovement;
    private PlayerInput _playerInput;

    public PlayerHealth Health { get; private set; }
    public int PlayerIndex => _playerInput.playerIndex;

    private void Awake()
    {
        _playerAttack = GetComponent<PlayerAttack>();
        _playerMovement = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerInput>();
        Health = GetComponent<PlayerHealth>();
    }

    public void Tick()
    {
        PlayerAttack.Tick();
        PlayerMovement.Tick();
    }

    public void DrawRect(Rect rect)
    {
        Vector3 center = new Vector3(rect.center.x, rect.center.y, transform.position.z);
        Vector3 size = new Vector3(rect.width, rect.height, 0f);
        Gizmos.DrawWireCube(center, size);
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.green;
        DrawRect(Hurtbox);

        if (PlayerAttack.TryGetHitbox(out Rect hitbox))
        {
            Gizmos.color = Color.red;
            DrawRect(hitbox);
        }
    }

    public void TryHit(Fighter target)
    {
        if (_playerAttack.TryHit(target.transform.position, out int damage))
            target.Health.TakeDamage(damage);
    }

    public void SetInputEnabled(bool inputEnabled)
    {
        if (inputEnabled) _playerInput.ActivateInput();
        else _playerInput.DeactivateInput();
    }

    public void SetHatColor(Color color)
    {
        foreach (Renderer hatRenderer in hatRenderers)
            hatRenderer.material.color = color;
    }
}
