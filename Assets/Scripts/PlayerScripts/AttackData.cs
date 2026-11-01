using System.Collections.Generic;
using UnityEngine;

public enum AttackPhase
{
    Startup,
    Active,
    Recovery
}

[System.Serializable]
public class AttackSFX
{
    [Tooltip("One of these is picked at random each time this entry fires")]
    public AudioClip[] clips;
    [Tooltip("0 = the first frame after startup ends (first active frame)")]
    [Min(0)] public int framesAfterStartup = 0;
    [Range(0f, 1f)] public float volume = 1f;

    [System.NonSerialized] private int _lastIndex = -1;

    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        int index;
        do
        {
            index = Random.Range(0, clips.Length);
        } while (index == _lastIndex);

        _lastIndex = index;
        return clips[index];
    }
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

    [Header("Audio")]
    [Tooltip("Leave empty to use the SFXManager's placeholder sound")]
    [SerializeField] private List<AttackSFX> sfxPlaylist = new List<AttackSFX>();

    public int TotalFrames => startupFrames + activeFrames + recoveryFrames;
    public int StartupFrames => startupFrames;
    public int Damage => damage;
    public float Range => range;
    public IReadOnlyList<AttackSFX> SFXPlaylist => sfxPlaylist;

    public AttackPhase GetPhase(int frame)
    {
        AttackPhase currAttackPhase;

        if (frame < startupFrames) currAttackPhase = AttackPhase.Startup;
        else if (frame < startupFrames + activeFrames) currAttackPhase = AttackPhase.Active;
        else currAttackPhase = AttackPhase.Recovery;

        return currAttackPhase;
    }
}