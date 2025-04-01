using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    [SerializeField] private AudioClip backgroundMusic;
    private AudioSource[] audioSources;
    private int currentSourceIndex = 0;
    private double nextEventTime;
    // private float updateStep = 0.003f;  // Update check interval (3ms)
    private float bufferAhead = 0.05f;  // Schedule ahead time (50ms)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        audioSources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            audioSources[i] = gameObject.AddComponent<AudioSource>();
            audioSources[i].clip = backgroundMusic;
            audioSources[i].loop = false;
            audioSources[i].playOnAwake = false;
            audioSources[i].volume = 0.05f;
        }
    }

    private void Update()
    {
        double currentTime = AudioSettings.dspTime;

        // If we're within our buffer window, schedule the next clip
        if (currentTime + bufferAhead > nextEventTime)
        {
            // Schedule the next audio source to play exactly when the current one ends
            int nextSourceIndex = (currentSourceIndex + 1) % 2;
            audioSources[nextSourceIndex].PlayScheduled(nextEventTime);

            // Update timing for the next clip
            nextEventTime += backgroundMusic.length;

            // Switch sources
            currentSourceIndex = nextSourceIndex;
        }
    }

    public void PlayMusic()
    {
        // Stop all currently playing or scheduled audio
        foreach (var source in audioSources)
        {
            source.Stop();
        }

        // Start playing immediately
        double startTime = AudioSettings.dspTime;
        audioSources[currentSourceIndex].PlayScheduled(startTime);

        // Schedule the next event
        nextEventTime = startTime + backgroundMusic.length;
    }

    public void StopMusic()
    {
        foreach (var source in audioSources)
        {
            source.Stop();
        }
    }

    public void SetVolume(float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume * 0.1f); // Reduce volume even further
        foreach (var source in audioSources)
        {
            source.volume = clampedVolume;
        }
    }
}