using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Music Profile")]
public class MusicProfile : ScriptableObject
{
    public AudioMixer mixer;

    [Tooltip("States available for this game or scene")]
    public MusicStateAsset[] states;

    public MusicStateAsset Get(string key)
    {
        foreach (var s in states)
            if (s != null && s.key == key) return s;
        return null;
    }
}