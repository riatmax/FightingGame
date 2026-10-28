using UnityEngine;

[CreateAssetMenu(fileName = "NewAttack", menuName = "Fighting Game/Attack Data")]
public class AttackData : ScriptableObject
{
    public int StartupFrames;
    public int ActiveFrames;
    public int RecoveryFrames;

    public int TotalFrames()
    {
        return StartupFrames + ActiveFrames + RecoveryFrames;
    }
}
