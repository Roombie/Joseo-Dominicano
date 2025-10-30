using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageCycler : MonoBehaviour
{
    [SerializeField] private TMP_Text label;

    private async void OnEnable()
    {
        await LocalizationSettings.InitializationOperation.Task;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        RefreshLabel();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale _)
    {
        RefreshLabel();
    }

    public async void CycleLanguage()
    {
        await LocalizationSettings.InitializationOperation.Task;

        var locales = LocalizationSettings.AvailableLocales?.Locales;
        if (locales == null || locales.Count == 0) return;

        var cur = LocalizationSettings.SelectedLocale;
        int i = Mathf.Max(0, locales.IndexOf(cur));
        var next = locales[(i + 1) % locales.Count];

        LocalizationSettings.SelectedLocale = next;
        PlayerPrefs.SetString(SettingsKeys.LanguageKey, next.Identifier.Code);
        PlayerPrefs.Save();

        RefreshLabel();
    }

    private void RefreshLabel()
    {
        var l = LocalizationSettings.SelectedLocale;
        string name = "—";

        if (l != null)
        {
            // access directly and validate the rest
            var ci = l.Identifier.CultureInfo; // can be null in custom premises
            if (ci != null && !string.IsNullOrEmpty(ci.NativeName))
                name = ci.NativeName;
            else if (!string.IsNullOrEmpty(l.LocaleName))
                name = l.LocaleName;
            else
                name = l.Identifier.Code;
        }

        if (label != null)
            label.text = $"{name.ToUpperInvariant()}";
    }
}