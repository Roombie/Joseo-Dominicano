using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

public class GameManager : MonoBehaviour
{
    #region Music
    [SerializeField] private MusicSwitcher musicSwitcher;
    [SerializeField, Range(0f, 1f)] private float pausedMusicDuck = 0.5f; // Volume multiplier when paused
    #endregion

    #region General
    [Header("General")]
    [SerializeField] InputReader input;
    int _playerMoney;
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
    [SerializeField] Rigidbody2D _playerPhysics;
    [SerializeField] PlayerWallet _playerWallet; //rafamaster3
    [SerializeField] PlayerCollect _playerCollect;
    [SerializeField] OxygenManager _oxygenManager; //rafamaster3
    [SerializeField] List<Spawner> _spawners; //rafamaster3-modified
    [SerializeField] UnityEvent _onStart;

    void Awake()
    {
        // _testMainMenu.SetActive(true);
        _testGameplayScreen.SetActive(false);
        _testHomeScreen.SetActive(false);
        _testGameOverScreen.SetActive(false);
        _playerCollect?.onCollect.AddListener(OnValuableCollected);
        // _oxygenManager?.onOxygenDepleted.AddListener(_Gameplay_OnPlayerDeath);
        UpdateTotalCoinsUI(0);
        input.PauseEvent += OnPause;
        input.EnablePlayer();

        foreach (Spawner spawner in _spawners)
        {
            spawner?.onSpawn.AddListener(AddSpawnedValuable);
        }
    }

    void Start()
    {
        _onStart?.Invoke();
    }

    void Update()
    {
        _Gameplay_UpdateDebugText();
    }

    void OnDestroy()
    {
        _playerCollect?.onCollect.RemoveListener(OnValuableCollected);
        // _oxygenManager?.onOxygenDepleted.RemoveListener(_Gameplay_OnPlayerDeath);
        input.PauseEvent -= OnPause;

        foreach (Spawner spawner in _spawners)
        {
            spawner?.onSpawn.RemoveListener(AddSpawnedValuable);
        }
    }

    public void ResetGame()
    {
        // RESET
        UpdateTotalCoinsUI(0);
        _currentDay = 0;
        _playerWallet.TrySpend(_playerWallet.Balance);
        _playerMoney = 0;
        _playerSack.Clear();
        _playerCurrentWeight = 0;
    }

    #endregion
    #region Main Menu
    [Header("Main Menu")]
    [SerializeField] GameObject _testMainMenu;
    [SerializeField] UnityEvent _onMenuDisplay;

