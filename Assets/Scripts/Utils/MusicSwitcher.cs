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
        double startAt = AudioSettings.dspTime + 0.1;
        foreach (var src in allSources)
        {
            if (src && src.clip)
                src.PlayScheduled(startAt);
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
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            if (from) from.volume = Mathf.Lerp(startVol, 0f, t);
            if (to) to.volume = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        if (from) from.volume = 0f;
        if (to) to.volume = 1f;
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
}