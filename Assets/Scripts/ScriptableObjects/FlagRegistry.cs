using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FlagRegistry", menuName = "Localization/Flag Registry")]
public class FlagRegistry : ScriptableObject
{
    public List<FlagEntry> entries = new List<FlagEntry>();

    public FlagEntry Get(string code)
    {
        return entries.Find(e => e.localeCode == code);
    }
}