    public void _MainMenu_Display()
    {
        musicSwitcher?.SwitchTo(MusicState.Menu, 0.15f);
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

    #region Extra Menus
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private GameObject _optionsPausePanel;
    [SerializeField] private GameObject _creditsPanel;
    [SerializeField] private bool _pauseWhileInOptions = true;

    public void _MainMenu_OpenOptions()
    {
        if (inShift && !isPaused) _Gameplay_Pause();

        if (_optionsPanel) _optionsPanel.SetActive(true);

        // Make sure toggles reflect saved state right away:
        foreach (var t in _optionsPanel.GetComponentsInChildren<ToggleSettingHandler>(true))
            t.RefreshUI();
    }

    public void _MainMenu_CloseOptions()
    {
        if (_optionsPanel) _optionsPanel.SetActive(false);

        // If we paused for options during gameplay, resume
        if (_pauseWhileInOptions && inShift && isPaused) _Gameplay_Resume();
    }

    public void _PauseMenu_OpenOptions()
    {
        if (inShift && !isPaused) _Gameplay_Pause();

        if (_optionsPausePanel) _optionsPausePanel.SetActive(true);

        // Make sure toggles reflect saved state right away:
        foreach (var t in _optionsPausePanel.GetComponentsInChildren<ToggleSettingHandler>(true))
            t.RefreshUI();
    }

    public void _PauseMenu_CloseOptions()
    {
        if (_optionsPausePanel) _optionsPausePanel.SetActive(false);
    }

    public void _MainMenu_OpenCredits()
    {
        if (_creditsPanel) _creditsPanel.SetActive(true);
    }

    public void _MainMenu_CloseCredits()
    {
        if (_creditsPanel) _creditsPanel.SetActive(false);
    }
    #endregion

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
    [SerializeField] TMP_Text _testGameSacSpace; //rafamaster3
    [SerializeField] TMP_Text _testGameplayCollectedItemInfo; //rafamaster3
    [SerializeField] TMP_Text _testGameplayTotalMoney; //rafamaster3
    [SerializeField] int collectedInfoDuration = 3; //rafamaster3
    [SerializeField] float _onCollectedItemHoldDuration = 2;
    [SerializeField] float _onCollectedItemFadeDuration = 0.2f;
    float _shiftTimeDuration = 30;
    [SerializeField] float _displayHurryUpOn = 10;
    float _shiftTimeLeft;
    int _currentShiftPayment = 0;
    float _playerCurrentWeight = 0;
    public int _playerSackCarrySpaceLimit = 10;
    int _playerSackCarrySpaceUsed;
    [SerializeField] TrashItemSO[] _levelTrashItems;
    List<TrashItemSO> _playerSack = new List<TrashItemSO>();
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
    [SerializeField] TMP_Text _dayLabel;
    [SerializeField] TMP_Text _dayGoalValue;
    [SerializeField] TMP_Text _dayTimerValue;
    [SerializeField] Color _dayGoalInsufficientColor = Color.red;
    [SerializeField] Color _dayGoalReachedColor = Color.green;
    [SerializeField] float _timesUpDisplayHomeDelay = 3;
    [SerializeField] UnityEvent _onGameplayHurryUp;
    [SerializeField] UnityEvent _onGameplayTimesUp;
    [SerializeField] UnityEvent _onPlay;
    [SerializeField] UnityEvent _onStartDay;
    [SerializeField] UnityEvent _onEndGameplay;
    bool isPaused;

    [Header("Deposit Feedback")]
    [SerializeField,] private TMP_Text totalCoinsCollectedText;
    [SerializeField] private GameObject _depositFeedbackUI;
    [SerializeField] private TMP_Text _depositFeedbackText;
    [SerializeField] private Animator _depositFeedbackAnimator;

    [SerializeField, Min(0f)] private float _depositHideDelay = 1f; // seconds after last deposit
    private Coroutine _depositHideRoutine;
    const string ANIM_RISE_UP = "Coins Obtained Rise Up";
    const string ANIM_RISE_DOWN = "Coins Obtained Rise Down";

    private void UpdateTotalCoinsUI(int value)
    {
        if (totalCoinsCollectedText != null)
            totalCoinsCollectedText.text = $"${value}";
    }

    public void _Gameplay_DepositValuables()
    {
        if (!inShift) return;

        int totalDepositWorth = 0;

        for (int i = 0; i < _playerSack.Count; i++)
        {
            var sackItem = _playerSack[i];
            _currentShiftPayment += sackItem.Worth;
            totalDepositWorth += sackItem.Worth; // accumulate total worth of this deposit
            _playerCurrentWeight -= sackItem.WeightKg;
            _playerSackCarrySpaceUsed -= sackItem.PickUpSpace;
        }

        _playerSack.Clear();
        _playerSackDebugOutput = "Clear";
        lastRejectedValuable = null;
        string goalValueColor = "#" + ColorUtility.ToHtmlStringRGBA(_currentShiftPayment >= days[_currentDay - 1].dayQuota ? _dayGoalReachedColor : _dayGoalInsufficientColor);
        _dayGoalValue.text = $"<color={goalValueColor}>$" + _currentShiftPayment + "/$" + days[_currentDay - 1].dayQuota + "</color>";

        // Show/refresh the popup and (re)start the idle-to-hide timer
        UpdateTotalCoinsUI(_playerMoney + _currentShiftPayment);
        ShowDepositFeedback(totalDepositWorth);

        _playerSackLabel.text = _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit;
        if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
        WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());
    }

