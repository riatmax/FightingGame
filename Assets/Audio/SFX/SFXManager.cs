using UnityEngine;

[System.Serializable]
public class SFXGroup
{
    [Tooltip("One of these is picked at random each time this group plays")]
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Pitch the sound is centered on. 1 = normal, below 1 = deeper, above 1 = higher")]
    [Range(0.5f, 2f)] public float basePitch = 1f;
    [Tooltip("Total pitch spread around the base pitch. 0 = no variation, 0.2 = +/-0.1 around the base pitch")]
    [Range(0f, 1f)] public float pitchVariance = 0f;

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

    public float GetRandomPitch()
    {
        float half = pitchVariance * 0.5f;
        return Mathf.Max(0.1f, basePitch + Random.Range(-half, half));
    }
}

public class SFXManager : MonoBehaviour
{
    /// <summary>
    ///
    ///     Plays every sound effect in the game. Each attack holds its own hit sound in its AttackData,
    ///     everything shared between attacks lives here.
    ///
    ///     Sounds play through a small pool of AudioSources so each one can have its own pitch
    ///     without affecting sounds that are already playing.
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

    [Header("--- Settings ---")]
    [Tooltip("How many sounds can play at the same time")]
    [Min(1)] [SerializeField] private int voiceCount = 8;

    private AudioSource[] _voices;
    private int _nextVoice;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _voices = new AudioSource[voiceCount];
        for (int i = 0; i < voiceCount; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound
            _voices[i] = source;
        }
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
        if (group == null) return;

        AudioClip clip = group.GetRandomClip();
        if (clip == null) return;

        AudioSource voice = GetFreeVoice();
        voice.clip = clip;
        voice.volume = group.volume;
        voice.pitch = group.GetRandomPitch();
        voice.Play();
    }

    private AudioSource GetFreeVoice()
    {
        // Prefer a source that isn't playing anything
        foreach (AudioSource voice in _voices)
            if (!voice.isPlaying) return voice;

        // All busy: cut off the next one in rotation
        AudioSource stolen = _voices[_nextVoice];
        _nextVoice = (_nextVoice + 1) % _voices.Length;
        return stolen;
    }
}