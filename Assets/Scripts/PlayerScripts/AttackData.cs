using UnityEngine;

public enum AttackPhase
{
    Startup,
    Active,
    Recovery
}

[CreateAssetMenu(fileName = "NewAttack", menuName = "Fighting Game/Attack Data")]
public class AttackData : ScriptableObject
{
    [Header("Frame Data")]
    [SerializeField] private int startupFrames;
    [SerializeField] private int activeFrames;
    [SerializeField] private int recoveryFrames;

    [Header("Damage")]
    [Tooltip("How much health this attack takes off when it hits")]
    [Min(0)] [SerializeField] private int damage;
    [Tooltip("Goes straight through a block and hits anyway")]
    [SerializeField] private bool breaksBlock;

    [Header("Audio")]
    [Tooltip("Plays when this attack hits. A miss plays the SFXManager's whiff sound instead")]
    [SerializeField] private SFXGroup hitSFX = new SFXGroup();

    [Header("Animation")]
    [Tooltip("Name of the state in the Animator. Leave empty if this attack has no animation")]
    [SerializeField] private string animationState;
    [Tooltip("How far through the clip the strike connects. Lined up with the first active frame")]
    [Range(0f, 1f)] [SerializeField] private float animationStrikePoint = 0.5f;

    [Header("Hitbox")]
    public Vector2 HitboxOffset;
    public Vector2 HitboxSize;

    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
    public int Damage => damage;
    public bool BreaksBlock => breaksBlock;
    public SFXGroup HitSFX => hitSFX;
    public string AnimationState => animationState;

    // Startup plays the clip up to the strike point and active + recovery plays the rest,
    // so the strike connects on the first active frame whatever the frame data is set to
    public float GetAnimationTime(int frame)
    {
        if (frame < startupFrames)
            return Mathf.Lerp(0f, animationStrikePoint, (float)frame / startupFrames);

        return Mathf.Lerp(animationStrikePoint, 1f, (float)(frame - startupFrames) / (activeFrames + recoveryFrames));
    }

    public AttackPhase GetPhase(int frame)
    {
        AttackPhase currAttackPhase;

        if (frame < startupFrames) currAttackPhase = AttackPhase.Startup;
        else if (frame < startupFrames + activeFrames) currAttackPhase = AttackPhase.Active;
        else currAttackPhase = AttackPhase.Recovery;

        return currAttackPhase;
    }
}