    public void _Gameplay_Display()
    {
        musicSwitcher?.SwitchTo(MusicState.Gameplay, 0.15f);
        _testGameplayScreen.SetActive(true);
        _playerPhysics.linearVelocity = Vector2.zero;
        _playerPhysics.angularVelocity = 0;
        _playerCollect.transform.position = _playerInitialPosition;
        _playerCollect.transform.rotation = Quaternion.identity;
        if (_dayLabel != null) _dayLabel.text = (_currentDay + 1).ToString();
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

        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;  // clave existente
        if (musicOn) AudioManager.Instance?.SetVolume(SettingType.MusicEnabledKey, pausedMusicDuck, persist: false);
    }

    public void _Gameplay_Resume()
    {
        Time.timeScale = 1;
        _pausePanel.SetActive(false);
        _oxygenManager.ConsumeOxygen();
        RunDayTimer();
        isPaused = false;

        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;
        if (musicOn) AudioManager.Instance?.SetMuted(SettingType.MusicEnabledKey, false);
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
        UpdateTotalCoinsUI(0);
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
        string goalValueColor = "#" + ColorUtility.ToHtmlStringRGBA(_currentShiftPayment >= days[_currentDay - 1].dayQuota ? _dayGoalReachedColor : _dayGoalInsufficientColor);
        _dayGoalValue.text = $"<color={goalValueColor}>$" + _currentShiftPayment + "/$" + days[_currentDay - 1].dayQuota + "</color>";
        _onStartDay?.Invoke();
    }

    TrashPickup lastRejectedValuable;

    void OnValuableCollected(TrashPickup pickup)
    {
        if (!inShift || pickup == null) return;

        var so = pickup.Item; // TrashItemSO
        if (so == null) return;

        // Evitar doble intento inmediato o basura sin “valor/espacio” (placeholder)
        if ((lastRejectedValuable != null && pickup == lastRejectedValuable) ||
            (so.Worth == 0 && so.PickUpSpace == 0))
        {
            return;
        }

        int freeSpace = _playerSackCarrySpaceLimit - _playerSackCarrySpaceUsed;
        if (so.PickUpSpace <= freeSpace)
        {
            // Añadir al saco
            _playerSack.Add(so);

            // Si tu unidad interna es gramos:
            // _playerCurrentWeight += Mathf.RoundToInt(so.WeightKg * 1000f);
            // Si es Kg (float/int):
            _playerCurrentWeight += so.WeightKg;

            _playerSackCarrySpaceUsed += so.PickUpSpace;
            _playerSackDebugOutput = $"\"{so.name}\" added ";

            bool isFull = _playerSackCarrySpaceUsed == _playerSackCarrySpaceLimit;

            if (isFull)
            {
                // Info “FULL”
                _testGameplayCollectedItemInfo.text = "FULL";
                _testGameplayCollectedItemInfo.gameObject.SetActive(true);
                Invoke(nameof(HideInfoText), collectedInfoDuration);

                _playerSackLabel.text = $"<color=yellow>({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit})</color>";
                if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
                WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());

                _playerSackDebugOutput += "(FULL)";
                lastRejectedValuable = pickup; // recordar el último rechazado para evitar spam
            }
            else
            {
                _playerSackLabel.text = $"{_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit}";
                if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
                WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());

                // Mostrar info del item recogido
                // Si llevas peso interno en gramos y quieres mostrar “g”: usa (so.WeightKg * 1000f).ToString("0")
                _testGameplayCollectedItemInfo.text =
                    $"{so.name} ${so.Worth} {so.WeightKg:0.##}kg {so.PickUpSpace}L";
                _testGameplayCollectedItemInfo.gameObject.SetActive(true);
                Invoke(nameof(HideInfoText), collectedInfoDuration);

                _playerSackDebugOutput += $"({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit})";
            }
            
            if (so.PickUpSound != null)
            {
                float minPitch = so.MinRandomPitch;
                float maxPitch = so.MaxRandomPitch;

                if (maxPitch < minPitch) { var t = minPitch; minPitch = maxPitch; maxPitch = t; }
                float pitch = UnityEngine.Random.Range(minPitch, maxPitch);

                AudioManager.Instance?.Play(so.PickUpSound, SoundCategory.SFX, volume: 1f, pitch: pitch, loop: false);
            }
            
