using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class MusicDirector : MonoBehaviour
{
    public static MusicDirector Instance { get; private set; }

    [SerializeField] private MusicProfile profile;
    [SerializeField] private List<AudioSource> stems = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartAllStems();
    }

    private void StartAllStems()
    {
        double startTime = AudioSettings.dspTime + 0.1;
        foreach (var src in stems)
        {
            if (src == null || src.clip == null) continue;
            src.loop = true;
            src.PlayScheduled(startTime);
        }
    }

    public void TransitionTo(string key, float? duration = null)
    {
        if (profile == null || profile.mixer == null) return;
        var state = profile.Get(key);
        if (state == null || state.snapshots == null || state.snapshots.Length == 0) return;

        int n = state.snapshots.Length;
        var weights = new float[n];
        float sum = 0f;

        for (int i = 0; i < n; i++)
        {
            float w = i < state.weights.Length ? state.weights[i] : 1f;
            weights[i] = w;
            sum += w;
        }

        for (int i = 0; i < n; i++) weights[i] /= sum;

        float t = duration ?? state.transitionDuration;
        profile.mixer.TransitionToSnapshots(state.snapshots, weights, t);
    }
}