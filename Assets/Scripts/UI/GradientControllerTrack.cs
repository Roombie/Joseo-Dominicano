using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.2f, 0.6f, 1f)]
[TrackBindingType(typeof(UIGradientMultiplyController))]
[TrackClipType(typeof(GradientControllerClip))]
public class GradientControllerTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<GradientControllerMixer>.Create(graph, inputCount);
    }
}