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
    [SerializeField] private int damage;
    [Tooltip("How far from the attacker the hit reaches, along the x axis")]
    [SerializeField] private float range;

    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
    public int Damage => damage;
    public float Range => range;

    public AttackPhase GetPhase(int frame)
    {
        AttackPhase currAttackPhase;

        if (frame < startupFrames) currAttackPhase = AttackPhase.Startup;
        else if (frame < startupFrames + activeFrames) currAttackPhase = AttackPhase.Active;
        else currAttackPhase = AttackPhase.Recovery;

        return currAttackPhase;
    }
}
