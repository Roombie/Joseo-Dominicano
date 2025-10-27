using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class StoryPager : MonoBehaviour
{
    [Header("Pages")]
    public List<StoryPage> pages = new();

    [Header("Input")]
    public InputActionReference submit;

    [Header("Behavior")]
    public bool playOnAwake = true;
    public bool allowPointerSubmit = true;
    public float safetyCooldown = 0.05f;

    [Header("Events")]
    public UnityEvent onAllPagesFinished;
    public UnityEvent<int> onPageStart;

    enum FlowState { Idle, Transitioning, WaitingForAdvance, Finished }
    FlowState state = FlowState.Idle;

    int currentIndex = -1;
    Coroutine flowRoutine;
    bool skipRequested;
    bool advanceRequested;

    void Awake()
    {
        if (submit)
        {
            submit.action.performed += OnSubmit;
            submit.action.Enable();
        }

        if (playOnAwake)
            StartCoroutine(WaitForSceneTransitionThenBegin());
    }

    IEnumerator WaitForSceneTransitionThenBegin()
    {
        // If scene transition manager doesn't exist, skip waiting
        if (SceneTransitionManager.Instance == null)
        {
            Debug.Log("[StoryPager] No SceneTransitionManager found — starting immediately.");
            Begin();
            yield break;
        }

        // Wait until the SceneTransitionManager exists and finishes transitioning
        while (SceneTransitionManager.Instance == null || SceneTransitionManager.Instance.isTransitioning)
            yield return null;

        Debug.Log("[StoryPager] Scene transition complete, beginning story flow");
        Begin();
    }

    void OnDestroy()
    {
        if (submit) submit.action.performed -= OnSubmit;
    }

    void Update()
    {
        if (!submit && allowPointerSubmit)
        {
            if (Input.GetMouseButtonDown(0) || TouchPressedThisFrame())
                OnSubmit(new InputAction.CallbackContext());
        }
    }

    bool TouchPressedThisFrame()
    {
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            return t.phase == UnityEngine.TouchPhase.Began;
        }
        return false;
    }

    public void Begin()
    {
        if (flowRoutine != null) StopCoroutine(flowRoutine);
        Debug.Log("[StoryPager] Begin story flow");
        flowRoutine = StartCoroutine(RunStory());
    }

    void OnSubmit(InputAction.CallbackContext ctx)
    {
        Debug.Log($"[StoryPager] Input received, current state = {state}");
        switch (state)
        {
            case FlowState.Transitioning:
                Debug.Log("[StoryPager] Skip requested");
                skipRequested = true;
                break;
            case FlowState.WaitingForAdvance:
                Debug.Log("[StoryPager] Advance requested");
                advanceRequested = true;
                break;
        }
    }

    IEnumerator RunStory()
    {
        if (pages == null || pages.Count == 0)
        {
            Debug.LogWarning("[StoryPager] No pages assigned!");
            state = FlowState.Finished;
            onAllPagesFinished?.Invoke();
            yield break;
        }

        state = FlowState.Transitioning;

        currentIndex = 0;
        var first = pages[currentIndex];
        skipRequested = false;
        onPageStart?.Invoke(currentIndex);

        Debug.Log($"[StoryPager] Playing first page: {first.name}");
        yield return PlayAppearWithSkip(first);

        state = FlowState.WaitingForAdvance;

        while (currentIndex < pages.Count - 1)
        {
            Debug.Log($"[StoryPager] Waiting for user to advance from page {currentIndex}");
            yield return WaitForAdvancePress();

            state = FlowState.Transitioning;

            var current = pages[currentIndex];
            var next = pages[currentIndex + 1];
            skipRequested = false;

            Debug.Log($"[StoryPager] Transitioning from {current.name} to {next.name}");
            yield return TransitionPagesWithSkip(current, next);

            currentIndex++;
            state = FlowState.WaitingForAdvance;
        }

        Debug.Log("[StoryPager] Waiting for final press to finish...");
        yield return WaitForAdvancePress();

        state = FlowState.Finished;
        Debug.Log("[StoryPager] All pages finished");
        onAllPagesFinished?.Invoke();
    }

    IEnumerator PlayAppearWithSkip(StoryPage page)
    {
        if (!page) yield break;

        Debug.Log($"[StoryPager] PlayAppear: {page.name}");
        var co = page.PlayAppear();
        while (co.MoveNext())
        {
            if (skipRequested)
            {
                Debug.Log("[StoryPager] Skipping appear animation");
                page.SkipCurrent();
                skipRequested = false;
            }
            yield return co.Current;
        }
        if (safetyCooldown > 0) yield return new WaitForSeconds(safetyCooldown);
        Debug.Log($"[StoryPager] Appear done: {page.name}");
    }

    IEnumerator TransitionPagesWithSkip(StoryPage current, StoryPage next)
    {
        if (next) next.gameObject.SetActive(true);

        Debug.Log($"[StoryPager] PlayLeave: {current?.name} / PlayAppear: {next?.name}");
        IEnumerator leaveCo = null;
        IEnumerator appearCo = null;

        if (current) leaveCo = current.PlayLeave();
        if (next) appearCo = next.PlayAppear();

        bool leaveDone = current == null;
        bool appearDone = next == null;

        while (!leaveDone || !appearDone)
        {
            if (skipRequested)
            {
                Debug.Log("[StoryPager] Skipping transition animations");
                if (current) current.SkipCurrent();
                if (next) next.SkipCurrent();
                skipRequested = false;
            }

            if (!leaveDone && !leaveCo.MoveNext()) leaveDone = true;
            if (!appearDone && !appearCo.MoveNext()) appearDone = true;

            yield return null;
        }

        if (safetyCooldown > 0) yield return new WaitForSeconds(safetyCooldown);
        Debug.Log($"[StoryPager] Transition done ({current?.name} -> {next?.name})");
    }

    IEnumerator WaitForAdvancePress()
    {
        advanceRequested = false;
        while (!advanceRequested) yield return null;
        Debug.Log("[StoryPager] Advance input confirmed");
        advanceRequested = false;
    }
}