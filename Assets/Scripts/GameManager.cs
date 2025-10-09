using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region General
    [Header("General")]
    float _playerMoney;
    // [SerializeField] int _dayCount = 5;
    [System.Serializable]
    struct Level
    {
        public int dayQuota;
        public float dayDuration;
    }
    [SerializeField] Level[] _days;

    void Awake()
    {
        _testMainMenu.SetActive(true);
        _testGameplayScreen.SetActive(false);
        _testHomeScreen.SetActive(false);
        _testGameOverScreen.SetActive(false);
    }

    void Update()
    {
        _Gameplay_UpdateDebugText();
        _Home_UpdateDebugText();
    }

    void OnDestroy()
    {

    }

    #endregion
    #region Main Menu
    [Header("Main Menu")]
    [SerializeField] GameObject _testMainMenu;
    [SerializeField] Animator _mainMenuAnimator;
    [SerializeField] string _onPlayMainMenuAnimation;

    public void _MainMenu_Display()
    {
        _testMainMenu.SetActive(true);
    }

    public void _MainMenu_PlayGame()
    {
        _testMainMenu.SetActive(false);
        _Gameplay_Display();
    }

    public void _MainMenu_QuitGame()
    {
        Application.Quit();
    }

    #endregion
    #region Gameplay
    [Header("Gameplay")]
    [SerializeField] GameObject _testGameplayScreen;
    [SerializeField] TMP_Text _testGameplayStateText;
    int _currentDay = 0;
    float _shiftTimeDuration = 30;
    [SerializeField] float _displayHurryUpOn = 10;
    float _shiftTimeLeft;
    int _currentShiftPayment = 0;
    int _playerCurrentWeight = 0;
    [SerializeField] int _playerSackCarrySpaceLimit = 10;
    int _playerSackCarrySpaceUsed;
    [SerializeField] TestValuable[] _testLevelValuables;
    List<TestValuable> _playerSack = new List<TestValuable>();
    Coroutine dayTimeRoutine;
    bool inShift;
    bool isInHurry;
    string _playerSackDebugOutput;
    [System.Serializable]
    struct TestValuable
    {
        public string name;
        public int value;
        public int weight;
        public int carrySpace;
    }

    public void _Gameplay_Display()
    {
        _testGameplayScreen.SetActive(true);
    }

    public void _Gameplay_StartDay()
    {
        if (inShift) return;
        inShift = true;
        ResetDayTimer();
        RunDayTimer();
        _currentDay++;
        _playerSackDebugOutput = "Clear";
        _currentShiftPayment = 0;
    }

    public void _Gameplay_CollectValuable()
    {
        if (!inShift) return;
        TestValuable randomCollectable = _testLevelValuables[Random.Range(0, _testLevelValuables.Length)];
        if (randomCollectable.carrySpace <= _playerSackCarrySpaceLimit - _playerSackCarrySpaceUsed)
        {
            _playerSack.Add(randomCollectable);
            _playerCurrentWeight += randomCollectable.weight;
            _playerSackCarrySpaceUsed += randomCollectable.carrySpace;
            _playerSackDebugOutput = "\"" + randomCollectable.name + "\" added ";
            if (_playerSackCarrySpaceUsed == _playerSackCarrySpaceLimit)
            {
                _playerSackDebugOutput += "(FULL)";
            }
            else
            {
                _playerSackDebugOutput += "(" + _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit + ")";
            }
        }
        else
        {
            _playerSackDebugOutput = "Not enough space to collect \"" + randomCollectable.name + "\" (" 
            + randomCollectable.carrySpace + " in (" + _playerSackCarrySpaceUsed + "/" 
            + _playerSackCarrySpaceLimit + ")";
        }
        
    }

    public void _Gameplay_DepositValuables()
    {
        if (!inShift) return;
        for (int i = 0; i < _playerSack.Count; i++)
        {
            var sackItem = _playerSack[i];
            _currentShiftPayment += sackItem.value;
            _playerCurrentWeight -= sackItem.weight;
            _playerSackCarrySpaceUsed -= sackItem.carrySpace;
        }
        _playerSack.Clear();
        _playerSackDebugOutput = "Clear";
    }

    public void _Gameplay_UpdateDebugText()
    {
        var gameplayDebugText = new System.Text.StringBuilder();
        gameplayDebugText.AppendLine("GAMEPLAY DEBUG TEXT");
        gameplayDebugText.AppendLine("───────────────────────────");
        if (inShift)
        {
            gameplayDebugText.AppendLine("Day " + _currentDay);
            gameplayDebugText.AppendLine("Shift Time Left: " + Mathf.Ceil(_shiftTimeLeft) + "s " + (isInHurry ? "HURRY UP!!!" : ""));
            gameplayDebugText.AppendLine("Shift Payment: " + _currentShiftPayment);
            gameplayDebugText.AppendLine("───────────────────────────");
            gameplayDebugText.AppendLine("Player Sack Space: " + _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit);
            gameplayDebugText.AppendLine("Player Weight: " + _playerCurrentWeight);
            gameplayDebugText.AppendLine("───────────────────────────");
            gameplayDebugText.AppendLine("Player Sack Valuables: ");
            for (int i = 0; i < _playerSack.Count; i++)
            {
                var sackItem = _playerSack[i];
                gameplayDebugText.AppendLine((i + 1) + "- " + sackItem.name + " - value: " + sackItem.value + ", weight: " + sackItem.weight);
            }
            gameplayDebugText.AppendLine("───────────────────────────");
            gameplayDebugText.AppendLine("Player Sack Update: " + _playerSackDebugOutput);
        }
        else
        {
            gameplayDebugText.AppendLine("- Start Day to Display Gameplay -");
        }
        
        _testGameplayStateText.text = gameplayDebugText.ToString();
    }
    
    public void _Gameplay_OnPlayerDeath()
    {
        if (!inShift) return;
        OnGameplayEnd();
        _gameOverDisplay.Set("Game Over", "You died...");
        _GameOver_Display();
    }

    public void _Gameplay_OnTimeUp()
    {
        if (!inShift) return;
        OnGameplayEnd();
        _gameOverDisplay.Set("Game Over", "The truck left you behind");
        _GameOver_Display();
    }

    public void _Gameplay_End()
    {
        if (!inShift) return;
        OnGameplayEnd();
        _Home_Display();
    }

    void OnGameplayEnd()
    {
        inShift = false;
        _playerMoney += _currentShiftPayment;
        _currentShiftPayment = 0;
        _testGameplayScreen.SetActive(false);
    }

    IEnumerator Internal_RunDayTimer()
    {
        while (_shiftTimeLeft > 0)
        {
            if (_shiftTimeLeft <= _displayHurryUpOn && !isInHurry)
            {
                isInHurry = true;
                OnHurryUpDisplay();
            }
            _shiftTimeLeft -= Time.deltaTime;
            yield return null;
        }
        _shiftTimeLeft = 0;
        _Gameplay_OnTimeUp();
    }

    void RunDayTimer()
    {
        if (dayTimeRoutine != null) StopCoroutine(dayTimeRoutine);
        dayTimeRoutine = StartCoroutine(Internal_RunDayTimer());
    }

    void ResetDayTimer()
    {
        isInHurry = false;
        _shiftTimeLeft = _shiftTimeDuration;
    }
    
    void OnHurryUpDisplay()
    {
        
    }

    #endregion
    #region Home
    [Header("Home")]
    [SerializeField] GameObject _testHomeScreen;
    [SerializeField] TMP_Text _testHomeText;
    // [SerializeField] float _dayCost = 1200;
    

    public void _Home_Display()
    {
        _testHomeScreen.SetActive(true);
    }

    public void _Home_EndDay()
    {
        _testHomeScreen.SetActive(false);
        if (_playerMoney > _days[_currentDay-1].dayQuota)
        {
            if (_currentDay >= _days.Length)
            {
                _gameOverDisplay.Set("Good Ending", "Gimme some beer for the man! whooo");
                _GameOver_Display();

            }
            else
            {
                _Gameplay_Display();
            }
        }
        else
        {
            _gameOverDisplay.Set("Bad Ending", "Your family starved...");
            _GameOver_Display();
        }
        
    }
    
    public void _Home_UpdateDebugText()
    {
        var homeDebugText = new System.Text.StringBuilder();
        homeDebugText.AppendLine("HOME DEBUG TEXT");
        homeDebugText.AppendLine("───────────────────────────");
        homeDebugText.AppendLine("Current Money: " + _playerMoney);
        homeDebugText.AppendLine("───────────────────────────");
        homeDebugText.AppendLine("Day Quota: " + _days[_currentDay-1].dayQuota);
        
        _testHomeText.text = homeDebugText.ToString();
    }

    #endregion

    #region Game Over
    [Header("Game Over")]
    [SerializeField] GameObject _testGameOverScreen;
    [SerializeField] TMP_Text _gameOverTitle;
    [SerializeField] TMP_Text _gameOverContext;
    
    struct GameEndDisplay
    {
        string endingTitle;
        string endingContext;

        public void SetEndingLabels(TMP_Text title, TMP_Text context)
        {
            title.text = endingTitle;
            context.text = endingContext;
        } 

        public void Set(string title = "Game Over", string context = "")
        {
            endingTitle = title;
            endingContext = context;
        }
    }
    GameEndDisplay _gameOverDisplay;
    
    public void _GameOver_Display()
    {
        _currentDay = 0;
        _gameOverDisplay.SetEndingLabels(_gameOverTitle, _gameOverContext);
        _testGameOverScreen.SetActive(true);
    }

    public void _GameOver_GoToMainMenu()
    {
        _testGameOverScreen.SetActive(false);
        _MainMenu_Display();
    }
    
#endregion

}