            Destroy(pickup.gameObject);
        }
        else
        {
            // Sin espacio suficiente
            if (_playerSackCarrySpaceUsed == _playerSackCarrySpaceLimit)
                _playerSackLabel.text = $"<color=yellow>({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit})</color>";
            else
                _playerSackLabel.text = $"<color=red>({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit}) +{so.PickUpSpace}</color>";

            if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
            WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());

            lastRejectedValuable = pickup;
            _playerSackDebugOutput =
                $"Not enough space to collect \"{so.name}\" ({so.PickUpSpace} in ({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit}))";
        }
    }

    void HideInfoText() => _testGameplayCollectedItemInfo.gameObject.SetActive(false); //rafamaster3
    // void HidePlayerSack() => _playerSackUI.SetActive(false);

    Coroutine WhenItemCollectedRoutine;
    IEnumerator playerSackWhenItemCollectedRoutine()
    {

        float t = 0;
        float fadeDuration = 1 / _onCollectedItemFadeDuration;
        while (t < 1)
        {
            _playerSackUI.alpha = Mathf.Lerp(0, 1, t);
            t += fadeDuration * Time.deltaTime;
            yield return null;
        }
        t = 1;
        _playerSackUI.alpha = 1;
        yield return new WaitForSeconds(_onCollectedItemHoldDuration);

        while (t > 0)
        {
            _playerSackUI.alpha = Mathf.Lerp(0, 1, t);
            t -= fadeDuration * Time.deltaTime;
            yield return null;
        }

        _playerSackUI.alpha = 0;
        lastRejectedValuable = null;
    }

    private void ShowDepositFeedback(int totalDepositWorth)
    {
        if (totalDepositWorth <= 0) return;
        if (_depositFeedbackUI == null || _depositFeedbackText == null) return;

        _depositFeedbackUI.SetActive(true);
        _depositFeedbackText.text = "+" + totalDepositWorth;

        if (_depositFeedbackAnimator != null)
        {
            // Restart the “up” anim immediately
            _depositFeedbackAnimator.Play(ANIM_RISE_UP, -1, 0f);
        }

        // Restart the idle-to-hide timer every time you deposit
        RestartDepositHideTimer();
    }

    private void RestartDepositHideTimer()
    {
        if (_depositHideRoutine != null) StopCoroutine(_depositHideRoutine);
        _depositHideRoutine = StartCoroutine(DepositHideRoutine());
    }

    private IEnumerator DepositHideRoutine()
    {
        yield return new WaitForSeconds(_depositHideDelay);

        if (_depositFeedbackAnimator != null)
        {
            _depositFeedbackAnimator.Play(ANIM_RISE_DOWN, -1, 0f);
        }
    }

    public void HideDepositFeedback()
    {
        if (_depositFeedbackUI != null)
            _depositFeedbackUI.SetActive(false);
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
                gameplayDebugText.AppendLine((i + 1) + "- " + sackItem.name + " - value: " + sackItem.Worth + ", weight: " + sackItem.WeightKg);
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
        if (_testGameplayShiftMoney != null && _currentDay > 0) _testGameplayShiftMoney.text = "Today Goal: " + (_currentShiftPayment + _playerWallet.Balance).ToString() + "/" + days[_currentDay - 1].dayQuota.ToString(); //rafamaster3
        if (_testGameplayTotalMoney != null) _testGameplayTotalMoney.text = "Ahorrado: $" + _playerWallet.Balance.ToString(); //rafamaster3

        if (_playerSackCarrySpaceUsed >= _playerSackCarrySpaceLimit)
        {
            if (_testGameSacSpace != null) _testGameSacSpace.text = "Saco lleno, sube"; //rafamaster3
        }
        else
        {
        if (_testGameSacSpace != null) _testGameSacSpace.text =  "Carga: \n" +_playerSackCarrySpaceUsed.ToString() + "/" + _playerSackCarrySpaceLimit.ToString(); //rafamaster3

        }
    }

    public void _Gameplay_OnPlayerDeath()
    {
        if (!inShift) return;
        isInHurry = false;
        musicSwitcher?.SetMusicAudible(false, 0.25f);
        OnGameplayEnd();
        _gameOverDisplay.Set(_gameOverTitleText, _gameOverContextText);
        _GameOver_Display();
    }

    public void _Gameplay_OnTimeUp()
    {
        if (!inShift) return;
        OnGameplayEnd();
        _onGameplayTimesUp?.Invoke();
        Invoke("_Home_Display", _timesUpDisplayHomeDelay);
        // _gameOverDisplay.Set("Game Over", "The truck left you behind");
        // _GameOver_Display();
    }

    // just for testing
    public void _Gameplay_End()
    {
        if (!inShift) return;
        isInHurry = false;
        StopDayTimer();
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

        // DestroyAllSpawnedValuables();
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
            _dayTimerValue.text = Mathf.Ceil(_shiftTimeLeft).ToString();
            yield return null;
        }
        _shiftTimeLeft = 0;
        _dayTimerValue.text = Mathf.Ceil(_shiftTimeLeft).ToString();
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
        _onGameplayHurryUp?.Invoke();
        musicSwitcher?.SwitchTo(MusicState.Hurry, 0.15f);
    }

    #endregion
    
    #region Home
    [Header("Home")]
    [SerializeField] GameObject _testHomeScreen;
    [SerializeField] TMP_Text _testHomeStateText;
    [SerializeField] UnityEvent _onHomeGoToNextDay;
    [SerializeField] private HomeStatsUI homeStatsUI;
    bool isInHome;

    public void _Home_Display()
    {
        isInHome = true;
        _testHomeScreen.SetActive(true);

        if (_testHomeScreen == null)
        {
            Debug.LogError("[GameManager] Home screen reference is missing!");
            return;
        }

        _testHomeScreen.SetActive(true);

        if (homeStatsUI == null)
        {
            Debug.LogError("[GameManager] HomeStatsUI reference is missing!");
            return;
        }

        if (homeStatsUI.stats == null || homeStatsUI.stats.Count == 0)
        {
            Debug.LogWarning("[GameManager] No stats configured in HomeStatsUI.");
            return;
        }

        var valueMap = new Dictionary<string, Func<string>>
        {
            ["money"] = () => _playerMoney.ToString(),
            ["quota"] = () => days[_currentDay - 1].dayQuota.ToString(),
            ["remaining"] = () =>
                Mathf.Max(0, _playerMoney - days[_currentDay - 1].dayQuota).ToString()
        };

        foreach (var stat in homeStatsUI.stats)
        {
            if (valueMap.TryGetValue(stat.id.ToLower(), out var getter))
                stat.valueGetter = getter;
            else
                stat.valueGetter = () => "-";
        }
        
        StartCoroutine(ShowHomeStatsDelayed());
    }

    private IEnumerator ShowHomeStatsDelayed()
    {
        // Wait one frame to allow UI activation
        yield return null;

        // Now safe to call DisplayStats()
        homeStatsUI.DisplayStats();
    }

    public void _Home_EndDay()
    {
        _testHomeScreen.SetActive(false);
        var quota = days[_currentDay - 1].dayQuota;

        if (_playerMoney >= quota)
        {
            _playerMoney -= quota;
            UpdateTotalCoinsUI(_playerMoney);
            _playerWallet.TrySpend(quota);
            _testGameplayTotalMoney.text = _playerWallet.Balance.ToString();

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
            isInHurry = false;
            musicSwitcher?.SetMusicAudible(false, 0.25f);
            _gameOverDisplay.Set(_badEndingTitleText, _badEndingContextText);
            _GameOver_Display();
        }

        isInHome = false;
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
        musicSwitcher?.SetMusicAudible(true, 0.25f);
        ResetGame();
        _MainMenu_Display();
    }
    #endregion
}
