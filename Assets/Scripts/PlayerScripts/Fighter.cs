using UnityEngine;

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
}
