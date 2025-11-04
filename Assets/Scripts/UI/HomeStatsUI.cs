using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;
using System;
using System.Collections;
using TMPro;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class HomeStatsUI : MonoBehaviour
{
    [Serializable]
    public class HomeStat
    {
        public string id;                      // e.g., "money", "quota", "remaining"
        public LocalizedString label;          // Localized string reference
        [NonSerialized] public Func<string> valueGetter; // Runtime value provider
    }

    [Header("References")]
    [SerializeField] private Transform statsContainer;
    [SerializeField] private HomeStatLine statLinePrefab;

    [Header("Separator Settings")]
    [Tooltip("If set, will appear after the specified stat index.")]
    [SerializeField] private TMP_Text separatorPrefab;
    [SerializeField, Min(0)] private int separatorAfterIndex = 1;
    [SerializeField, Range(1, 200)] private int separatorDotCount = 69;
    [SerializeField] private float lineDelay = 0.4f;
    [SerializeField] private float dotDelay = 0.02f;

    [Header("Audio")]
    [SerializeField] private AudioClip lineAppearSFX;
    [SerializeField] private AudioClip dotSFX;
    [SerializeField, Min(1)] private int dotSoundEveryNDots = 4;

    [Header("Stats Data")]
    public List<HomeStat> stats = new List<HomeStat>();

    [Header("Skip Settings")]
    [SerializeField] private InputActionReference skipAction;
    [SerializeField] private bool allowSkip = true;
    [SerializeField] private bool skipOnAnyTouch = true;
    [SerializeField] private bool skipOnAnyKey = false;

    [Header("Events")]
    public UnityEvent onDisplayAwake;          // Fires when this script enables
    public UnityEvent onDisplayStart;          // Fires when stats display begins
    public UnityEvent onDisplayComplete;       // Fires when all stats are shown
    public UnityEvent onDisplayInterrupted;    // Fires if display is cleared before completion
    public UnityEvent onSkipPressed;           // Fires when user skips the animation

    private readonly List<HomeStatLine> instantiatedLines = new();
    private TMP_Text instantiatedSeparator;
    private Coroutine currentDisplayRoutine;
    private bool isDisplayInProgress = false;
    private bool skipRequested = false;
    private int currentStatIndex = 0;

    void OnEnable()
    {
        // Fire awake event when script becomes enabled
        onDisplayAwake?.Invoke();

        if (allowSkip)
        {
            if (skipAction != null)
            {
                skipAction.action.Enable();
                skipAction.action.performed += OnSkipAction;
            }
        }
    }

    void OnDisable()
    {
        if (skipAction != null)
        {
            skipAction.action.performed -= OnSkipAction;
            skipAction.action.Disable();
        }
    }

    void Update()
    {
        if (!isDisplayInProgress || !allowSkip || skipRequested) return;

        // Check for alternative skip methods
        if (skipOnAnyTouch && Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            SkipDisplay();
        }
        else if (skipOnAnyKey && Input.anyKeyDown)
        {
            SkipDisplay();
        }
    }

    private void OnSkipAction(InputAction.CallbackContext context)
    {
        if (isDisplayInProgress && !skipRequested)
        {
            SkipDisplay();
        }
    }

    public void DisplayStats()
    {
        if (!isActiveAndEnabled)
        {
            StartCoroutine(WaitUntilActiveThenDisplay());
            return;
        }

        Clear();
        currentDisplayRoutine = StartCoroutine(DisplayRoutineDelayed(2f));
    }

    private IEnumerator WaitUntilActiveThenDisplay()
    {
        yield return new WaitUntil(() => isActiveAndEnabled);
        Clear();
        currentDisplayRoutine = StartCoroutine(DisplayRoutineDelayed(2f));
    }

    private IEnumerator DisplayRoutineDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentDisplayRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        isDisplayInProgress = true;
        skipRequested = false;
        currentStatIndex = 0;
        
        // Fire start event
        onDisplayStart?.Invoke();

        for (currentStatIndex = 0; currentStatIndex < stats.Count; currentStatIndex++)
        {
            // Check for skip before each line
            if (skipRequested)
            {
                // Skip requested - complete all remaining stats instantly
                CompleteRemainingStatsInstantly();
                break;
            }

            yield return StartCoroutine(DisplaySingleStat(currentStatIndex));
        }

        // Only fire complete if we didn't skip or we finished naturally
        if (!skipRequested || currentStatIndex >= stats.Count)
        {
            // Fire complete event
            onDisplayComplete?.Invoke();
        }

        isDisplayInProgress = false;
        currentDisplayRoutine = null;
    }

    private IEnumerator DisplaySingleStat(int index)
    {
        var stat = stats[index];

        // Instantiate and enable
        var line = Instantiate(statLinePrefab, statsContainer);
        line.gameObject.SetActive(true);
        instantiatedLines.Add(line);

        // Localize label
        AsyncOperationHandle<string> localizedOp = stat.label.GetLocalizedStringAsync();
        
        // Wait for localization but check for skip
        while (!localizedOp.IsDone && !skipRequested)
        {
            yield return null;
        }

        if (localizedOp.IsValid() && localizedOp.Status == AsyncOperationStatus.Succeeded &&
            !string.IsNullOrEmpty(localizedOp.Result))
        {
            line.SetLabel(localizedOp.Result);
        }
        else
        {
            line.SetLabel(stat.id); // fallback
            Debug.LogWarning($"[HomeStatsUI] Missing localization for '{stat.id}'");
        }

        // Value
        string value = stat.valueGetter?.Invoke() ?? "-";
        line.SetValue(value);

        // Play line sound (unless we're skipping)
        if (lineAppearSFX != null && !skipRequested)
            AudioManager.Instance?.Play(lineAppearSFX, SoundCategory.SFX);

        // Wait for line delay, but check for skip during wait
        if (!skipRequested)
        {
            float timer = 0f;
            while (timer < lineDelay && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        // Separator animation
        if (separatorPrefab != null && index == separatorAfterIndex)
        {
            yield return StartCoroutine(DisplaySeparator());
        }
    }

    private IEnumerator DisplaySeparator()
    {
        if (skipRequested)
        {
            // Create separator instantly when skipping
            instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
            instantiatedSeparator.text = new string('.', separatorDotCount);
            yield break;
        }

        instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
        instantiatedSeparator.text = string.Empty;

        for (int d = 0; d < separatorDotCount; d++)
        {
            if (skipRequested) break;

            instantiatedSeparator.text += ".";
            if (dotSFX != null && d % dotSoundEveryNDots == 0)
                AudioManager.Instance?.Play(dotSFX, SoundCategory.SFX);

            // Wait for dot delay, but check for skip
            float timer = 0f;
            while (timer < dotDelay && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }

        if (!skipRequested)
        {
            float timer = 0f;
            while (timer < lineDelay && !skipRequested)
            {
                timer += Time.deltaTime;
                yield return null;
            }
        }
    }

    private void CompleteRemainingStatsInstantly()
    {
        // Fire skip event immediately when skip is detected
        onSkipPressed?.Invoke();

        // Complete all remaining stats instantly
        for (int i = currentStatIndex; i < stats.Count; i++)
        {
            var stat = stats[i];

            // Check if this stat was already created (in case we skipped during its creation)
            bool alreadyExists = i < instantiatedLines.Count;
            
            HomeStatLine line;
            if (alreadyExists)
            {
                line = instantiatedLines[i];
            }
            else
            {
                // Instantiate new line if it doesn't exist
                line = Instantiate(statLinePrefab, statsContainer);
                line.gameObject.SetActive(true);
                instantiatedLines.Add(line);
            }

            // Set label instantly (use ID as fallback)
            line.SetLabel(stat.id);

            // Value
            string value = stat.valueGetter?.Invoke() ?? "-";
            line.SetValue(value);

            // Add separator if needed
            if (separatorPrefab != null && i == separatorAfterIndex && instantiatedSeparator == null)
            {
                instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
                instantiatedSeparator.text = new string('.', separatorDotCount);
            }
        }

        // Fire complete event after all stats are shown
        onDisplayComplete?.Invoke();
    }

    public void SkipDisplay()
    {
        if (isDisplayInProgress && !skipRequested)
        {
            skipRequested = true;
            // Note: onSkipPressed is now called in CompleteRemainingStatsInstantly()
        }
    }

    public void RefreshInstant()
    {
        for (int i = 0; i < instantiatedLines.Count && i < stats.Count; i++)
        {
            string value = stats[i].valueGetter?.Invoke() ?? "-";
            instantiatedLines[i].SetValue(value);
        }
    }

    public void Clear()
    {
        // If display was in progress and we're clearing, fire interrupted event
        if (isDisplayInProgress)
        {
            onDisplayInterrupted?.Invoke();
            isDisplayInProgress = false;
        }

        skipRequested = false;
        currentStatIndex = 0;

        // Stop any running coroutine
        if (currentDisplayRoutine != null)
        {
            StopCoroutine(currentDisplayRoutine);
            currentDisplayRoutine = null;
        }

        foreach (var l in instantiatedLines)
            if (l != null) Destroy(l.gameObject);
        instantiatedLines.Clear();

        if (instantiatedSeparator != null)
        {
            Destroy(instantiatedSeparator.gameObject);
            instantiatedSeparator = null;
        }
    }

    public void PlayAppearSound()
    {
        if (lineAppearSFX != null)
            AudioManager.Instance?.Play(lineAppearSFX, SoundCategory.SFX);
    }

    // Optional: Public method to check if display is currently running
    public bool IsDisplayInProgress()
    {
        return isDisplayInProgress;
    }

    // Optional: Public method to check if skip was requested
    public bool WasSkipped()
    {
        return skipRequested;
    }
}