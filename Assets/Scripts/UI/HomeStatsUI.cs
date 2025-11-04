using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;

public class HomeStatsUI : MonoBehaviour
{
    [Serializable]
    public class HomeStat
    {
        public string id;                               
        public LocalizedString label;
        [NonSerialized] public Func<string> valueGetter;
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
    public UnityEvent onDisplayEnable;
    public UnityEvent onDisplayStart;
    public UnityEvent onDisplayComplete;
    public UnityEvent onDisplayInterrupted;
    public UnityEvent onSkipPressed;

    private readonly List<HomeStatLine> _instantiatedLines = new();
    private TMP_Text _instantiatedSeparator;
    private Coroutine _currentDisplayRoutine;

    private bool _isDisplayInProgress;
    private bool _skipRequested;
    private int _currentStatIndex;
    private int _displaySequenceId = 0;
    private bool _weEnabledSkipAction;

    private void OnEnable()
    {
        ResetVisualState();

        // Setup skip input
        if (allowSkip && skipAction?.action != null)
        {
            var action = skipAction.action;
            if (!action.enabled)
            {
                action.Enable();
                _weEnabledSkipAction = true;
            }
            action.performed += OnSkipAction;
        }
    }

    private void ResetVisualState()
    {
        StopAllCoroutines();
        ++_displaySequenceId;

        _isDisplayInProgress = false;
        _skipRequested = false;
        _currentStatIndex = 0;

        foreach (var l in _instantiatedLines)
            if (l != null) Destroy(l.gameObject);
        _instantiatedLines.Clear();

        if (_instantiatedSeparator != null)
        {
            Destroy(_instantiatedSeparator.gameObject);
            _instantiatedSeparator = null;
        }
    }

    private void OnDisable()
    {
        if (skipAction?.action != null)
        {
            skipAction.action.performed -= OnSkipAction;
            if (_weEnabledSkipAction)
            {
                skipAction.action.Disable();
                _weEnabledSkipAction = false;
            }
        }

        // Trigger interrupt if animation mid-run
        if (_isDisplayInProgress)
            onDisplayInterrupted?.Invoke();

        ResetVisualState();
    }

    private void Update()
    {
        if (!_isDisplayInProgress || !allowSkip || _skipRequested) return;

        // Keyboard skip
        if (skipOnAnyKey && Input.anyKeyDown)
        {
            SkipDisplay();
            return;
        }

        // Touch skip that ignores UI
        if (skipOnAnyTouch && Input.touchCount > 0)
        {
            var touch = Input.GetTouch(0);
            if (touch.phase == UnityEngine.TouchPhase.Began)
            {
                bool overUI = false;
#if ENABLE_INPUT_SYSTEM
                if (EventSystem.current != null)
                    overUI = EventSystem.current.IsPointerOverGameObject(touch.fingerId);
#else
                if (EventSystem.current != null)
                    overUI = EventSystem.current.IsPointerOverGameObject();
#endif
                if (!overUI)
                    SkipDisplay();
            }
        }
    }

    private void OnSkipAction(InputAction.CallbackContext _)
    {
        if (_isDisplayInProgress && !_skipRequested)
            SkipDisplay();
    }

    public void DisplayStats()
    {
        ResetVisualState();
        if (!isActiveAndEnabled)
        {
            StartCoroutine(WaitUntilActiveThenDisplay());
            return;
        }

        onDisplayEnable?.Invoke();
        _currentDisplayRoutine = StartCoroutine(DisplayRoutineDelayed(2f));
    }

    private IEnumerator WaitUntilActiveThenDisplay()
    {
        while (this != null && !isActiveAndEnabled)
            yield return null;
        if (this == null) yield break;

        ResetVisualState();
        _currentDisplayRoutine = StartCoroutine(DisplayRoutineDelayed(2f));
    }

    private IEnumerator DisplayRoutineDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        _currentDisplayRoutine = StartCoroutine(DisplayRoutine());
    }

    private IEnumerator DisplayRoutine()
    {
        int mySeq = ++_displaySequenceId;
        _isDisplayInProgress = true;
        _skipRequested = false;
        _currentStatIndex = 0;

        onDisplayStart?.Invoke();

        if (stats == null || stats.Count == 0)
        {
            _isDisplayInProgress = false;
            onDisplayComplete?.Invoke();
            yield break;
        }

        for (_currentStatIndex = 0; _currentStatIndex < stats.Count; _currentStatIndex++)
        {
            if (!IsMySeq(mySeq)) yield break;
            if (_skipRequested)
            {
                yield return StartCoroutine(CompleteRemainingStatsInstantly(mySeq));
                break;
            }
            yield return StartCoroutine(DisplaySingleStat(_currentStatIndex, mySeq));
        }

        if (!IsMySeq(mySeq)) yield break;

        if (_currentStatIndex >= stats.Count)
            onDisplayComplete?.Invoke();

        _isDisplayInProgress = false;
    }

