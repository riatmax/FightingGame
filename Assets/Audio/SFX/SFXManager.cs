using UnityEngine;

[System.Serializable]
public class SFXGroup
{
    [Tooltip("One of these is picked at random each time this group plays")]
    public AudioClip[] clips;
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

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    /// <summary>
    ///
    ///     Plays every sound effect in the game. Each attack holds its own hit sound in its AttackData,
    ///     everything shared between attacks lives here
    ///
    /// </summary>



    public static SFXManager Instance { get; private set; }

    [Header("--- Player Sounds ---")]
    [Tooltip("Plays when an attack's active frames end without hitting anyone")]
    [SerializeField] private SFXGroup attackWhiff = new SFXGroup();
    [Tooltip("Plays on top of the attack's own hit sound when a player gets hit")]
    [SerializeField] private SFXGroup playerHit = new SFXGroup();
    [SerializeField] private SFXGroup footstep = new SFXGroup();
    [Tooltip("Plays instead of the hit sounds when a player blocks an attack")]
    [SerializeField] private SFXGroup defend = new SFXGroup();

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    public void PlayAttackHit(AttackData attack)
    {
        Play(attack.HitSFX);
        Play(playerHit);
    }

    public void PlayAttackWhiff()
    {
        Play(attackWhiff);
    }

    public void PlayFootstep()
    {
        Play(footstep);
    }

    public void PlayDefend()
    {
        Play(defend);
    }

    private void Play(SFXGroup group)
    {
        AudioClip clip = group.GetRandomClip();
        if (clip == null) return;

        audioSource.PlayOneShot(clip, group.volume);
    }
}
