using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Music State")]
public class MusicStateAsset : ScriptableObject
{
    [Tooltip("Unique name or password of the state")]
    public string key;

    [Tooltip("Snapshots to mix and their relative weight")]
    public AudioMixerSnapshot[] snapshots;

    [Range(0f, 1f)] public float[] weights;

    [Tooltip("Default transition duration to this state")]
    public float transitionDuration = 0.75f;
}