    private IEnumerator DisplaySingleStat(int index, int seq)
    {
        var stat = stats[index];
        var line = Instantiate(statLinePrefab, statsContainer);
        line.gameObject.SetActive(true);
        _instantiatedLines.Add(line);

        AsyncOperationHandle<string> op = stat.label.GetLocalizedStringAsync();
        bool labelSet = false;

        while (!op.IsDone && !_skipRequested && IsMySeq(seq)) yield return null;

        try
        {
            if (IsMySeq(seq) && op.Status == AsyncOperationStatus.Succeeded && !string.IsNullOrEmpty(op.Result))
            {
                line.SetLabel(op.Result);
                labelSet = true;
            }
        }
        finally
        {
            if (op.IsValid()) Addressables.Release(op);
        }

        if (!labelSet)
            line.SetLabel(string.IsNullOrEmpty(stat.id) ? "-" : stat.id);

        string value = "-";
        try { value = stat.valueGetter?.Invoke() ?? "-"; } catch { }
        line.SetValue(value);

        if (!_skipRequested && IsMySeq(seq) && lineAppearSFX != null)
            SafePlay(lineAppearSFX);

        if (!_skipRequested && IsMySeq(seq))
        {
            float t = 0f;
            while (t < lineDelay && !_skipRequested && IsMySeq(seq))
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (IsMySeq(seq) && separatorPrefab != null && index == separatorAfterIndex)
            yield return StartCoroutine(DisplaySeparator(seq));
    }

    private IEnumerator DisplaySeparator(int seq)
    {
        if (_skipRequested || !IsMySeq(seq))
        {
            if (_instantiatedSeparator == null)
            {
                _instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
                _instantiatedSeparator.text = new string('.', Mathf.Max(0, separatorDotCount));
            }
            yield break;
        }

        _instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
        var sb = new StringBuilder(separatorDotCount);

        for (int i = 0; i < separatorDotCount && IsMySeq(seq); i++)
        {
            if (_skipRequested) break;

            sb.Append('.');
            _instantiatedSeparator.text = sb.ToString();

            if (dotSFX != null && (i % dotSoundEveryNDots) == 0)
                SafePlay(dotSFX);

            float t = 0f;
            while (t < dotDelay && !_skipRequested && IsMySeq(seq))
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        if (!_skipRequested && IsMySeq(seq))
        {
            float t = 0f;
            while (t < lineDelay && !_skipRequested && IsMySeq(seq))
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator CompleteRemainingStatsInstantly(int seq)
    {
        onSkipPressed?.Invoke();
        yield return null;

        for (int i = _currentStatIndex; i < stats.Count && IsMySeq(seq); i++)
        {
            var stat = stats[i];
            bool exists = i < _instantiatedLines.Count && _instantiatedLines[i] != null;

            HomeStatLine line = exists ? _instantiatedLines[i] : Instantiate(statLinePrefab, statsContainer);
            if (!exists) _instantiatedLines.Add(line);

            line.SetLabel(string.IsNullOrEmpty(stat.id) ? "-" : stat.id);
            string value = "-";
            try { value = stat.valueGetter?.Invoke() ?? "-"; } catch { }
            line.SetValue(value);

            if (separatorPrefab != null && i == separatorAfterIndex && _instantiatedSeparator == null)
            {
                _instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
                _instantiatedSeparator.text = new string('.', Mathf.Max(0, separatorDotCount));
            }

            yield return null;
        }

        _currentStatIndex = stats.Count;
    }

    public void SkipDisplay()
    {
        if (_isDisplayInProgress && !_skipRequested)
            _skipRequested = true;
    }

    public void RefreshInstant()
    {
        int count = Mathf.Min(_instantiatedLines.Count, stats.Count);
        for (int i = 0; i < count; i++)
        {
            string val = "-";
            try { val = stats[i].valueGetter?.Invoke() ?? "-"; } catch { }
            _instantiatedLines[i].SetValue(val);
        }
    }

    private bool IsMySeq(int seq) => seq == _displaySequenceId;

    private void SafePlay(AudioClip clip)
    {
        try { AudioManager.Instance?.Play(clip, SoundCategory.SFX); }
        catch { }
    }

    public void PlayAppearSound()
    {
        AudioManager.Instance?.Play(lineAppearSFX, SoundCategory.SFX);
    }
}