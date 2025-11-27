using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;

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
    [SerializeField] private bool skipOnAnyKey = true;

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

    void OnEnable()
    {
        ResetVisualState();
        SetupSkipInput();
    }

    void OnDisable()
    {
        TeardownSkipInput();

        if (_isDisplayInProgress)
            onDisplayInterrupted?.Invoke();

        ResetVisualState();
    }

    void Start()
    {
        for (int i = 0; i < stats.Count; i++)
        {
            var stat = stats[i];
            bool hasLocalization = stat.label != null &&
                                   !string.IsNullOrEmpty(stat.label.TableEntryReference) &&
                                   !string.IsNullOrEmpty(stat.label.TableReference);

            Debug.Log($"Stat {i} - ID: '{stat.id}', Localización: {(hasLocalization ? "CONFIGURADA" : "NO CONFIGURADA")}");
        }
    }

    void SetupSkipInput()
    {
        if (!allowSkip || skipAction == null || skipAction.action == null)
            return;

        var action = skipAction.action;
        action.performed -= OnSkipAction;
        action.performed += OnSkipAction;

        if (!action.enabled)
        {
            action.Enable();
            _weEnabledSkipAction = true;
        }
    }

    void TeardownSkipInput()
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
    }

    void OnSkipAction(InputAction.CallbackContext context)
    {
        if (_isDisplayInProgress && !_skipRequested && allowSkip)
        {
            SkipDisplay();
        }
    }

    void Update()
    {
        if (!_isDisplayInProgress || !allowSkip || _skipRequested)
            return;

        // Skip por cualquier tecla, evitando duplicar con skipAction
        if (skipOnAnyKey && Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
        {
            if (skipAction != null && skipAction.action != null)
            {
                bool isSkipActionKey = false;
                foreach (var binding in skipAction.action.bindings)
                {
                    if (binding.path.Contains("Keyboard"))
                    {
                        var key = Keyboard.current.FindKeyOnCurrentKeyboardLayout(binding.path);
                        if (key != null && key.wasPressedThisFrame)
                        {
                            isSkipActionKey = true;
                            break;
                        }
                    }
                }

                if (isSkipActionKey)
                    return;
            }

            SkipDisplay();
            return;
        }

        // Skip por toque si no hay skipAction
        if (skipOnAnyTouch && Touchscreen.current != null && skipAction == null)
        {
            if (Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                bool overUI = EventSystem.current != null &&
                              EventSystem.current.IsPointerOverGameObject(
                                  Touchscreen.current.primaryTouch.touchId.ReadValue());

                if (!overUI)
                    SkipDisplay();
            }
        }
    }

    void ResetVisualState()
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

    public void DisplayStats()
    {
        ResetVisualState();
        SetupSkipInput();

        _isDisplayInProgress = true;

        if (!isActiveAndEnabled)
        {
            StartCoroutine(WaitUntilActiveThenDisplay());
            return;
        }

        onDisplayEnable?.Invoke();
        _currentDisplayRoutine = StartCoroutine(DisplayRoutineDelayed(2f));
    }

    IEnumerator WaitUntilActiveThenDisplay()
    {
        while (this != null && !isActiveAndEnabled)
            yield return null;
        if (this == null) yield break;

        ResetVisualState();
        _currentDisplayRoutine = StartCoroutine(DisplayRoutineDelayed(2f));
    }

    IEnumerator DisplayRoutineDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        _currentDisplayRoutine = StartCoroutine(DisplayRoutine());
    }

    IEnumerator DisplayRoutine()
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
        if (_currentStatIndex >= stats.Count) onDisplayComplete?.Invoke();
        _isDisplayInProgress = false;
    }

    // ====== VERSIÓN ORIGINAL QUE YA FUNCIONABA (no la tocamos) ======
    IEnumerator DisplaySingleStat(int index, int seq)
    {
        var stat = stats[index];
        var line = Instantiate(statLinePrefab, statsContainer);
        line.gameObject.SetActive(true);
        _instantiatedLines.Add(line);

        AsyncOperationHandle<string> op = stat.label.GetLocalizedStringAsync();
        bool labelSet = false;

        while (!op.IsDone && !_skipRequested && IsMySeq(seq))
            yield return null;

        try
        {
            if (IsMySeq(seq) &&
                op.Status == AsyncOperationStatus.Succeeded &&
                !string.IsNullOrEmpty(op.Result))
            {
                line.SetLabel(op.Result);   // Texto localizado
                labelSet = true;
            }
        }
        finally
        {
            if (op.IsValid())
                Addressables.Release(op);
        }

        if (!labelSet)
            line.SetLabel(string.IsNullOrEmpty(stat.id) ? "-" : stat.id);

        string value = "-";
        try { value = stat.valueGetter?.Invoke() ?? "-"; } catch { }
        line.SetValue(value);

        if (!_skipRequested && IsMySeq(seq) && lineAppearSFX != null)
            SafePlay(lineAppearSFX);

        if (!_skipRequested && IsMySeq(seq))
            yield return new WaitForSeconds(lineDelay);

        if (IsMySeq(seq) && separatorPrefab != null && index == separatorAfterIndex)
            yield return StartCoroutine(DisplaySeparator(seq));
    }

    IEnumerator DisplaySeparator(int seq)
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

            yield return new WaitForSeconds(dotDelay);
        }

        if (!_skipRequested && IsMySeq(seq))
            yield return new WaitForSeconds(lineDelay);
    }

    // ====== NUEVO: SKIP PERO USANDO LOCALIZACIÓN ======
    IEnumerator CompleteRemainingStatsInstantly(int seq)
    {
        onSkipPressed?.Invoke();
        yield return null;

        for (int i = _currentStatIndex; i < stats.Count && IsMySeq(seq); i++)
        {
            var stat = stats[i];
            bool exists = i < _instantiatedLines.Count && _instantiatedLines[i] != null;

            HomeStatLine line = exists
                ? _instantiatedLines[i]
                : Instantiate(statLinePrefab, statsContainer);

            if (!exists)
                _instantiatedLines.Add(line);

            // Igual que DisplaySingleStat, pero sin chequear _skipRequested
            AsyncOperationHandle<string> op = stat.label.GetLocalizedStringAsync();
            bool labelSet = false;

            while (!op.IsDone && IsMySeq(seq))
                yield return null;

            try
            {
                if (IsMySeq(seq) &&
                    op.Status == AsyncOperationStatus.Succeeded &&
                    !string.IsNullOrEmpty(op.Result))
                {
                    line.SetLabel(op.Result);
                    labelSet = true;
                }
            }
            finally
            {
                if (op.IsValid())
                    Addressables.Release(op);
            }

            if (!labelSet)
                line.SetLabel(string.IsNullOrEmpty(stat.id) ? "-" : stat.id);

            string value = "-";
            try { value = stat.valueGetter?.Invoke() ?? "-"; } catch { }
            line.SetValue(value);

            if (separatorPrefab != null && i == separatorAfterIndex && _instantiatedSeparator == null)
            {
                _instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
                _instantiatedSeparator.text = new string('.', Mathf.Max(0, separatorDotCount));
            }

            // Un frame para que la UI se actualice
            yield return null;
        }

        _currentStatIndex = stats.Count;
        onDisplayComplete?.Invoke();
        _isDisplayInProgress = false;
    }

    public void SkipDisplay()
    {
        if (_isDisplayInProgress && !_skipRequested)
        {
            _skipRequested = true;
            if (_instantiatedSeparator != null)
                _instantiatedSeparator.text = new string('.', Mathf.Max(0, separatorDotCount));
        }
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

    bool IsMySeq(int seq) => seq == _displaySequenceId;

    void SafePlay(AudioClip clip)
    {
        try { AudioManager.Instance?.Play(clip, SoundCategory.SFX); } catch { }
    }

    // for animation events
    public void AppearSound()
    {
        if (lineAppearSFX != null)
        {
            SafePlay(lineAppearSFX);
            Debug.Log("Appear sound played");
        }
    }
}