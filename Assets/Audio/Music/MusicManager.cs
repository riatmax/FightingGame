using UnityEngine;

public class MusicManager : MonoBehaviour
{
    /// <summary>
    ///
    ///     Plays background music from a playlist. Tracks crossfade into each other, and the playlist
    ///     can loop, shuffle, or play through once. A playlist with a single track just loops that track.
    ///
    ///     Uses two AudioSources internally so one can fade out while the other fades in.
    ///
    /// </summary>

    public static MusicManager Instance { get; private set; }

    [Header("--- Playlist ---")]
    [SerializeField] private AudioClip[] tracks;
    [Tooltip("Start playing as soon as the scene loads")]
    [SerializeField] private bool playOnStart = true;
    [Tooltip("Go back to the start of the playlist after the last track. Off = stops after the last track")]
    [SerializeField] private bool loopPlaylist = true;
    [Tooltip("Play tracks in random order (never the same track twice in a row)")]
    [SerializeField] private bool shuffle = false;

    [Header("--- Settings ---")]
    [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;
    [Tooltip("Seconds for a track to fade in and the previous one to fade out. 0 = hard cut")]
    [Min(0f)] [SerializeField] private float crossfadeDuration = 2f;
    [Tooltip("Keep playing when a new scene loads (no restart, no duplicate)")]
    [SerializeField] private bool persistAcrossScenes = false;

    private AudioSource[] _sources;
    private int _active;        // index of the source playing the current track
    private int _trackIndex = -1;
    private bool _isPlaying;
    private bool _isPaused;

    public bool IsPlaying => _isPlaying && !_isPaused;
    public float Volume => volume;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (persistAcrossScenes) DontDestroyOnLoad(gameObject);

        _sources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D sound
            source.volume = 0f;
            _sources[i] = source;
        }
    }

    private void Start()
    {
        if (playOnStart) Play();
    }

    private void Update()
    {
        if (_isPlaying && !_isPaused) CheckForTrackEnd();
        UpdateFades();
    }

    // ---------- Public controls ----------

    /// <summary>Starts the playlist from the beginning (or a random track if shuffle is on).</summary>
    public void Play()
    {
        if (tracks == null || tracks.Length == 0) return;

        _isPlaying = true;
        _isPaused = false;
        _trackIndex = -1;
        StartTrack(PickNextIndex());
    }

    /// <summary>Swaps to a different playlist, e.g. menu music vs. fight music.</summary>
    public void PlayPlaylist(AudioClip[] newTracks)
    {
        tracks = newTracks;
        Play();
    }

    /// <summary>Fades out and stops.</summary>
    public void Stop()
    {
        _isPlaying = false;
        _isPaused = false;
        // UpdateFades fades everything to 0 and stops the sources
    }

    public void Pause()
    {
        if (!_isPlaying || _isPaused) return;
        _isPaused = true;
        foreach (AudioSource s in _sources) s.Pause();
    }

    public void Resume()
    {
        if (!_isPlaying || !_isPaused) return;
        _isPaused = false;
        foreach (AudioSource s in _sources) s.UnPause();
    }

    /// <summary>Skips to the next track in the playlist.</summary>
    public void NextTrack()
    {
        if (!_isPlaying || tracks == null || tracks.Length == 0) return;
        AdvanceTrack();
    }

    /// <summary>For a volume slider in an options menu.</summary>
    public void SetVolume(float newVolume)
    {
        volume = Mathf.Clamp01(newVolume);
    }

    // ---------- Internals ----------

    private void CheckForTrackEnd()
    {
        AudioSource current = _sources[_active];
        if (current.clip == null) return;

        // One looping track: let the AudioSource loop it by itself
        if (tracks.Length == 1 && loopPlaylist) return;

        float remaining = current.clip.length - current.time;
        bool finished = !current.isPlaying;

        // Start the next track early so the two overlap during the crossfade
        if (finished || remaining <= crossfadeDuration)
            AdvanceTrack();
    }

    private void AdvanceTrack()
    {
        int next = PickNextIndex();

        if (next < 0)
        {
            // Reached the end of a non-looping playlist
            _isPlaying = false;
            return;
        }

        StartTrack(next);
    }

    private int PickNextIndex()
    {
        if (tracks.Length == 1) return loopPlaylist || _trackIndex < 0 ? 0 : -1;

        if (shuffle)
        {
            int index;
            do
            {
                index = Random.Range(0, tracks.Length);
            } while (index == _trackIndex);
            return index;
        }

        int nextIndex = _trackIndex + 1;
        if (nextIndex >= tracks.Length)
            return loopPlaylist ? 0 : -1;
        return nextIndex;
    }

    private void StartTrack(int index)
    {
        if (tracks[index] == null) return;

        _trackIndex = index;
        _active = 1 - _active; // the other source becomes the active one; the old one fades out

        AudioSource source = _sources[_active];
        source.clip = tracks[index];
        source.loop = tracks.Length == 1 && loopPlaylist;
        source.time = 0f;
        source.volume = crossfadeDuration > 0f ? 0f : volume;
        source.Play();
    }

    private void UpdateFades()
    {
        float step = crossfadeDuration > 0f
            ? (volume / crossfadeDuration) * Time.unscaledDeltaTime
            : float.MaxValue;

        for (int i = 0; i < _sources.Length; i++)
        {
            AudioSource source = _sources[i];
            if (!source.isPlaying && !_isPaused) continue;

            bool isCurrent = _isPlaying && i == _active;
            float target = isCurrent ? volume : 0f;
            source.volume = Mathf.MoveTowards(source.volume, target, step);

            // A faded-out source is done
            if (!isCurrent && source.volume <= 0f && !_isPaused)
                source.Stop();
        }
    }
}