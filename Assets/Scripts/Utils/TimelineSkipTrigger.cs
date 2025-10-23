using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Events;
using UnityEngine.Timeline;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

public class TimelineSkipTrigger : MonoBehaviour
{
    [Header("Timeline Reference")]
    public PlayableDirector director;

    [Header("Optional Marker Name (leave empty to jump to end)")]
    public string markerName = "";

    [Header("Events")]
    public UnityEvent onTimelineSkipped;
    public UnityEvent onTimelineEnded;

    [Header("Input")]
    public InputActionReference submitAction;

    private bool hasSkipped = false;
    private bool hasEnded = false;

    void OnEnable()
    {
        if (director != null)
            director.stopped += OnTimelineStopped;

        EnhancedTouchSupport.Enable();

        if (submitAction != null)
        {
            submitAction.action.Enable();
            submitAction.action.performed += OnSubmitPerformed;
        }
    }

    void OnDisable()
    {
        if (director != null)
            director.stopped -= OnTimelineStopped;

        if (submitAction != null)
        {
            submitAction.action.performed -= OnSubmitPerformed;
            submitAction.action.Disable();
        }

        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        // If no InputActionReference is assigned, fallback to default new Input System keys/touch
        if (submitAction == null)
        {
            bool submitPressed = false;
            bool touchPressed = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                    submitPressed = true;
            }

            if (!submitPressed && Gamepad.current != null)
            {
                if (Gamepad.current.buttonSouth.wasPressedThisFrame)
                    submitPressed = true;
            }

            if (Touchscreen.current != null)
            {
                var t = Touchscreen.current.primaryTouch;
                if (t.press.wasPressedThisFrame)
                    touchPressed = true;
            }

            if ((submitPressed || touchPressed) && !hasSkipped)
            {
                SkipOrJumpTimeline();
            }
        }
    }

    void OnSubmitPerformed(InputAction.CallbackContext ctx)
    {
        if (!hasSkipped && ctx.performed)
        {
            SkipOrJumpTimeline();
        }
    }

    void SkipOrJumpTimeline()
    {
        if (director == null)
            return;

        double targetTime = -1;

        if (!string.IsNullOrEmpty(markerName))
        {
            if (double.TryParse(markerName, System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double parsedSeconds))
            {
                targetTime = Mathf.Clamp((float)parsedSeconds, 0f, (float)director.duration);
            }
            else
            {
                targetTime = FindMarkerTime(markerName);
            }
        }

        if (targetTime < 0)
            targetTime = director.duration;

        // Avoid jumping if we have already passed that time
        if (director.time >= targetTime)
        {
            Debug.Log("[TimelineSkipTrigger] Timeline already past marker, skip ignored.");
            return;
        }

        director.time = targetTime;
        director.Play();

        hasSkipped = true;
        onTimelineSkipped?.Invoke();
    }

    void OnTimelineStopped(PlayableDirector pd)
    {
        if (pd == director && !hasEnded)
        {
            hasEnded = true;
            onTimelineEnded?.Invoke();
        }
    }

    double FindMarkerTime(string name)
    {
        if (director == null || director.playableAsset == null) return -1;

        var timelineAsset = director.playableAsset as TimelineAsset;
        if (timelineAsset == null) return -1;

        foreach (var root in timelineAsset.GetRootTracks())
        {
            double t = FindMarkerOnTrackRecursive(root, name);
            if (t >= 0) return t;
        }

        foreach (var track in timelineAsset.GetOutputTracks())
        {
            double t = FindMarkerOnTrackRecursive(track, name);
            if (t >= 0) return t;
        }

        return -1;
    }

    double FindMarkerOnTrackRecursive(TrackAsset track, string name)
    {
        if (track == null) return -1;

        foreach (var marker in track.GetMarkers())
        {
            var prop = marker.GetType().GetProperty("name");
            if (prop != null)
            {
                var val = prop.GetValue(marker, null) as string;
                if (!string.IsNullOrEmpty(val) && val.Equals(name, System.StringComparison.Ordinal))
                    return marker.time;
            }

            if (marker.ToString().IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return marker.time;
        }

        foreach (var child in track.GetChildTracks())
        {
            double t = FindMarkerOnTrackRecursive(child, name);
            if (t >= 0) return t;
        }

        return -1;
    }
}