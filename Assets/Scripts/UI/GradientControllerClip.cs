using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[Serializable]
public class GradientControllerClip : PlayableAsset, ITimelineClipAsset
{
    public GradientControllerBehaviour template = new GradientControllerBehaviour();
    public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.ClipIn | ClipCaps.Extrapolation;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<GradientControllerBehaviour>.Create(graph, template);
    }
}