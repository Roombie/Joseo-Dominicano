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
        string flagKey = "default";

        if (l != null)
        {
            var ci = l.Identifier.CultureInfo;
            if (ci != null && !string.IsNullOrEmpty(ci.NativeName))
                name = ci.NativeName;
            else if (!string.IsNullOrEmpty(l.LocaleName))
                name = l.LocaleName;
            else
                name = l.Identifier.Code;

            // Choose icon keys matching your TMP sprite asset names
            switch (l.Identifier.Code)
            {
                case "es":
                    flagKey = "es"; // Spanish flag sprite name
                    break;
                case "en":
                    flagKey = "us"; // or "gb" depending on your preference
                    break;
                default:
                    flagKey = "globe"; // fallback icon
                    break;
            }
        }

        if (label != null)
            label.text = $"<sprite name=\"{flagKey}\"> {name.ToUpperInvariant()}";
    }
}