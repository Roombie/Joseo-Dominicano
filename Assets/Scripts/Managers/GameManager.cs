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
    public static GameManager Instance { get; private set; }

    #region Music
    [SerializeField] private MusicSwitcher musicSwitcher;
    [SerializeField, Range(0f, 1f)] private float pausedMusicDuck = 0.5f; // Volume multiplier when paused
    #endregion

    #region General
    [Header("General")]
    [SerializeField] InputReader input;
    int _currentDay = 0;

    // [Serializable]
    // public struct Level
    // {
    //     public int dayQuota;
    //     public float dayDuration;
    // }
    [SerializeField] LevelDayConfig _dayTemplate;
    [SerializeField] LevelDayDB _days;

    public List<LevelDayConfig> days => _days.days;
    // public void SetNewDayConfig(List<LevelDayConfig> days) => _days.days = days;

    [Header("References")]
    [SerializeField] Rigidbody2D _playerPhysics;
    [SerializeField] PlayerSmoothMovement _playerMovement;
    [SerializeField] PlayerWallet _playerWallet; //rafamaster3
    [SerializeField] PlayerCollect _playerCollect;
    public OxygenManager _oxygenManager; //rafamaster3
    [SerializeField] UnityEvent _onStart;

    // CHECKPOINT

    [Header("Checkpoint (Session Only)")]
    [Tooltip("Checkpoint will be captured when this day starts (after _currentDay++ in _Gameplay_StartDay).")]
    [SerializeField] private int checkpointDay = 3;

    [Tooltip("Assign your GameOver 'Continue' button root(s) here. They will be active only if a checkpoint exists.")]
    [SerializeField] private List<GameObject> checkpointContinueButtonRoots = new List<GameObject>();

    [Serializable]
    private struct ShopItemCheckpoint
    {
        public ShopItemSO item;
        public bool isPurchased;
        public int currentLevel;
        public bool useLevels;
    }

    [Serializable]
    private class CheckpointData
    {
        public int resumeDay;
        public int walletBalance;
        public int carryCapacity;
        public List<ShopItemCheckpoint> shopItems = new List<ShopItemCheckpoint>();
    }

    private CheckpointData _checkpoint;

    // help

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        for (int i = 0; i < _days.days.Count; i++)
        {
            if (_days.days[i] == null) _days.days.RemoveAt(i);
        }

        // LevelDayConfig firstDefaultDay = new LevelDayConfig();
        LevelDayConfig firstDefaultDay = ScriptableObject.CreateInstance<LevelDayConfig>();
        firstDefaultDay.quota = _dayTemplate.quota;
        firstDefaultDay.duration = _dayTemplate.duration;
        firstDefaultDay.spawnableObjects = _dayTemplate.spawnableObjects;
        firstDefaultDay.minSpawnInterval = _dayTemplate.minSpawnInterval;
        firstDefaultDay.maxSpawnInterval = _dayTemplate.maxSpawnInterval;
        firstDefaultDay.moveSpeed = _dayTemplate.moveSpeed;
        firstDefaultDay.maxVerticalWiggle = _dayTemplate.maxVerticalWiggle;
        firstDefaultDay.maxWiggleRate = _dayTemplate.maxWiggleRate;
        if (_days.days.Count == 0) _days.days.Add(firstDefaultDay);

        _testGameplayScreen.SetActive(false);
        _testHomeScreen.SetActive(false);

        if (_drownedVariantRoot != null) _drownedVariantRoot.SetActive(false);
        if (_quotaVariantRoot != null) _quotaVariantRoot.SetActive(false);
        if (_victoryRoot != null) _victoryRoot.SetActive(false);

        _playerCollect?.onCollect.AddListener(OnValuableCollected);
        // _oxygenManager?.onOxygenDepleted.AddListener(_Gameplay_OnPlayerDeath);

        ResetShopPurchases();
        UpdateTotalCoinsUI();

        if (_playerWallet != null)
            _playerWallet.OnBalanceChanged += OnWalletChanged;

        input.PauseEvent += OnPause;
        input.EnablePlayer();

        if (_pauseMenuView != null)
            _pauseMenuView.onCloseAnimationFinished.AddListener(OnPauseMenuClosed);
        
        RefreshCheckpointButtons();
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

        if (_playerWallet != null)
            _playerWallet.OnBalanceChanged -= OnWalletChanged;
    }

    private void OnApplicationQuit()
    {
        // Requirement: checkpoint must be deleted when exiting the app
        ClearCheckpoint();
    }

    public void ResetGame()
    {
        ResetCarryCapacity();
        UpdateTotalCoinsUI();
        _currentDay = 0;

        // Dejar el wallet en 0
        if (_playerWallet != null && _playerWallet.Balance > 0)
            _playerWallet.TrySpend(_playerWallet.Balance);

        UpdateTotalCoinsUI();
        _playerSack.Clear();
        _playerCurrentWeight = 0;
        _isDead = false;
        _currentShiftPayment = 0;
        ResetShopPurchases();
        ResetInteractionLock();

        if (_playerMovement != null)
            _playerMovement.ForceStopSprint();
    }

    private void OnWalletChanged(int newBalance)
    {
        UpdateTotalCoinsUI();
        UpdateDayGoalUI();

        if (inShift)
        {
            int quota = _days.days[_currentDay - 1].quota;

            // Si el wallet está por debajo de la cuota, reseteamos wasAboveQuota
            if (newBalance < quota)
                wasAboveQuota = false;
        }

        RefreshAllShopItemsUI();
    }

    private void RefreshAllShopItemsUI()
    {
        var shopItemsUI = FindObjectsByType<ShopItemUI>(FindObjectsSortMode.None);
        foreach (var ui in shopItemsUI)
        {
            if (ui != null && ui.IsValid())
            {
                ui.ForceUpdate();
            }
        }
    }

    private void ResetShopPurchases()
    {
        var shopItems = Resources.FindObjectsOfTypeAll<ShopItemSO>();
        int resetCount = 0;

        foreach (var item in shopItems)
        {
            if (item == null) 
                continue;

            item.isPurchased = false;

            // If use levels, reset to level 0
            if (item.useLevels)
            {
                item.currentLevel = 0;
            }

            resetCount++;
        }

        Debug.Log($"Reseteados {resetCount} items de tienda (compras y niveles)");
    }

    private bool _interactionsLocked = false;
    public bool InteractionsLocked => _interactionsLocked;

    public void LockInteractionsAndCloseShops()
    {
        _interactionsLocked = true;

        // Cerrar todas las shops abiertas
        var shops = FindObjectsByType<ShopInteraction>(FindObjectsSortMode.None);
        foreach (var shop in shops)
        {
            if (shop != null && shop.IsShopOpen)
            {
                shop.Close();
            }
        }
    }

    public void ResetInteractionLock()
    {
        _interactionsLocked = false;
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
        _Gameplay_Display();
    }

    public void _MainMenu_QuitGame()
    {
        ClearCheckpoint();
        Application.Quit();
    }

    #region Extra Menus
    [SerializeField] private GameObject _optionsPanel;
    [SerializeField] private GameObject _optionsPausePanel;
    [SerializeField] private GameObject _creditsPanel;
    [SerializeField] private bool _pauseWhileInOptions = true;

    [SerializeField] private MenuView _pauseMenuView;
    [SerializeField] private MenuView _pauseOptionsMenuView;
    [SerializeField] private MenuView _mainOptionsMenuView;
    [SerializeField] private MenuView _mainCreditsMenuView;

    public void _MainMenu_OpenOptions()
    {
        // En menú principal: no hace falta pausar
        if (_mainOptionsMenuView != null)
        {
            _mainOptionsMenuView.Show();
        }
        else if (_optionsPanel)
        {
            _optionsPanel.SetActive(true);
        }

        GameObject root = _mainOptionsMenuView != null
            ? _mainOptionsMenuView.gameObject
            : _optionsPanel;

        if (root != null)
        {
            foreach (var t in root.GetComponentsInChildren<ToggleSettingHandler>(true))
                t.RefreshUI();
        }
    }

    public void _MainMenu_CloseOptions()
    {
        if (_mainOptionsMenuView != null)
        {
            _mainOptionsMenuView.Hide();
        }
        else if (_optionsPanel)
        {
            _optionsPanel.SetActive(false);
        }
    }

    public void _PauseMenu_OpenOptions()
    {
        if (inShift && !isPaused) _Gameplay_Pause();

        // Show options:
        if (_pauseOptionsMenuView != null)
            _pauseOptionsMenuView.Show();
        else if (_optionsPausePanel != null)
            _optionsPausePanel.SetActive(true);

        // Refresh UI toggles from the root of the options menu
        GameObject root = null;
        if (_pauseOptionsMenuView != null)
            root = _pauseOptionsMenuView.gameObject;
        else
            root = _optionsPausePanel;

        if (root != null)
        {
            foreach (var t in root.GetComponentsInChildren<ToggleSettingHandler>(true))
                t.RefreshUI();
        }
    }

    public void _PauseMenu_CloseOptions()
    {
        if (_pauseOptionsMenuView != null)
            _pauseOptionsMenuView.Hide(); // animated close
        else if (_optionsPausePanel != null)
            _optionsPausePanel.SetActive(false);

    }

    public void _MainMenu_OpenCredits()
    {
        if (_mainCreditsMenuView != null)
        {
            _mainCreditsMenuView.Show();
        }
        else if (_creditsPanel)
        {
            _creditsPanel.SetActive(true);
        }
    }

    public void _MainMenu_CloseCredits()
    {
        if (_mainCreditsMenuView != null)
        {
            _mainCreditsMenuView.Hide();
        }
        else if (_creditsPanel)
        {
            _creditsPanel.SetActive(false);
        }
    }
    #endregion

    #endregion

    #region Gameplay
    [Header("Gameplay")]

    [SerializeField] GameObject pauseButton;
    public GameObject PauseButton => pauseButton;

    [SerializeField] Vector3 _playerInitialPosition;
    [SerializeField] GameObject _testGameplayScreen;
    [SerializeField] GameObject _pausePanel;
    [SerializeField] TMP_Text _testGameplayStateText;
    [SerializeField] TMP_Text _playerSackLabel;
    [SerializeField] CanvasGroup _playerSackUI;
    [SerializeField] TMP_Text _testGameplayTimer;          //rafamaster3
    [SerializeField] TMP_Text _testGameplayShiftMoney;     //rafamaster3 (Now: "Hoy: _currentShiftPayment / Quota")
    [SerializeField] TMP_Text _testGameSacSpace;           //rafamaster3
    [SerializeField] TMP_Text _testGameplayCollectedItemInfo; //rafamaster3
    [SerializeField] TMP_Text _testGameplayTotalMoney;     //rafamaster3 ("Ahorrado: $X")
    [SerializeField] int collectedInfoDuration = 3;        //rafamaster3
    [SerializeField] float _onCollectedItemHoldDuration = 2;
    [SerializeField] float _onCollectedItemFadeDuration = 0.2f;

    private bool _pauseLocked = false;
    float _shiftTimeDuration = 30;
    [SerializeField] float _displayHurryUpOn = 10;
    float _shiftTimeLeft;

    // Lo ganado hoy (GROSS, solo depositado en este día, no restamos compras)
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
    [HideInInspector] public bool inShift;
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
    [SerializeField] GameObject _dayObject;
    [SerializeField] TMP_Text _dayHUDLabel;
    [SerializeField] GameObject _dayHUDObject;
    [SerializeField] GameObject _lastDayObject;
    [SerializeField] GameObject _lastHUDDayObject;
    [SerializeField] TMP_Text _dayGoalValue;
    [SerializeField] TMP_Text _dayTimerValue;
    [SerializeField] Color _dayGoalInsufficientColor = Color.red;
    [SerializeField] Color _dayGoalReachedColor = Color.green;
    [SerializeField] float _timesUpDisplayHomeDelay = 3;
    [SerializeField] UnityEvent _onGameplayHurryUp;
    [SerializeField] UnityEvent _onGameplayTimesUp;
    [SerializeField] UnityEvent _onPlay;
    [SerializeField] UnityEvent _onStartDay;
    [SerializeField] UnityEvent<LevelDayConfig> _onDayChange; 
    [SerializeField] UnityEvent _onEndGameplay;
    [HideInInspector] public bool isPaused;
    [SerializeField] private AudioClip pauseSFX;
    [SerializeField] private AudioClip resumeSFX;
    [SerializeField] private AudioClip depositItems;
    [SerializeField] private AudioClip goalCompleted;
    bool wasAboveQuota = false;

    [Header("Deposit Feedback")]
    [SerializeField] private TMP_Text totalCoinsCollectedText;
    [SerializeField] private GameObject _depositFeedbackUI;
    [SerializeField] private TMP_Text _depositFeedbackText;
    [SerializeField] private Animator _depositFeedbackAnimator;

    [SerializeField, Min(0f)] private float _depositHideDelay = 1f; // seconds after last deposit
    private Coroutine _depositHideRoutine;
    const string ANIM_RISE_UP = "Coins Obtained Rise Up";
    const string ANIM_RISE_DOWN = "Coins Obtained Rise Down";

    [HideInInspector] public bool _isDead = false;

    void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            if (inShift && !isPaused)
                _Gameplay_Pause();
        }
    }

    public void UpdateTotalCoinsUI()
    {
        if (totalCoinsCollectedText != null && _playerWallet != null)
            totalCoinsCollectedText.text = $"${_playerWallet.Balance}";
    }

    private void UpdateDayGoalUI()
    {
        if (!inShift || _currentDay <= 0) return;

        int wallet = _playerWallet.Balance;
        int quota = _days.days[_currentDay - 1].quota;;

        string goalValueColor = "#" + ColorUtility.ToHtmlStringRGBA(
            wallet >= quota ? _dayGoalReachedColor : _dayGoalInsufficientColor
        );

        _dayGoalValue.text = $"<color={goalValueColor}>${wallet}/${quota}</color>";
    }
    
    public void _Gameplay_DepositValuables()
    {
        if (!inShift) return;
        if (_playerWallet == null) return;

        int totalDepositWorth = 0;

        for (int i = 0; i < _playerSack.Count; i++)
        {
            var sackItem = _playerSack[i];

            // Lo ganado hoy (grosso)
            _currentShiftPayment += sackItem.Worth;
            totalDepositWorth += sackItem.Worth;

            _playerCurrentWeight -= sackItem.WeightKg;
            _playerSackCarrySpaceUsed -= sackItem.PickUpSpace;
        }

        _playerSack.Clear();
        lastRejectedValuable = null;

        // Primero añadir el dinero al wallet
        if (totalDepositWorth > 0)
            _playerWallet.AddMoney(totalDepositWorth);

        // Actualizar color y texto de meta con el WALLET actual
        int quota = _days.days[_currentDay - 1].quota;;

        // Solo si realmente depositaste algo
        if (totalDepositWorth > 0)
        {
            bool nowAboveQuota = _playerWallet.Balance >= quota;

            // Si antes NO estabas por encima y ahora sí → CRUZASTE LA META
            if (!wasAboveQuota && nowAboveQuota)
            {
                AudioManager.Instance?.Play(goalCompleted,SoundCategory.SFX);
            }

            // Sonido al depositar items (siempre que haya algo)
            AudioManager.Instance?.Play(depositItems, SoundCategory.SFX);

            // Actualizamos el estado para el próximo depósito
            wasAboveQuota = nowAboveQuota;
        }

        string goalValueColor = "#" + ColorUtility.ToHtmlStringRGBA(
            _playerWallet.Balance >= quota ? _dayGoalReachedColor : _dayGoalInsufficientColor
        );
        _dayGoalValue.text =
            $"<color={goalValueColor}>${_playerWallet.Balance}/${quota}</color>";
        
        // UI
        UpdateTotalCoinsUI();
        ShowDepositFeedback(totalDepositWorth);

        _playerSackLabel.text = _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit;

        if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
        WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());
    }

    public void _Gameplay_Display()
    {
        _isDead = false;
        musicSwitcher?.SwitchTo(MusicState.Gameplay, 0.15f);
        _testGameplayScreen.SetActive(true);   
        _playerPhysics.linearVelocity = Vector2.zero;
        _playerPhysics.angularVelocity = 0;
        _playerCollect.transform.position = _playerInitialPosition;
        _playerCollect.transform.rotation = Quaternion.identity;
        
        ShowDayBanner(true);

        _onPlay?.Invoke();
    }

    public void ShowDayBanner(bool show)
    {
        if (!show)
        {
            if (_dayObject != null) _dayObject.SetActive(false);
            if (_lastDayObject != null) _lastDayObject.SetActive(false);
            return;
        }

        int totalDays = _days.days.Count;
        int displayDay = _currentDay + 1; // el dia que va a comenzar

        bool isLastDay = displayDay >= totalDays;

        if (_dayObject != null) _dayObject.SetActive(!isLastDay);
        if (_lastDayObject != null) _lastDayObject.SetActive(isLastDay);

        if (_dayHUDObject != null) _dayHUDObject.SetActive(!isLastDay);
        if (_lastHUDDayObject != null) _lastHUDDayObject.SetActive(isLastDay);

        if (!isLastDay && _dayLabel != null)
            _dayLabel.text = $"{displayDay}/{totalDays}";

        if (!isLastDay && _dayHUDLabel != null)
            _dayHUDLabel.text = $"{displayDay}";
    }

    public void _Gameplay_StartDayOnDelay()
    {
        StartCoroutine(GameplayStartDayDelay());
    }

    public void DisallowPause(bool value)
    {
        _pauseLocked = value;
    }

    public void _Gameplay_SilentPause()
    {
        if (!inShift || isPaused || _interactionsLocked)
            return;

        Time.timeScale = 0;
        isPaused = true;

        _oxygenManager.PauseOxygen();
        StopDayTimer();

        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;
        if (musicOn)
            AudioManager.Instance?.SetVolume(SettingType.MusicEnabledKey, pausedMusicDuck, persist: false);
    }

    public void _Gameplay_SilentResume()
    {
        Time.timeScale = 1;
        isPaused = false;

        _oxygenManager.ConsumeOxygen();
        RunDayTimer();

        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;
        if (musicOn)
            AudioManager.Instance?.SetMuted(SettingType.MusicEnabledKey, false);
    }

    public void _Gameplay_Pause()
    {
        if (!inShift || isPaused || _interactionsLocked)
            return;

        isPaused = true;

        Time.timeScale = 0f;  // Animator on pause menu must use Unscaled Time
        AudioManager.Instance?.Play(pauseSFX, SoundCategory.SFX);

        if (_pauseMenuView != null)
            _pauseMenuView.Show();
        else if (_pausePanel != null)
            _pausePanel.SetActive(true);

        _oxygenManager.PauseOxygen();
        StopDayTimer();

        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;
        if (musicOn)
            AudioManager.Instance?.SetVolume(SettingType.MusicEnabledKey, pausedMusicDuck, persist: false);
    }

    public void _Gameplay_Resume()
    {
        if (!isPaused)
            return;

        if (_pauseMenuView != null)
        {
            _pauseMenuView.Hide();
        }
        else
        {
            if (_pausePanel != null)
                _pausePanel.SetActive(false);

            OnPauseMenuClosed();
        }
    }


    public void OnPauseMenuClosed()
    {
        // This is called by pauseMenuView.onCloseAnimationFinished
        Time.timeScale = 1f;
        AudioManager.Instance?.Play(resumeSFX, SoundCategory.SFX);

        _PauseMenu_CloseOptions(); // just in case
        _oxygenManager.ConsumeOxygen();
        RunDayTimer();
        isPaused = false;

        bool musicOn = PlayerPrefs.GetInt(SettingsKeys.MusicEnabledKey, 1) == 1;
        if (musicOn)
            AudioManager.Instance?.SetMuted(SettingType.MusicEnabledKey, false);
    }

    [SerializeField] private float _pauseDebounceSeconds = 0.15f;
    private Coroutine _pauseDebounceRoutine;

    private void PauseDebounce()
    {
        _pauseLocked = true;

        if (_pauseDebounceRoutine != null)
            StopCoroutine(_pauseDebounceRoutine);

        _pauseDebounceRoutine = StartCoroutine(PauseDebounceRoutine());
    }

    private IEnumerator PauseDebounceRoutine()
    {
        yield return new WaitForSecondsRealtime(_pauseDebounceSeconds);
        _pauseLocked = false;
        _pauseDebounceRoutine = null;
    }

    void OnPause()
    {
        if (_pauseLocked || _interactionsLocked)
            return;

        PauseDebounce();

        // Si estamos en gameplay, esto funciona como PAUSE/BACK in-game
        if (inShift)
        {
            // No está pausado aún → abrir menú de pausa
            if (!isPaused)
            {
                _Gameplay_Pause();
                return;
            }

            // Ya está pausado → ver si las opciones del PAUSE están abiertas
            bool pauseOptionsOpen = false;

            if (_pauseOptionsMenuView != null)
                pauseOptionsOpen = _pauseOptionsMenuView.gameObject.activeInHierarchy;
            else if (_optionsPausePanel != null)
                pauseOptionsOpen = _optionsPausePanel.activeSelf;

            if (pauseOptionsOpen)
            {
                // Primer pulsación mientras estás en opciones de pausa → cerrar SOLO opciones
                _PauseMenu_CloseOptions();
                return;
            }

            // Si no hay opciones de pausa abiertas → cerrar el menú de pausa (con animación)
            _Gameplay_Resume();
            return;
        }

        // Si NO estamos en gameplay, usar el mismo botón como "Back" en el menú principal
        OnMainMenuBack();
    }

    public void OnMainMenuBack()
    {
        // Don’t run in gameplay or pause
        if (inShift || isPaused)
            return;

        // Only handle this if main menu is actually visible
        if (_testMainMenu == null || !_testMainMenu.activeInHierarchy)
            return;

        // Close main menu credits if open
        bool creditsOpen = false;
        if (_mainCreditsMenuView != null)
            creditsOpen = _mainCreditsMenuView.gameObject.activeInHierarchy;
        else if (_creditsPanel != null)
            creditsOpen = _creditsPanel.activeSelf;

        if (creditsOpen)
        {
            _MainMenu_CloseCredits();
            return;
        }

        // Close main menu options if open
        bool optionsOpen = false;
        if (_mainOptionsMenuView != null)
            optionsOpen = _mainOptionsMenuView.gameObject.activeInHierarchy;
        else if (_optionsPanel != null)
            optionsOpen = _optionsPanel.activeSelf;

        if (optionsOpen)
        {
            _MainMenu_CloseOptions();
            return;
        }
    }

    public void _Gameplay_GoToMenu()
    {
        ClearCheckpoint();

        isPaused = false;
        Time.timeScale = 1;

        if (_pauseMenuView != null)
            _pauseMenuView.gameObject.SetActive(false);
        else if (_pausePanel != null)
            _pausePanel.SetActive(false);

        if (inShift)
            OnGameplayEnd(); // clear gameplay state

        ResetGame();         // clear progress, economy, purchases
        _MainMenu_Display(); // show main menu
    }

    IEnumerator GameplayStartDayDelay()
    {
        yield return new WaitForSecondsRealtime(_startDayDelay);
        _Gameplay_StartDay();
    }

    public void _Gameplay_StartDay()
    {
        if (inShift) return;
        ResetInteractionLock();

        inShift = true;
        wasAboveQuota = false;
        _currentDay++;
        
        TryCaptureCheckpointIfNeeded();

        _onDayChange?.Invoke(_days.days[_currentDay - 1]);
        ResetDayTimer();
        RunDayTimer();
        _oxygenManager.ResetOxygen();

        _playerSackDebugOutput = "Clear";
        _currentShiftPayment = 0;

        int quota = _days.days[_currentDay - 1].quota;;
        int wallet = _playerWallet != null ? _playerWallet.Balance : 0;

        string goalValueColor = "#" + ColorUtility.ToHtmlStringRGBA(
            wallet >= quota ? _dayGoalReachedColor : _dayGoalInsufficientColor
        );
        _dayGoalValue.text = $"<color={goalValueColor}>${wallet}/${quota}</color>";

        _onStartDay?.Invoke();
        
        input.EnablePlayer();

        if (_playerMovement != null)
        {
            _playerMovement.ResetMove();
            _playerMovement.ResetSprintState();
            _playerMovement.SetMobileControlsForGameplay(true);  
        }
    }

    TrashPickup lastRejectedValuable;

    void OnValuableCollected(TrashPickup pickup)
    {
        if (!inShift || pickup == null) return;

        var so = pickup.Item;
        if (so == null) return;

        // If this is the same rejected item as last time OR a worthless item, ignore
        if ((lastRejectedValuable != null && pickup == lastRejectedValuable) ||
            (so.Worth == 0 && so.PickUpSpace == 0))
        {
            return;
        }

        int freeSpace = _playerSackCarrySpaceLimit - _playerSackCarrySpaceUsed;

        // Enough space → accept item and DESPAWN via pool
        if (so.PickUpSpace <= freeSpace)
        {
            _playerSack.Add(so);
            _playerCurrentWeight += so.WeightKg;
            _playerSackCarrySpaceUsed += so.PickUpSpace;
            _playerSackDebugOutput = $"\"{so.name}\" added ";

            bool isFull = _playerSackCarrySpaceUsed == _playerSackCarrySpaceLimit;

            if (isFull)
            {
                _testGameplayCollectedItemInfo.text = "FULL";
                _testGameplayCollectedItemInfo.gameObject.SetActive(true);
                Invoke(nameof(HideInfoText), collectedInfoDuration);

                _playerSackLabel.text =
                    $"<color=yellow>({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit})</color>";

                if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
                WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());

                _playerSackDebugOutput += "(FULL)";
                // No need to set lastRejectedValuable here, the item will despawn
            }
            else
            {
                _playerSackLabel.text =
                    $"{_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit}";

                if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
                WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());

                _testGameplayCollectedItemInfo.text =
                    $"{so.name} ${so.Worth} {so.WeightKg:0.##}kg {so.PickUpSpace}L";
                _testGameplayCollectedItemInfo.gameObject.SetActive(true);
                Invoke(nameof(HideInfoText), collectedInfoDuration);

                _playerSackDebugOutput +=
                    $"({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit})";
            }

            // Play pickup SFX
            if (so.PickUpSound != null)
            {
                float minPitch = so.MinRandomPitch;
                float maxPitch = so.MaxRandomPitch;

                if (maxPitch < minPitch)
                {
                    var t = minPitch;
                    minPitch = maxPitch;
                    maxPitch = t;
                }

                float pitch = UnityEngine.Random.Range(minPitch, maxPitch);
                AudioManager.Instance?.Play(
                    so.PickUpSound,
                    SoundCategory.SFX,
                    volume: 1f,
                    pitch: pitch,
                    loop: false
                );
            }

            // despawn via TrashPickup to pool 
            pickup.OnCollected();
        }
        else
        {
            // Not enough space → just show feedback, do NOT despawn
            if (_playerSackCarrySpaceUsed == _playerSackCarrySpaceLimit)
                _playerSackLabel.text =
                    $"<color=yellow>({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit})</color>";
            else
                _playerSackLabel.text =
                    $"<color=red>({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit}) +{so.PickUpSpace}</color>";

            if (WhenItemCollectedRoutine != null) StopCoroutine(WhenItemCollectedRoutine);
            WhenItemCollectedRoutine = StartCoroutine(playerSackWhenItemCollectedRoutine());

            lastRejectedValuable = pickup;
            _playerSackDebugOutput =
                $"Not enough space to collect \"{so.name}\" ({so.PickUpSpace} in ({_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit}))";
        }
    }

    void HideInfoText() => _testGameplayCollectedItemInfo.gameObject.SetActive(false);

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
            _depositFeedbackAnimator.Play(ANIM_RISE_UP, -1, 0f);
        }

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
            gameplayDebugText.AppendLine("Shift Time Left: " +
                Mathf.Ceil(_shiftTimeLeft) + "s " +
                (isInHurry ? "HURRY UP!!!" : ""));
            gameplayDebugText.AppendLine("Shift Payment (GROSS today): " + _currentShiftPayment);
            gameplayDebugText.AppendLine("───────────────────────────");
            gameplayDebugText.AppendLine("Player Sack Space: " +
                _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit);
            gameplayDebugText.AppendLine("Player Weight: " + _playerCurrentWeight);
            gameplayDebugText.AppendLine("───────────────────────────");
            gameplayDebugText.AppendLine("Player Sack Valuables: ");
            for (int i = 0; i < _playerSack.Count; i++)
            {
                var sackItem = _playerSack[i];
                gameplayDebugText.AppendLine((i + 1) + "- " + sackItem.name +
                    " - value: " + sackItem.Worth +
                    ", weight: " + sackItem.WeightKg);
            }
            gameplayDebugText.AppendLine("───────────────────────────");
            gameplayDebugText.AppendLine("Player Sack Update: " + _playerSackDebugOutput);
        }
        else
        {
            gameplayDebugText.AppendLine("- Start Day to Display Gameplay -");
        }

        if (_testGameplayStateText != null)
            _testGameplayStateText.text = gameplayDebugText.ToString();

        if (_testGameplayTimer != null)
            _testGameplayTimer.text = Mathf.Ceil(_shiftTimeLeft).ToString();

        if (_testGameplayShiftMoney != null && _currentDay > 0)
        {
            int quota = _days.days[_currentDay - 1].quota;
            _testGameplayShiftMoney.text =
                $"Hoy: ${_currentShiftPayment} / Meta ${quota}";
        }

        if (_testGameplayTotalMoney != null && _playerWallet != null)
            _testGameplayTotalMoney.text = "Ahorrado: $" + _playerWallet.Balance.ToString();

        if (_playerSackCarrySpaceUsed >= _playerSackCarrySpaceLimit)
        {
            if (_testGameSacSpace != null)
                _testGameSacSpace.text = "Saco lleno, sube";
        }
        else
        {
            if (_testGameSacSpace != null)
                _testGameSacSpace.text =
                    "Carga: \n" + _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit;
        }
    }

    public void _Gameplay_OnPlayerDeath()
    {
        if (!inShift || _isDead) return;
        _isDead = true;
        isInHurry = false;

        musicSwitcher?.SetMusicAudible(false, 0.25f);
        OnGameplayEnd();
        ShowEnding(GameEndingType.Drowned);
        _GameOver_Display();
    }

    public void _Gameplay_OnTimeUp()
    {
        if (!inShift) return;
        LockInteractionsAndCloseShops();
        OnGameplayEnd();
        _onGameplayTimesUp?.Invoke();
        Invoke("_Home_Display", _timesUpDisplayHomeDelay);
    }

    // just for testing
    public void _Gameplay_End()
    {
        if (!inShift) return;
        isInHurry = false;
        LockInteractionsAndCloseShops();
        input.EnableUI();
        input.DisablePlayer();
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
        _shiftTimeLeft = 0;
        _oxygenManager.ResetOxygen();

        if (_playerMovement != null)
        {
            _playerMovement.ResetMove();
            _playerMovement.ForceStopSprint();
            _playerMovement.SetMobileControlsForGameplay(false);
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
        _shiftTimeLeft = _days.days[_currentDay - 1].duration;
    }

    void OnHurryUpDisplay()
    {
        _onGameplayHurryUp?.Invoke();
        musicSwitcher?.SwitchTo(MusicState.Hurry, 0.15f);
    }

    public void IncreasePlayerCarryCapacity(int amount)
    {
        _playerSackCarrySpaceLimit += amount;
        if (_playerSackCarrySpaceLimit < 1)
            _playerSackCarrySpaceLimit = 1;

        RefreshCarrySpaceUI();
    }

    public void RefreshCarrySpaceUI()
    {
        if (_playerSackLabel != null)
            _playerSackLabel.text =
                $"{_playerSackCarrySpaceUsed}/{_playerSackCarrySpaceLimit}";

        if (_testGameSacSpace)
            _testGameSacSpace.text =
                _playerSackCarrySpaceUsed + "/" + _playerSackCarrySpaceLimit;
    }

    public void ResetCarryCapacity()
    {
        _playerSackCarrySpaceLimit = 3;
        _playerSackCarrySpaceUsed = 0;
        RefreshCarrySpaceUI();
    }

    public void LoseSackSpaceAndItems(int minLoss, int maxLoss)
    {
        if (_playerSack.Count == 0)
        {
            // No hay nada que perder
            _playerSackCarrySpaceUsed = 0;
            RefreshCarrySpaceUI();
            return;
        }

        if (maxLoss < minLoss)
            maxLoss = minLoss;

        int spaceToLose = UnityEngine.Random.Range(minLoss, maxLoss + 1);
        int remainingSpaceToRemove = spaceToLose;

        // Delete random items until the space to lose is covered
        while (remainingSpaceToRemove > 0 && _playerSack.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, _playerSack.Count);
            TrashItemSO item = _playerSack[index];

            remainingSpaceToRemove -= item.PickUpSpace;

            _playerCurrentWeight -= item.WeightKg;
            _playerSackCarrySpaceUsed -= item.PickUpSpace;

            if (_playerSackCarrySpaceUsed < 0)
                _playerSackCarrySpaceUsed = 0;

            _playerSack.RemoveAt(index);
        }

        RefreshCarrySpaceUI();

        Debug.Log($"Hazard stole items worth {spaceToLose} sack space.");
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

        input.EnableUI();
        input.DisablePlayer();

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

        int wallet = _playerWallet != null ? _playerWallet.Balance : 0;
        int quota = _days.days[_currentDay - 1].quota;
        int result = wallet - quota;

        var valueMap = new Dictionary<string, Func<string>>
        {
            ["money"] = () => $"${_playerWallet.Balance}",
            ["quota"] = () => $"${quota}",
            ["remaining"] = () =>
            {
                int currentWallet = _playerWallet.Balance;
                int currentResult = currentWallet - quota;
                string sign = currentResult > 0 ? "" : "";
                return $"{sign}${currentResult}";
            }
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
        yield return null;
        homeStatsUI.DisplayStats();
    }

    bool CanPayQuota()
    {
        int quota = _days.days[_currentDay - 1].quota;
        return _playerWallet != null && _playerWallet.Balance >= quota;
    }

    public void _Home_EndDay()
    {
        _testHomeScreen.SetActive(false);

        input.DisableUI();
        input.EnablePlayer();

        // If cannot pay quota → BAD END
        if (!CanPayQuota())
        {
            ResetShopPurchases();
            isInHurry = false;
            musicSwitcher?.SetMusicAudible(false, 0.25f);
            ShowEnding(GameEndingType.NotReachedQuota);
            _GameOver_Display();
            isInHome = false;
            return;
        }

        int quota = _days.days[_currentDay - 1].quota;

        // Pay the quota from wallet (global economy system)
        _playerWallet.TrySpend(quota);

        UpdateTotalCoinsUI();

        if (_testGameplayTotalMoney != null)
            _testGameplayTotalMoney.text = "Ahorrado: $" + _playerWallet.Balance;

        // If last day, good ending
        if (_currentDay >= _days.days.Count)
        {
            ResetShopPurchases();
            ShowEnding(GameEndingType.GoodEnding);
            _GameOver_Display();
        }
        else
        {
            _Gameplay_Display();
            _onHomeGoToNextDay?.Invoke();
        }

        isInHome = false;
    }
    #endregion

    #region Ending Screens
    public enum GameEndingType
    {
        Drowned,
        NotReachedQuota,
        GoodEnding
    }

    [SerializeField] GameObject _drownedVariantRoot;
    [SerializeField] GameObject _quotaVariantRoot;
    [SerializeField] GameObject _victoryRoot;

    public void ShowEnding(GameEndingType type)
    {
        _drownedVariantRoot.SetActive(false);
        _quotaVariantRoot.SetActive(false);
        _victoryRoot.SetActive(false);

        switch (type)
        {
            case GameEndingType.Drowned:
                _drownedVariantRoot.SetActive(true);
                break;

            case GameEndingType.NotReachedQuota:
                _quotaVariantRoot.SetActive(true);
                break;

            case GameEndingType.GoodEnding:
                _victoryRoot.SetActive(true);
                break;
        }
    }

    public void _GameOver_Display()
    {
        input.EnableUI();
        input.DisablePlayer();
        musicSwitcher?.SetMusicAudible(false, 0.25f);
        Time.timeScale = 0f;
        RefreshCheckpointButtons();
    }

    public void _GameOver_GoToMainMenu()
    {
        ClearCheckpoint();

        Time.timeScale = 1f;

        if (_drownedVariantRoot != null) _drownedVariantRoot.SetActive(false);
        if (_quotaVariantRoot != null) _quotaVariantRoot.SetActive(false);
        if (_victoryRoot != null) _victoryRoot.SetActive(false);

        musicSwitcher?.SetMusicAudible(true, 0.25f);
        ResetShopPurchases();
        ResetGame();
        _MainMenu_Display();
    }
    
    // Checkpoint action
    public void _GameOver_ContinueFromCheckpoint()
    {
        if (_checkpoint == null)
        {
            _GameOver_GoToMainMenu();
            return;
        }

        // Close ending variants
        if (_drownedVariantRoot != null) _drownedVariantRoot.SetActive(false);
        if (_quotaVariantRoot != null) _quotaVariantRoot.SetActive(false);
        if (_victoryRoot != null) _victoryRoot.SetActive(false);

        // Make sure time is running
        Time.timeScale = 1f;

        // Ensure input is back to gameplay
        input.DisableUI();
        input.EnablePlayer();

        // Re-enable music (death mutes it)
        musicSwitcher?.SetMusicAudible(true, 0.15f);

        // Defensive: close pause/options if they were open
        if (_pauseMenuView != null) _pauseMenuView.gameObject.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(false);

        _PauseMenu_CloseOptions(); // safe call even if not open

        // Reset runtime flags
        isPaused = false;
        _isDead = false;
        isInHome = false;
        isInHurry = false;

        // Restore snapshot (wallet, carry capacity, shop state)
        RestoreCheckpoint(_checkpoint);

        // Prepare to start the saved day fresh:
        // _Gameplay_StartDay does _currentDay++, so set it to resumeDay - 1
        inShift = false;
        _currentDay = Mathf.Max(0, _checkpoint.resumeDay - 1);

        // Clear shift runtime variables
        _playerSack.Clear();
        _playerCurrentWeight = 0;
        _playerSackCarrySpaceUsed = 0;
        _shiftTimeLeft = 0;
        _currentShiftPayment = 0;
        wasAboveQuota = false;

        ResetInteractionLock();

        // Restart gameplay
        _Gameplay_Display();
        _onHomeGoToNextDay?.Invoke();
    }
    
    // Checkppint Interanals
    private void TryCaptureCheckpointIfNeeded()
    {
        if (_checkpoint != null) return;
        if (checkpointDay <= 0) return;

        if (_currentDay == checkpointDay)
        {
            CaptureCheckpoint();
        }
    }

    private void CaptureCheckpoint()
    {
        var data = new CheckpointData();
        data.resumeDay = _currentDay;
        data.walletBalance = _playerWallet != null ? _playerWallet.Balance : 0;
        data.carryCapacity = _playerSackCarrySpaceLimit;
        data.shopItems = new List<ShopItemCheckpoint>();

        // Capture all ShopItemSO states (purchase + levels).
        var allShopItems = Resources.FindObjectsOfTypeAll<ShopItemSO>();
        foreach (var item in allShopItems)
        {
            if (item == null) continue;

            var st = new ShopItemCheckpoint
            {
                item = item,
                isPurchased = item.isPurchased,
                currentLevel = item.useLevels ? item.currentLevel : 0,
                useLevels = item.useLevels
            };

            data.shopItems.Add(st);
        }

        _checkpoint = data;
        RefreshCheckpointButtons();
        Debug.Log($"[Checkpoint] Captured at Day {_checkpoint.resumeDay}");
    }

    private void RestoreCheckpoint(CheckpointData data)
    {
        if (data == null) return;

        // Restore wallet balance through AddMoney/TrySpend to keep wallet events consistent.
        if (_playerWallet != null)
        {
            int current = _playerWallet.Balance;
            int target = Mathf.Max(0, data.walletBalance);
            int diff = target - current;

            if (diff > 0)
                _playerWallet.AddMoney(diff);
            else if (diff < 0)
                _playerWallet.TrySpend(-diff);
        }

        // Restore carry capacity.
        _playerSackCarrySpaceLimit = Mathf.Max(1, data.carryCapacity);

        // Restore shop items.
        foreach (var st in data.shopItems)
        {
            if (st.item == null) continue;

            st.item.isPurchased = st.isPurchased;

            if (st.useLevels)
                st.item.currentLevel = Mathf.Max(0, st.currentLevel);
            else
                st.item.currentLevel = 0;
        }

        // Refresh UI and shop visuals.
        UpdateTotalCoinsUI();
        RefreshCarrySpaceUI();
        RefreshAllShopItemsUI();
        RefreshCheckpointButtons();
    }

    private void ClearCheckpoint()
    {
        _checkpoint = null;
        RefreshCheckpointButtons();
        Debug.Log("[Checkpoint] Cleared.");
    }

    private void RefreshCheckpointButtons()
    {
        bool show = _checkpoint != null;

        if (checkpointContinueButtonRoots == null)
            return;

        foreach (var go in checkpointContinueButtonRoots)
        {
            if (go == null)
            {
                Debug.LogWarning("[Checkpoint] Continue root is NULL in list.");
                continue;
            }

            go.SetActive(show);
        }
    }

    #endregion
}