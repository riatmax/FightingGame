using Unity.Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineImpulseSource))]
public class HitEffects : MonoBehaviour
{
    /// <summary>
    ///
    ///     Everything that makes a hit feel good - VFX, blood and camera shake. Each attack
    ///     holds its own hit VFX and shake in its AttackData, everything shared lives here
    ///
    /// </summary>



    public static HitEffects Instance { get; private set; }

    [Header("--- Blood ---")]
    [SerializeField] private GameObject bloodVFX;
    [SerializeField] private float bloodScale = 0.25f;

    [Header("--- Block ---")]
    [SerializeField] private GameObject blockVFX;
    [SerializeField] private float blockVFXScale = 0.5f;
    [Tooltip("Shield that flashes up around the player who blocked")]
    [SerializeField] private GameObject shieldVFX;
    [SerializeField] private float shieldScale = 0.45f;
    [SerializeField] private float shieldDuration = 0.2f;
    [SerializeField] private float blockShakeForce = 0.03f;

    [Header("--- Strike Arc ---")]
    [Tooltip("Swoosh along the path of an attack, one per player so it matches their color")]
    [SerializeField] private GameObject[] strikeArcVFX;
    [SerializeField] private float strikeArcScale = 0.35f;
    [Tooltip("From the attacker's feet, for a player facing right. X flips with facing")]
    [SerializeField] private Vector3 strikeArcOffset = new Vector3(0.45f, 1.35f, -0.3f);
    [Tooltip("Rotation for a player facing right. Facing left mirrors it")]
    [SerializeField] private Vector3 strikeArcRotation = new Vector3(50f, 270f, 270f);

    [Header("--- KO ---")]
    [Tooltip("Added on top of the shake from the attack that landed the KO")]
    [SerializeField] private float koShakeForce = 0.1f;
    [Range(0.01f, 1f)] [SerializeField] private float koTimeScale = 0.2f;
    [Tooltip("Real seconds the slow motion lasts, it isn't affected by the slow motion itself")]
    [SerializeField] private float koSlowMotionDuration = 2f;

    [Header("--- Spawning ---")]
    [Tooltip("Seconds before a spawned effect gets cleaned up")]
    [SerializeField] private float vfxLifetime = 3f;

    private CinemachineImpulseSource _impulseSource;
    private float _freezeEndTime;
    private float _slowMotionEndTime;

    private void Awake()
    {
        Instance = this;
        _impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    // Freezes and slow motion both run on real time, and a freeze always wins over slow motion
    private void Update()
    {
        UpdateTimeScale();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    private void UpdateTimeScale()
    {
        if (Time.unscaledTime < _freezeEndTime) Time.timeScale = 0f;
        else if (Time.unscaledTime < _slowMotionEndTime) Time.timeScale = koTimeScale;
        else Time.timeScale = 1f;
    }

    public void PlayHit(AttackData attack, Vector3 point, int attackerFacing)
    {
        Spawn(attack.HitVFX, point, Quaternion.identity, attack.HitVFXScale, vfxLifetime);
        Spawn(bloodVFX, point, Quaternion.Euler(0f, attackerFacing > 0 ? 0f : 180f, 0f), bloodScale, vfxLifetime);
        Shake(attack.ShakeForce);

        if (attack.FreezeTime > 0f)
        {
            _freezeEndTime = Mathf.Max(_freezeEndTime, Time.unscaledTime + attack.FreezeTime);
            UpdateTimeScale();
        }
    }

    public void PlayStrikeArc(int playerIndex, Vector3 attackerPosition, int facing)
    {
        if (playerIndex >= strikeArcVFX.Length) return;

        Vector3 position = attackerPosition + new Vector3(strikeArcOffset.x * facing, strikeArcOffset.y, strikeArcOffset.z);
        Quaternion rotation = Quaternion.Euler(0f, facing > 0 ? 0f : 180f, 0f) * Quaternion.Euler(strikeArcRotation);
        Spawn(strikeArcVFX[playerIndex], position, rotation, strikeArcScale, vfxLifetime);
    }

    public void PlayBlock(Vector3 point, Vector3 defenderPosition)
    {
        Spawn(blockVFX, point, Quaternion.identity, blockVFXScale, vfxLifetime);
        Spawn(shieldVFX, defenderPosition, Quaternion.identity, shieldScale, shieldDuration);
        Shake(blockShakeForce);
    }

    // Slow motion starts once any freeze from the finishing hit is over
    public void PlayKO()
    {
        Shake(koShakeForce);
        _slowMotionEndTime = Mathf.Max(_freezeEndTime, Time.unscaledTime) + koSlowMotionDuration;
        UpdateTimeScale();
    }

    public float KOSequenceEndTime => _slowMotionEndTime;

    private void Shake(float force)
    {
        _impulseSource.GenerateImpulseWithForce(force);
    }

    private void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float lifetime)
    {
        if (prefab == null) return;

        GameObject effect = Instantiate(prefab, position, rotation);
        effect.transform.localScale = Vector3.one * scale;

        // The packs use Local scaling, which ignores the parent and would leave the effect full size
        foreach (ParticleSystem system in effect.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = system.main;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        }

        Destroy(effect, lifetime);
    }
}
