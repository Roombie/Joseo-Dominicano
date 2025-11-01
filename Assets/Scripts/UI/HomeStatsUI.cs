using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Localization;
using System;
using System.Collections;
using TMPro;
using UnityEngine.ResourceManagement.AsyncOperations;

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
    [SerializeField, Range(1, 200)] private int separatorDotCount = 60;
    [SerializeField] private float lineDelay = 0.4f;
    [SerializeField] private float dotDelay = 0.02f;

    [Header("Stats Data")]
    public List<HomeStat> stats = new List<HomeStat>();

    private readonly List<HomeStatLine> instantiatedLines = new();
    private TMP_Text instantiatedSeparator;

    // ---------------------------------------------------------------------
    // Public API
    // ---------------------------------------------------------------------

    public void DisplayStats()
    {
        if (!isActiveAndEnabled)
        {
            StartCoroutine(WaitUntilActiveThenDisplay());
            return;
        }

        Clear();
        StartCoroutine(DisplayRoutine());
    }

    private IEnumerator WaitUntilActiveThenDisplay()
    {
        yield return new WaitUntil(() => isActiveAndEnabled);
        Clear();
        StartCoroutine(DisplayRoutine());
    }

    // ---------------------------------------------------------------------
    // Core Display Coroutine
    // ---------------------------------------------------------------------

    private IEnumerator DisplayRoutine()
    {
        for (int i = 0; i < stats.Count; i++)
        {
            var stat = stats[i];

            // Instantiate a new stat line
            var line = Instantiate(statLinePrefab, statsContainer);
            line.gameObject.SetActive(true); // ✅ Ensure it’s visible
            instantiatedLines.Add(line);

            // Try to get localized label
            AsyncOperationHandle<string> localizedOp = stat.label.GetLocalizedStringAsync();
            yield return localizedOp;

            if (localizedOp.Status == AsyncOperationStatus.Succeeded &&
                !string.IsNullOrEmpty(localizedOp.Result))
            {
                line.SetLabel(localizedOp.Result);
            }
            else
            {
                line.SetLabel(stat.id); // fallback label
                Debug.LogWarning($"[HomeStatsUI] Missing localization for '{stat.id}'");
            }

            // Assign value (non-localized)
            string value = stat.valueGetter?.Invoke() ?? "-";
            line.SetValue(value);

            yield return new WaitForSeconds(lineDelay);

            // Separator animation
            if (separatorPrefab != null && i == separatorAfterIndex)
            {
                instantiatedSeparator = Instantiate(separatorPrefab, statsContainer);
                instantiatedSeparator.text = string.Empty;

                for (int d = 0; d < separatorDotCount; d++)
                {
                    instantiatedSeparator.text += ".";
                    yield return new WaitForSeconds(dotDelay);
                }

                yield return new WaitForSeconds(lineDelay);
            }
        }
    }

    // ---------------------------------------------------------------------
    // Refresh existing stats without re-instantiating
    // ---------------------------------------------------------------------

    public void RefreshInstant()
    {
        for (int i = 0; i < instantiatedLines.Count && i < stats.Count; i++)
        {
            string value = stats[i].valueGetter?.Invoke() ?? "-";
            instantiatedLines[i].SetValue(value);
        }
    }

    // ---------------------------------------------------------------------
    // Cleanup helpers
    // ---------------------------------------------------------------------

    public void Clear()
    {
        foreach (var l in instantiatedLines)
        {
            if (l != null) Destroy(l.gameObject);
        }
        instantiatedLines.Clear();

        if (instantiatedSeparator != null)
        {
            Destroy(instantiatedSeparator.gameObject);
            instantiatedSeparator = null;
        }
    }
}