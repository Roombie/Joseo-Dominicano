using UnityEngine;

/// <summary>
/// Every time this object is enabled, it randomly enables/disables
/// itself or a list of target objects based on a given chance.
/// </summary>
public sealed class ChanceEnabler : MonoBehaviour
{
    [Header("0 = never, 1 = always")]
    [Range(0f, 1f)]
    [SerializeField] private float enableChance = 0.5f;

    [Header("If empty, this GameObject is used")]
    [SerializeField] private GameObject[] targets;

    // Called every time the GameObject (or its parent) is enabled
    private void OnEnable()
    {
        bool shouldEnable = Random.value < enableChance;

        // If no targets assigned, affect this GameObject
        if (targets == null || targets.Length == 0)
        {
            // Avoid recursive OnEnable spam: only change if needed
            if (gameObject.activeSelf != shouldEnable)
                gameObject.SetActive(shouldEnable);

            return;
        }

        // Enable/disable all targets
        for (int i = 0; i < targets.Length; i++)
        {
            GameObject t = targets[i];
            if (t == null) continue;

            if (t.activeSelf != shouldEnable)
                t.SetActive(shouldEnable);
        }
    }
}