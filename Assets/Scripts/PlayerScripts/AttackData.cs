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
    [SerializeField] private int hitstunFrames;
    [SerializeField] private int blockstunFrames;
    [SerializeField] private float pushbackOnHit;
    [SerializeField] private float pushbackOnBlock;

    [Header("Damage")]
    [Tooltip("How much health this attack takes off when it hits")]
    [Min(0)] [SerializeField] private int damage;
    [Tooltip("Goes straight through a block and hits anyway")]
    [SerializeField] private bool breaksBlock;

    [Header("Hit Feel")]
    [Tooltip("Plays where the attack connects")]
    [SerializeField] private GameObject hitVFX;
    [SerializeField] private float hitVFXScale = 0.5f;
    [Tooltip("How hard the camera shakes when this attack hits")]
    [SerializeField] private float shakeForce = 0.08f;
    [Tooltip("Game frames the two fighters freeze for when this attack hits")]
    [Min(0)] [SerializeField] private int hitstopFrames = 3;
    [Tooltip("Real seconds the whole game freezes for when this attack hits - VFX, camera, everything")]
    [Min(0f)] [SerializeField] private float freezeTime;
    [Tooltip("Plays the attacker's strike arc swoosh as the active frames start")]
    [SerializeField] private bool playsStrikeArc;

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
    public GameObject HitVFX => hitVFX;
    public float HitVFXScale => hitVFXScale;
    public float ShakeForce => shakeForce;
    public int HitstopFrames => hitstopFrames;
    public float FreezeTime => freezeTime;
    public bool PlaysStrikeArc => playsStrikeArc;
    public SFXGroup HitSFX => hitSFX;
    public string AnimationState => animationState;
    public int HitstunFrames => hitstunFrames;
    public int BlockstunFrames => blockstunFrames;

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
