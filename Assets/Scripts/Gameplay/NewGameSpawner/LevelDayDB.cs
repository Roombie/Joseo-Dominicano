using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DayConfig", menuName = "Scriptable Object/Day DB", order = 2)]
public class LevelDayDB : ScriptableObject
{
    [SerializeField] List<LevelDayConfig> _days = new List<LevelDayConfig>();
    public List<LevelDayConfig> days => _days;
}
