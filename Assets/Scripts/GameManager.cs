using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    #region General
    [Header("General")]
    [SerializeField] InputReader input;
    float _playerMoney;
    int _currentDay = 0;
    // [SerializeField] int _dayCount = 5;
    [System.Serializable]
    public struct Level
    {
        public int dayQuota;
        public float dayDuration;
    }
    public Level[] days;
    [Header("References")]
    [SerializeField] DeliverInteraction _depositValuables; //rafamaster3
    [SerializeField] PlayerWallet _playerWallet; //rafamaster3
    [SerializeField] PlayerCollect _playerCollect;
    [SerializeField] OxygenManager _oxygenManager; //rafamaster3
    [SerializeField] List<Spawner> _spawners; //rafamaster3-modified

    void Awake()
    {
        _testMainMenu.SetActive(true);
        _testGameplayScreen.SetActive(false);
        _testHomeScreen.SetActive(false);
        _testGameOverScreen.SetActive(false);
        _depositValuables?.onDepositValuables.AddListener(_Gameplay_DepositValuables);
        _playerCollect?.onCollect.AddListener(OnValuableCollected);
        _oxygenManager?.onOxygenDepleted.AddListener(_Gameplay_OnPlayerDeath);
        input.PauseEvent += OnPause;
        input.EnablePlayer();

        foreach (Spawner spawner in _spawners)
        {
            spawner?.onSpawn.AddListener(AddSpawnedValuable);
        }
    }

    void Update()
    {
        _Gameplay_UpdateDebugText();
        _Home_UpdateDebugText();
    }

    void OnDestroy()
    {
        _depositValuables?.onDepositValuables.RemoveListener(_Gameplay_DepositValuables);
        _playerCollect?.onCollect.RemoveListener(OnValuableCollected);
        _oxygenManager?.onOxygenDepleted.RemoveListener(_Gameplay_OnPlayerDeath);
        input.PauseEvent -= OnPause;

        foreach (Spawner spawner in _spawners)
        {
            spawner?.onSpawn.RemoveListener(AddSpawnedValuable);
        }
    }

    public void ResetGame()
    {
        // RESET
        _currentDay = 0;
        _playerWallet.TrySpend(_playerWallet.Balance);
        _playerMoney = 0;
    }

    #endregion
    #region Main Menu
    [Header("Main Menu")]
    [SerializeField] GameObject _testMainMenu;
    [SerializeField] UnityEvent _onMenuDisplay;

    public void _MainMenu_Display()
    {
        _testMainMenu.SetActive(true);
        _onMenuDisplay?.Invoke();
    }

    public void _MainMenu_PlayGame()
    {
        // _testMainMenu.SetActive(false);
        _Gameplay_Display();
    }

    public void _MainMenu_QuitGame()
    {
        Application.Quit();
    }

    #endregion
    #region Gameplay
    [Header("Gameplay")]
    [SerializeField] Vector3 _playerInitialPosition;
    [SerializeField] GameObject _testGameplayScreen;
    [SerializeField] GameObject _pausePanel;
    [SerializeField] TMP_Text _testGameplayStateText;
    [SerializeField] TMP_Text _playerSackLabel;
    [SerializeField] CanvasGroup _playerSackUI;
    [SerializeField] TMP_Text _testGameplayTimer; //rafamaster3
    [SerializeField] TMP_Text _testGameplayShiftMoney; //rafamaster3
    [SerializeField] TMP_Text _testGameplayCollectedItemInfo; //rafamaster3
    [SerializeField] TMP_Text _testGameplayTotalMoney; //rafamaster3
    [SerializeField] int collectedInfoDuration = 3; //rafamaster3
    [SerializeField] float _onCollectedItemTotalDuration = 3;
    [SerializeField] float _onCollectedItemHold = 2;
    float _shiftTimeDuration = 30;
    [SerializeField] float _displayHurryUpOn = 10;
    float _shiftTimeLeft;
    int _currentShiftPayment = 0;
    int _playerCurrentWeight = 0;
    public int _playerSackCarrySpaceLimit = 10;
    int _playerSackCarrySpaceUsed;
    [SerializeField] TestValuableData[] _testLevelValuables;
    List<TestValuableData> _playerSack = new List<TestValuableData>();
    List<GameObject> _spawnedValuables = new List<GameObject>();
    void AddSpawnedValuable(GameObject valuable) => _spawnedValuables.Add(valuable);
    public void DestroyAllSpawnedValuables()
    {
        foreach (var item in _spawnedValuables)
        {
            if (item == null) continue;
            Destroy(item.gameObject);
        }
        _spawnedValuables.Clear();
    }
    Coroutine dayTimeRoutine;
    bool inShift;
    bool isInHurry;
    string _playerSackDebugOutput;
    [System.Serializable]
    public struct TestValuableData
    {
        public string name;
        public int value;
        public int weight;
        public int carrySpace;
    }
    [SerializeField] float _startDayDelay = 2;
    [SerializeField] string _dayLabelText = "Dia";
    [SerializeField] TMP_Text _dayLabel;
    [SerializeField] TMP_Text _dayGoalLabel;
    [SerializeField] string _dayGoalText = "Meta diaria";
    [SerializeField] TMP_Text _dayTimerLabel;
    [SerializeField] string _dayTimerText = "Tiempo";
    [SerializeField] UnityEvent _onPlay;
    [SerializeField] UnityEvent _onStartDay;
    [SerializeField] UnityEvent _onEndGameplay;
    bool isPaused;

    public void _Gameplay_Display()
    {
        _testGameplayScreen.SetActive(true);
        _playerCollect.transform.position = _playerInitialPosition;
        _playerCollect.transform.rotation = Quaternion.identity;
        if (_dayLabel != null) _dayLabel.text = _dayLabelText + " " + (_currentDay + 1);
        _onPlay?.Invoke();
    }

    public void _Gameplay_StartDayOnDelay()
    {
        StartCoroutine(GameplayStartDayDelay());
    }

    public void _Gameplay_Pause()
    {
        if (!inShift) return;
        Time.timeScale = 0;
        _pausePanel.SetActive(true);
        _oxygenManager.PauseOxygen();
        StopDayTimer();
        isPaused = true;
    }

    public void _Gameplay_Resume()
    {
        Time.timeScale = 1;
        _pausePanel.SetActive(false);
        _oxygenManager.ConsumeOxygen();
        RunDayTimer();
        isPaused = false;
    }

    void OnPause()
    {
        if (isPaused) _Gameplay_Resume();
        else _Gameplay_Pause();
    }
    
    public void _Gameplay_GoToMenu()
    {
        if (!inShift) return;
        _currentDay--;
        OnGameplayEnd();
        _MainMenu_Display();
    }


    IEnumerator GameplayStartDayDelay()
    {
        // Debug.Log("Waiting");
        yield return new WaitForSecondsRealtime(_startDayDelay);
        // Debug.Log("START");
        _Gameplay_StartDay();
    }

    public void _Gameplay_StartDay()
    {
        if (inShift) return;
        inShift = true;
        _currentDay++;
        ResetDayTimer();
        RunDayTimer();
        _oxygenManager.ResetOxygen();
        // _spawner.currentLevel = _currentDay;
        // _spawner.currentLevel = 0;
        // _spawner.LaunchSpawner();
        // _spawner.currentLevel = 1;

        foreach (Spawner spawner in _spawners)
        {
            if (spawner.enabled == false) continue;
            spawner.currentLevel = _currentDay;
            spawner.LaunchSpawner();
        }

        _playerSackDebugOutput = "Clear";
        _currentShiftPayment = 0;
        _dayGoalLabel.text = _dayGoalText + ": $" + _currentShiftPayment + "/$" + days[_currentDay - 1].dayQuota;
        _onStartDay?.Invoke();
    }

    void OnValuableCollected(TestValuable valuableComponent)
    {
        if (!inShift) return;
        var valuable = valuableComponent.valuable;
        if (valuable.value == 0 && valuable.carrySpace == 0)
        {
            // Destroy(valuableComponent.gameObject);
            return;
        }

        if (valuable.carrySpace <= _playerSackCarrySpaceLimit - _playerSackCarrySpaceUsed)
        {
            _playerSack.Add(valuable);
            _playerCurrentWeight += valuable.weight;
            _playerSackCarrySpaceUsed += valuable.carrySpace;
            _playerSackDebugOutput = "\"" + valuable.name + "\" added ";
            if (_playerSackCarrySpaceUsed == _playerSackCarrySpaceLimit)
            {
                { //rafamaster3: Display collectible info text for short time 
                    _testGameplayCollectedItemInfo.text = "FULL"; //rafamaster3
                    _testGameplayCollectedItemInfo.gameObject.SetActive(true); //rafamaster3
                    Invoke(nameof(HideInfoText), collectedInfoDuration); //rafamaster3
                }

                {
                    _playerSackLabel.text = "LLENO";
                    // Invoke("HidePlayerSack", collectedInfoDuration);
                    if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
                    WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());
                }
                _playerSackDebugOutput += "(FULL)";
            }
            else
            {
                {
                    _playerSackLabel.text = _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit;
                    // Invoke("HidePlayerSack", collectedInfoDuration);
                    if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
                    WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());
                }

                { //rafamaster3: Display collectible info text for short time
                    _testGameplayCollectedItemInfo.text = valuable.name + " $" + valuable.value + " " + valuable.weight + "g " + valuable.carrySpace + "L".ToString(); //rafamaster3
                    _testGameplayCollectedItemInfo.gameObject.SetActive(true); //rafamaster3
                    Invoke(nameof(HideInfoText), collectedInfoDuration); //rafamaster3
                }

                _playerSackDebugOutput += "(" + _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit + ")";
            }
            Destroy(valuableComponent.gameObject);
        }
        else
        {
            {
                _playerSackLabel.text = "Falta espacio";
                // Invoke("HidePlayerSack", collectedInfoDuration);
                if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
                WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());
            }
                
            _playerSackDebugOutput = "Not enough space to collect \"" + valuable.name + "\" ("
            + valuable.carrySpace + " in (" + _playerSackCarrySpaceUsed + "/"
            + _playerSackCarrySpaceLimit + ")";
        }
    }

    void HideInfoText() => _testGameplayCollectedItemInfo.gameObject.SetActive(false); //rafamaster3
    // void HidePlayerSack() => _playerSackUI.SetActive(false);

    Coroutine WhenItemCollectedRoutine;
    IEnumerator playerSackWhenItemCollectedRoutine()
    {

        float t = 0;
        float deltaDuration = 1 / (collectedInfoDuration - _onCollectedItemHold);
        while (t < 1)
        {
            _playerSackUI.alpha = Mathf.Lerp(0, 1, t);
            t += 2 * deltaDuration * Time.deltaTime;
            yield return null;
        }
        t = 1;

        yield return new WaitForSeconds(_onCollectedItemHold);

        while (t > 0)
        {
            _playerSackUI.alpha = Mathf.Lerp(0, 1, t);
            t -= 2 * deltaDuration * Time.deltaTime;
            yield return null;
        }

        _playerSackUI.alpha = 0;
    }


    public void _Gameplay_CollectValuable()
    {
        if (!inShift) return;
        TestValuableData randomCollectable = _testLevelValuables[Random.Range(0, _testLevelValuables.Length)];
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
        _dayGoalLabel.text = _dayGoalText + ": $" + _currentShiftPayment + "/$" + days[_currentDay-1].dayQuota;

        {
            _playerSackLabel.text = _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit;
            // Invoke("HidePlayerSack", collectedInfoDuration);
            if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
            WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());
        }
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

        if (_testGameplayStateText != null) _testGameplayStateText.text = gameplayDebugText.ToString();
        if (_testGameplayTimer != null) _testGameplayTimer.text = Mathf.Ceil(_shiftTimeLeft).ToString(); //rafamaster3
        if (_testGameplayShiftMoney != null) _testGameplayShiftMoney.text = "Hoy: $" + _currentShiftPayment.ToString(); //rafamaster3
        if (_testGameplayTotalMoney != null) _testGameplayTotalMoney.text = "Ahorrado: $" + _playerWallet.Balance.ToString(); //rafamaster3
    }

    public void _Gameplay_OnPlayerDeath()
    {
        if (!inShift) return;
        OnGameplayEnd();
        _gameOverDisplay.Set(_gameOverTitleText, _gameOverContextText);
        _GameOver_Display();
    }

    public void _Gameplay_OnTimeUp()
    {
        if (!inShift) return;
        OnGameplayEnd();
        _Home_Display();
        // _gameOverDisplay.Set("Game Over", "The truck left you behind");
        // _GameOver_Display();
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
        _playerSack.Clear();
        _playerCurrentWeight = 0;
        _playerSackCarrySpaceUsed = 0;
        _playerMoney += _currentShiftPayment;
        _playerWallet.AddMoney(_currentShiftPayment);
        _currentShiftPayment = 0;
        _oxygenManager.ResetOxygen(); //rafamaster3

        foreach (Spawner spawner in _spawners)
        {
            spawner.StopSpawning();
        }

        DestroyAllSpawnedValuables();
        _testGameplayScreen.SetActive(false);
        _onEndGameplay?.Invoke();
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
            _dayTimerLabel.text = _dayTimerText + ": " + Mathf.Ceil(_shiftTimeLeft);
            yield return null;
        }
        _shiftTimeLeft = 0;
        _dayTimerLabel.text = _dayTimerText + ": " + _shiftTimeLeft;
        _Gameplay_OnTimeUp();
    }

    void RunDayTimer()
    {
        if (dayTimeRoutine != null) StopCoroutine(dayTimeRoutine);
        dayTimeRoutine = StartCoroutine(Internal_RunDayTimer());
    }

    void StopDayTimer()
    {
        if (dayTimeRoutine != null) StopCoroutine(dayTimeRoutine);
    }

    void ResetDayTimer()
    {
        isInHurry = false;
        // _shiftTimeLeft = _shiftTimeDuration;
        _shiftTimeLeft = days[_currentDay-1].dayDuration;
    }

    void OnHurryUpDisplay()
    {

    }

    #endregion
    #region Home
    [Header("Home")]
    [SerializeField] GameObject _testHomeScreen;
    [SerializeField] TMP_Text _testHomeStateText;
    [SerializeField] UnityEvent _onHomeGoToNextDay;
    // [SerializeField] float _dayCost = 1200;
    bool isInHome;

    public void _Home_Display()
    {
        isInHome = true;
        _testHomeScreen.SetActive(true);
    }

    public void _Home_EndDay()
    {
        _testHomeScreen.SetActive(false);
        var quota = days[_currentDay - 1].dayQuota;
        if (_playerMoney >= quota)

        {
            _playerMoney -= quota;
            _playerWallet.TrySpend(quota); //rafamaster3
            _testGameplayTotalMoney.text = _playerWallet.Balance.ToString(); //rafamaster3

            if (_currentDay >= days.Length)
            {
                _gameOverDisplay.Set(_winTitleText, _winContextText);
                _GameOver_Display();

            }
            else
            {
                _Gameplay_Display();
                _onHomeGoToNextDay?.Invoke();
            }
        }
        else
        {
            _gameOverDisplay.Set(_badEndingTitleText, _badEndingContextText);
            _GameOver_Display();
        }
        isInHome = false;
    }

    public void _Home_UpdateDebugText()
    {
        if (!isInHome) return;
        var homeDebugText = new System.Text.StringBuilder();
        homeDebugText.AppendLine("HOME DEBUG TEXT");
        homeDebugText.AppendLine("───────────────────────────");
        homeDebugText.AppendLine("Current Money: " + _playerMoney);
        homeDebugText.AppendLine("───────────────────────────");
        homeDebugText.AppendLine("Day Quota: " + days[_currentDay - 1].dayQuota);

        if (_testHomeStateText != null) _testHomeStateText.text = homeDebugText.ToString();
    }

    #endregion
    #region Game Over
    [Header("Game Over")]
    [SerializeField] GameObject _testGameOverScreen;
    [SerializeField] string _winTitleText = "Good Ending";
    [SerializeField] string _winContextText = "Gimme some beer for the man! whooo";
    [SerializeField] string _gameOverTitleText = "Game Over";
    [SerializeField] string _gameOverContextText = "You died...";
    [SerializeField] string _badEndingTitleText = "Bad Ending";
    [SerializeField] string _badEndingContextText = "Your family starved...";
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
        _gameOverDisplay.SetEndingLabels(_gameOverTitle, _gameOverContext);
        _testGameOverScreen.SetActive(true);
    }

    public void _GameOver_GoToMainMenu()
    {
        _testGameOverScreen.SetActive(false);
        ResetGame();
        _MainMenu_Display();
    }

    #endregion

}
