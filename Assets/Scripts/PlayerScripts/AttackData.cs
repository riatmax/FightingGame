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

    [Header("Hitbox")]
    public Vector2 HitboxOffset;
    public Vector2 HitboxSize;

    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
    public int Damage => damage;
    public bool BreaksBlock => breaksBlock;
    public SFXGroup HitSFX => hitSFX;

    public AttackPhase GetPhase(int frame)
    {
        AttackPhase currAttackPhase;

        if (frame < startupFrames) currAttackPhase = AttackPhase.Startup;
        else if (frame < startupFrames + activeFrames) currAttackPhase = AttackPhase.Active;
        else currAttackPhase = AttackPhase.Recovery;

        return currAttackPhase;
    }
}
