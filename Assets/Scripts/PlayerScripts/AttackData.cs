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
    [SerializeField] private int startupFrames;
    [SerializeField] private int activeFrames;
    [SerializeField] private int recoveryFrames;

    public Vector2 HitboxOffset;
    public Vector2 HitboxSize;

    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;

    public AttackPhase GetPhase(int frame)
    {
        AttackPhase currAttackPhase;

        if (frame < startupFrames) currAttackPhase = AttackPhase.Startup;
        else if (frame < startupFrames + activeFrames) currAttackPhase = AttackPhase.Active;
        else currAttackPhase = AttackPhase.Recovery;

        return currAttackPhase;
    }
}
