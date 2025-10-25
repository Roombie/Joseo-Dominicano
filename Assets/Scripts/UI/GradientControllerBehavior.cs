using System;
using UnityEngine;
using UnityEngine.Playables;

[Serializable]
public class GradientControllerBehaviour : PlayableBehaviour
{
    [Header("Color A")]
    public bool animateColorA;
    public Color fromColorA = Color.clear, toColorA = Color.clear;
    public AnimationCurve curveColorA = AnimationCurve.Linear(0,0,1,1);

    [Header("Color B")]
    public bool animateColorB;
    public Color fromColorB = Color.clear, toColorB = Color.clear;
    public AnimationCurve curveColorB = AnimationCurve.Linear(0,0,1,1);

    [Header("Gradient Offset")]
    public bool animateGradientOffset;
    [Range(-1f, 1f)] public float fromGradientOffset, toGradientOffset;
    public AnimationCurve curveGradientOffset = AnimationCurve.Linear(0,0,1,1);

    [Header("Gradient Derivation")]
    public bool animateGradientDerivation;
    [Range(0f, 1f)] public float fromGradientDerivation, toGradientDerivation;
    public AnimationCurve curveGradientDerivation = AnimationCurve.Linear(0,0,1,1);

    [Header("Custom Speed (Vector2)")]
    public bool animateCustomSpeed;
    public Vector2 fromCustomSpeed, toCustomSpeed;
    public AnimationCurve curveCustomSpeed = AnimationCurve.Linear(0,0,1,1);

    [Header("Optional: Enum Keyword 'Type' (index)")]
    public bool animateTypeIndex;
    public int fromTypeIndex, toTypeIndex;

    public void Evaluate(double normT, float weight, ref Accum acc)
    {
        float t;

        if (animateColorA) {
            t = curveColorA.Evaluate((float)normT);
            acc.colorA += Color.LerpUnclamped(fromColorA, toColorA, t) * weight;
            acc.mColorA += weight;
        }
        if (animateColorB) {
            t = curveColorB.Evaluate((float)normT);
            acc.colorB += Color.LerpUnclamped(fromColorB, toColorB, t) * weight;
            acc.mColorB += weight;
        }
        if (animateGradientOffset) {
            t = curveGradientOffset.Evaluate((float)normT);
            acc.gradOffset += Mathf.LerpUnclamped(fromGradientOffset, toGradientOffset, t) * weight;
            acc.mGradOffset += weight;
        }
        if (animateGradientDerivation) {
            t = curveGradientDerivation.Evaluate((float)normT);
            acc.gradDerivation += Mathf.LerpUnclamped(fromGradientDerivation, toGradientDerivation, t) * weight;
            acc.mGradDerivation += weight;
        }
        if (animateCustomSpeed) {
            t = curveCustomSpeed.Evaluate((float)normT);
            acc.customSpeed += Vector2.LerpUnclamped(fromCustomSpeed, toCustomSpeed, t) * weight;
            acc.mCustomSpeed += weight;
        }
        if (animateTypeIndex) {
            float lerped = Mathf.LerpUnclamped(fromTypeIndex, toTypeIndex, (float)normT);
            acc.typeWeighted += lerped * weight;
            acc.typeWeight += weight;
            acc.hasType = true;
        }
    }
}

public struct Accum
{
    public Color colorA, colorB;
    public float gradOffset, gradDerivation;
    public Vector2 customSpeed;

    public float mColorA, mColorB, mGradOffset, mGradDerivation, mCustomSpeed;
    public bool hasType; public float typeWeighted; public float typeWeight;

    public void Reset()
    {
        colorA = colorB = Color.clear;
        gradOffset = gradDerivation = 0f;
        customSpeed = Vector2.zero;
        mColorA = mColorB = mGradOffset = mGradDerivation = mCustomSpeed = 0f;
        hasType = false; typeWeighted = 0f; typeWeight = 0f;
    }
}

public class GradientControllerMixer : PlayableBehaviour
{
    private bool _firstFrameProcessed = false;
    private Accum _baseState; // Store the state before any clips influence

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        var ctrl = playerData as UIGradientMultiplyController;
        if (ctrl == null) return;

        var acc = new Accum(); 
        acc.Reset(); // Always start fresh

        int inputCount = playable.GetInputCount();
        bool anyActiveClips = false;

        for (int i = 0; i < inputCount; i++)
        {
            float w = playable.GetInputWeight(i);
            if (w <= 0f) continue;

            anyActiveClips = true;

            var inputPlayable = playable.GetInput(i);
            var beh = ((ScriptPlayable<GradientControllerBehaviour>)inputPlayable).GetBehaviour();

            double dur = inputPlayable.GetDuration();
            double time = inputPlayable.GetTime();
            double normT = dur > 0.0 ? Mathf.Clamp01((float)(time / dur)) : 0.0;

            beh.Evaluate(normT, w, ref acc);
        }

        // If no active clips, do nothing (material keeps current state)
        if (!anyActiveClips) return;

        // Apply the accumulated values from clips only
        ApplyAccumulatedValues(ctrl, ref acc);
    }

    private void CaptureBaseState(UIGradientMultiplyController ctrl, ref Accum acc)
    {
        // Start with the controller's current values as base
        acc.colorA = ctrl.colorA;
        acc.colorB = ctrl.colorB;
        acc.gradOffset = ctrl.gradientOffset;
        acc.gradDerivation = ctrl.gradientDerivation;
        acc.customSpeed = ctrl.customSpeed;
        
        // Mark all as having full contribution
        acc.mColorA = 1f;
        acc.mColorB = 1f;
        acc.mGradOffset = 1f;
        acc.mGradDerivation = 1f;
        acc.mCustomSpeed = 1f;

        if (ctrl.debugMode) 
        {
            Debug.Log($"Captured base state - ColorA: {acc.colorA}, ColorB: {acc.colorB}");
        }
    }

    private void ApplyAccumulatedValues(UIGradientMultiplyController ctrl, ref Accum acc)
    {
        // Apply blended values from active clips only
        if (acc.mColorA > 0f) 
        {
            var c = acc.colorA / acc.mColorA;
            ctrl.ApplyColorA_NoRecord(c);
            if (ctrl.debugMode) Debug.Log($"Mixer Applied ColorA: {c} (contributions: {acc.mColorA})");
        }

        if (acc.mColorB > 0f) 
        {
            var c = acc.colorB / acc.mColorB;
            ctrl.ApplyColorB_NoRecord(c);
            if (ctrl.debugMode) Debug.Log($"Mixer Applied ColorB: {c} (contributions: {acc.mColorB})");
        }

        if (acc.mGradOffset > 0f)
        {
            float value = acc.gradOffset / acc.mGradOffset;
            ctrl.ApplyGradientOffset_NoRecord(value);
        }

        if (acc.mGradDerivation > 0f)
        {
            float value = acc.gradDerivation / acc.mGradDerivation;
            ctrl.ApplyGradientDerivation_NoRecord(value);
        }

        if (acc.mCustomSpeed > 0f)
        {
            Vector2 value = acc.customSpeed / acc.mCustomSpeed;
            ctrl.ApplyCustomSpeed_NoRecord(value);
        }

        if (acc.hasType && acc.typeWeight > 0f) 
        {
            int rounded = Mathf.RoundToInt(acc.typeWeighted / acc.typeWeight);
            ctrl.SetTypeIndex(rounded);
        }
    }

    // Reset when the playable is destroyed (Timeline stops)
    public override void OnPlayableDestroy(Playable playable)
    {
        _firstFrameProcessed = false;
    }
}