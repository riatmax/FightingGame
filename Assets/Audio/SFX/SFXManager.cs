using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SFXManager : MonoBehaviour
{
    public static SFXManager Instance { get; private set; }

    [Tooltip("Played on the first active frame for any attack with an empty playlist")]
    [SerializeField] private AudioClip placeholderSFX;
    [Range(0f, 1f)] [SerializeField] private float placeholderVolume = 1f;

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

    public void OnAttackFrame(AttackData attack, int frame)
    {
        int soundFrame = attack.StartupFrames;

        // No playlist on this attack -> placeholder
        if (attack.SFXPlaylist.Count == 0)
        {
            if (frame == soundFrame) Play(placeholderSFX, placeholderVolume);
            return;
        }

        // Otherwise the attack's own playlist decides what plays
        foreach (AttackSFX sfx in attack.SFXPlaylist)
        {
            if (frame == soundFrame + sfx.framesAfterStartup)
                Play(sfx.GetRandomClip(), sfx.volume);
        }
    }

    private void Play(AudioClip clip, float volume)
    {
        if (clip == null) return;
        audioSource.PlayOneShot(clip, volume);
    }
}