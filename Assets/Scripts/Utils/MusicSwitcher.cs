using System.Collections;
using UnityEngine;

public enum MusicState { Menu, Gameplay, Hurry }

public class MusicSwitcher : MonoBehaviour
{
    public AudioSource menuSource;
    public AudioSource gameplaySource;
    public AudioSource hurrySource;

    private AudioSource[] allSources;
    private MusicState current = MusicState.Menu;
    private Coroutine muteRoutine;

    void OnEnable()
    {
        AudioManager.OnMusicEnabledChanged += HandleMusicToggle;
    }

    void OnDisable()
    {
        AudioManager.OnMusicEnabledChanged -= HandleMusicToggle;
    }

    private void HandleMusicToggle(bool enabled)
    {
        StopAllCoroutines();

        foreach (var src in allSources)
        {
            if (!src) continue; 
            src.mute = false; // Ensure all sources are unmuted internally
            src.volume = enabled ? (src == GetSource(current) ? 1f : 0f) : 0f; // Adjust volume according to the global toggle
        }
    }

    void Awake()
    {
        allSources = new[] { menuSource, gameplaySource, hurrySource };
        foreach (var src in allSources)
        {
            if (src == null) continue;
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
        }
    }

    void Start()
    {
        bool musicEnabled = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;

        double startAt = AudioSettings.dspTime + 0.1;
        foreach (var src in allSources)
        {
            if (src && src.clip)
            {
                src.volume = musicEnabled ? 0f : 0f;
                src.PlayScheduled(startAt);

                if (!musicEnabled)
                    src.mute = true;
            }
        }

        SetImmediate(MusicState.Menu);
    }

    public void SetImmediate(MusicState state)
    {
        current = state;
        foreach (var s in allSources) if (s) s.volume = 0f;
        GetSource(state).volume = 1f;
    }

    public void SwitchTo(MusicState state, float fade = 1f)
    {
        if (state == current) return;
        StopAllCoroutines();
        StartCoroutine(FadeTo(state, fade));
    }

    private IEnumerator FadeTo(MusicState next, float fade)
    {
        var from = GetSource(current);
        var to = GetSource(next);
        current = next;
        float time = 0f;
        float duration = Mathf.Max(0.01f, fade);
        float startVol = from ? from.volume : 0f;
        if (to) to.volume = 0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            if (from) from.volume = Mathf.Lerp(startVol, 0f, t);
            if (to) to.volume = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        if (from) from.volume = 0f;
        if (to) to.volume = 1f;

        yield return null;
        if (from && from.volume > 0.0001f)
            from.volume = 0f;
    }

    private AudioSource GetSource(MusicState s)
    {
        return s switch
        {
            MusicState.Menu => menuSource,
            MusicState.Gameplay => gameplaySource,
            MusicState.Hurry => hurrySource,
            _ => null
        };
    }

    /// <summary>
    /// Silences or restores the music without stopping playback.
    /// </summary>
    public void SetMusicAudible(bool audible, float fadeTime = 0.5f)
    {
        if (muteRoutine != null)
            StopCoroutine(muteRoutine);
        muteRoutine = StartCoroutine(FadeMusicAudibility(audible, fadeTime));
    }

    private IEnumerator FadeMusicAudibility(bool audible, float duration)
    {
        float[] startVolumes = new float[allSources.Length];
        float[] targetVolumes = new float[allSources.Length];
        
        // Set target volumes based on current music state
        for (int i = 0; i < allSources.Length; i++)
        {
            if (allSources[i])
            {
                startVolumes[i] = allSources[i].volume;
                // Only the current state's source should have volume when audible
                targetVolumes[i] = audible ? (allSources[i] == GetSource(current) ? 1f : 0f) : 0f;
            }
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            for (int i = 0; i < allSources.Length; i++)
            {
                if (allSources[i])
                    allSources[i].volume = Mathf.Lerp(startVolumes[i], targetVolumes[i], k);
            }
            yield return null;
        }

        for (int i = 0; i < allSources.Length; i++)
        {
            if (allSources[i])
                allSources[i].volume = targetVolumes[i];
        }
    }
}