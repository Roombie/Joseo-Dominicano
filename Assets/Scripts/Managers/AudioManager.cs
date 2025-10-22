using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Mixer & Groups")]
    public AudioMixer mixer;
    public AudioMixerGroup sfxMixerGroup;
    public AudioMixerGroup musicMixerGroup;

    [Header("Object Pool")]
    public int poolSize = 10;

    // --- Internal state ---
    private Queue<AudioSource> sfxPool;
    private AudioSource musicSource;
    private readonly List<AudioSource> pausedSources = new();
    private readonly Dictionary<AudioClip, AudioSource> activeSounds = new();

    public AudioClip CurrentMusic { get; private set; }

    // AudioMixers parameters keys
    private const string MusicVolKey = "MusicVolume";
    private const string SoundVolKey = "SoundVolume";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePools();

        // Initialize mixer parameters from saved volume values WITHOUT writing back
        // (mute/unmute state will be applied by SettingsManager)
        float music = GetSavedVolumeValue(SettingType.MusicEnabledKey, 0.6f);
        float sfx   = GetSavedVolumeValue(SettingType.SFXEnabledKey,  0.6f);
        SetVolume(SettingType.MusicEnabledKey, music, persist:false);
        SetVolume(SettingType.SFXEnabledKey,  sfx,  persist:false);
    }

    private void InitializePools()
    {
        sfxPool = new Queue<AudioSource>();
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            sfxPool.Enqueue(source);
        }
    }

    // -------------------
    // Volume / Mute API
    // -------------------

    /// <summary>
    /// Set mixer volume from 0..1 linear. By default persists the value under a dedicated *volume* key.
    /// </summary>
    public void SetVolume(SettingType type, float volume, bool persist = true)
    {
        string param = type switch
        {
            SettingType.MusicEnabledKey => "MusicVolume",
            SettingType.SFXEnabledKey => "SFXVolume",
            _ => null
        };

        if (string.IsNullOrEmpty(param))
        {
            Debug.LogWarning($"[AudioManager] Mixer param not found for {type}");
            return;
        }

        float v = Mathf.Clamp01(volume);
        float dB = (v > 0.0001f) ? Mathf.Log10(v) * 20f : -80f; // 0..1 → dB
        mixer.SetFloat(param, dB);

        if (persist)
        {
            SetSavedVolumeValue(type, v);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Mute/unmute at the mixer without overwriting the user's saved volume.
    /// When unmuting, restores the saved volume (or provided fallback if not present).
    /// </summary>
    public void SetMuted(SettingType type, bool muted, float fallback = 0.6f)
    {
        float saved = GetSavedVolumeValue(type, fallback);
        SetVolume(type, muted ? 0f : saved, persist:false);
    }

    /// <summary>Reads the persisted *volume value* (0..1) for a given type.</summary>
    public float GetSavedVolumeValue(SettingType type, float fallback = 1f)
    {
        string key = GetVolumeValueKey(type);
        return PlayerPrefs.GetFloat(key, Mathf.Clamp01(fallback));
    }

    /// <summary>Writes the persisted *volume value* (0..1) for a given type.</summary>
    private void SetSavedVolumeValue(SettingType type, float value01)
    {
        string key = GetVolumeValueKey(type);
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(value01));
    }

    private string GetVolumeValueKey(SettingType type) => type switch
    {
        //  SettingType    =>    AudioMixers Exposed parameters
        SettingType.MusicEnabledKey => MusicVolKey,
        SettingType.SFXEnabledKey => SoundVolKey,
        _ => "UnknownVolume"
    };

    // -------------------
    // Playback API
    // -------------------

    public void Play(AudioClip clip, SoundCategory category = SoundCategory.SFX, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        if (clip == null) return;

        AudioSource source = GetAudioSource(category);
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = pitch;
        source.loop = loop;
        source.Play();

        activeSounds[clip] = source;

        if (category == SoundCategory.Music)
            CurrentMusic = clip;

        // Only auto-return one-shots.
        if (category == SoundCategory.SFX && !loop)
            StartCoroutine(ReturnToPoolAfterPlayback(source, clip.length / Mathf.Abs(pitch)));
    }

    public void PlayOnce(AudioClip clip, SoundCategory category = SoundCategory.SFX, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        if (!IsPlaying(clip))
            Play(clip, category, volume, pitch);
    }

    public void PlayOrReplace(AudioClip clip, SoundCategory category, bool loop = false)
    {
        if (clip == null) return;
        StopCategory(category);
        Play(clip, category, 1f, 1f, loop);
    }

    public void StopCategory(SoundCategory category)
    {
        var toRemove = new List<AudioClip>();

        foreach (var kv in activeSounds)
        {
            var clip = kv.Key;
            var src  = kv.Value;
            if (src != null && GetCategory(src.outputAudioMixerGroup) == category)
            {
                src.Stop();
                toRemove.Add(clip);
            }
        }

        foreach (var clip in toRemove)
            activeSounds.Remove(clip);

        if (category == SoundCategory.Music && musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            if (musicSource.clip != null && activeSounds.ContainsKey(musicSource.clip))
                activeSounds.Remove(musicSource.clip);
            CurrentMusic = null;
        }
    }

    private SoundCategory GetCategory(AudioMixerGroup group)
    {
        if (group == musicMixerGroup) return SoundCategory.Music;
        return SoundCategory.SFX;
    }

    private AudioSource GetAudioSource(SoundCategory category)
    {
        if (category == SoundCategory.Music)
        {
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.outputAudioMixerGroup = musicMixerGroup;
            }
            return musicSource;
        }

        AudioSource source = (category == SoundCategory.SFX && sfxPool.Count > 0)
            ? sfxPool.Dequeue()
            : gameObject.AddComponent<AudioSource>();

        source.outputAudioMixerGroup = sfxMixerGroup;
        source.playOnAwake = false;
        return source;
    }

    public void Stop(AudioClip clip)
    {
        if (clip == null || !activeSounds.ContainsKey(clip)) return;

        AudioSource source = activeSounds[clip];
        if (source != null) source.Stop();
        activeSounds.Remove(clip);
    }

    public bool IsPlaying(AudioClip clip)
        => System.Array.Exists(GetComponents<AudioSource>(), src => src.clip == clip && src.isPlaying);

    public void PauseAll() => PauseSources(GetComponents<AudioSource>());
    public void ResumeAll() => ResumeSources(pausedSources);

    public void PauseCategory(SoundCategory category)
    {
        foreach (var source in GetComponents<AudioSource>())
        {
            if (source.isPlaying && GetCategory(source) == category)
            {
                source.Pause();
                if (!pausedSources.Contains(source))
                    pausedSources.Add(source);
            }
        }
    }

    public void ResumeCategory(SoundCategory category)
    {
        foreach (var source in pausedSources)
        {
            if (source != null && GetCategory(source) == category)
                source.UnPause();
        }
    }

    public void StopAll()
    {
        foreach (var source in GetComponents<AudioSource>())
        {
            source.Stop();
            if (!sfxPool.Contains(source) && source != musicSource)
                sfxPool.Enqueue(source);
        }
        if (musicSource != null) musicSource.clip = null;
        CurrentMusic = null;
        pausedSources.Clear();
        activeSounds.Clear();
    }

    private IEnumerator ReturnToPoolAfterPlayback(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (source != null)
        {
            source.Stop();
            sfxPool.Enqueue(source);
        }
    }

    public void PlayBackgroundMusic(AudioClip clip, float volume = 1f, float pitch = 1f, bool loop = true)
    {
        if (musicSource != null && musicSource.isPlaying)
            StartCoroutine(FadeOutMusic(musicSource, 1f));

        Play(clip, SoundCategory.Music, volume, pitch, loop);
    }

    private IEnumerator FadeOutMusic(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (source == null) yield break;
            source.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }
        if (source != null)
        {
            source.volume = 0f;
            source.Stop();
        }
    }

    private void PauseSources(AudioSource[] sources)
    {
        foreach (var source in sources)
        {
            if (source.isPlaying)
            {
                source.Pause();
                if (!pausedSources.Contains(source))
                    pausedSources.Add(source);
            }
        }
    }

    private void ResumeSources(List<AudioSource> sources)
    {
        foreach (var source in sources)
        {
            if (source != null)
                source.UnPause();
        }
        sources.Clear();
    }

    private SoundCategory GetCategory(AudioSource source)
    {
        if (source.outputAudioMixerGroup == musicMixerGroup) return SoundCategory.Music;
        return SoundCategory.SFX;
    }
}

public enum SoundCategory
{
    SFX,
    Music,
}