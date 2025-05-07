using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Controls gameplay mechanics for GridWorld scene and manages interaction with ExperimentManager.
/// Handles trial flow, player/reward spawning, and data logging.
/// </summary>
public class GameController : MonoBehaviour
{
    #region Singleton Pattern
    public static GameController Instance { get; private set; }

    private void Awake()
    {
        // Implement the singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    #endregion


    #region Component References
    [SerializeField] private ExperimentManager experimentManager;
    [SerializeField] private GridWorldManager gridWorldManager;
    [SerializeField] private PracticeManager practiceManager;

    private PlayerSpawner playerSpawner;
    private RewardSpawner rewardSpawner;
    private GridManager gridManager;
    private CountdownTimer countdownTimer;
    private ScoreManager scoreManager;
    private LogManager logManager;
    // Cache component references for better performance
    // private readonly Dictionary<Type, UnityEngine.Component> cachedComponents =
    //     new Dictionary<Type, UnityEngine.Component>();
    #endregion

    #region Configuration
    [SerializeField] private float maxTrialDuration = 5f;
    private bool hasLoggedTrialOutcome = false;
    // [SerializeField] private float startTrialDelay = 0.1f;
    [SerializeField] private float endTrialDelay = 0.1f;
    [SerializeField] private int[] pressesPerEffortLevel = { 1, 2, 3 };
    private float decisionPhaseStartTime;

    #endregion

    #region State Tracking
    private GameObject currentPlayer;
    private GameObject currentReward;
    private Vector2 playerInitialPosition;
    private Vector2 playerFinalPosition;
    private float trialStartTime;
    private float trialEndTime;
    private float actionReactionTime;
    private int currentBlockNumber;
    private int currentTrialIndex;
    private int currentEffortLevel;
    private int currentPracticeTrialCount;

    private bool isTrialActive;
    private bool isTrialEnded;
    private bool rewardCollected;
    private bool scoreAdded;
    private bool isPracticingTrials;
    private bool isInitializingTrial;
    #endregion



    #region Unity Lifecycle Methods
    private void Update()
    {
        // Check for trial timeout if movement is active
        if (!hasLoggedTrialOutcome && (Time.time - trialStartTime > maxTrialDuration))
        {
            Debug.Log("Trial timed out - logging movement failure");
            hasLoggedTrialOutcome = true;

            // Call movement failure logging
            PlayerController.Instance?.LogMovementFailure();

            // Handle trial end - but ONLY if the trial isn't already ending
            if (isTrialActive && !isTrialEnded)
            {
                EndTrial(false);
            }
        }
    }

    private bool IsPracticeMode()
    {
        return PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
    }

    private void Start()
    {
        // Ensure we're in the correct trial mode
        ValidateTrialMode();

        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Initialize ExperimentManager and GridWorldManager
        experimentManager = ExperimentManager.Instance;
        // gridWorldManager = GridWorldManager.Instance;

        // Get initial block and trial information from ExperimentManager
        currentBlockNumber = experimentManager.GetCurrentBlockNumber();
        currentTrialIndex = experimentManager.GetCurrentTrialIndex();
        currentEffortLevel = (int)experimentManager.GetCurrentTrialEV();

        // Set current effort level and presses per step
        SetPressesPerStep(currentEffortLevel);

        // Initialize components
        Initialize();
        InitializeLogManager();
        LoadCalibratedPressesPerEffortLevel();

        // Start the trial
        // StartCoroutine(StartTrial());

        // Only initialize components if we're actually in GridWorld scene
        if (SceneManager.GetActiveScene().name == "GridWorld" ||
            SceneManager.GetActiveScene().name == "PracticeGridWorld")
        {
            Initialize();
            InitializeLogManager();
            LoadCalibratedPressesPerEffortLevel();
            StartCoroutine(SetupGridWorldScene());
        }

        experimentManager.OnTrialEnded += OnTrialEnd;


        // Ensure movement is enabled if we're in GridWorld scene
        if (SceneManager.GetActiveScene().name == "GridWorld")
        {
            StartCoroutine(SetupGridWorldScene());
        }

        if (SceneManager.GetActiveScene().name == "PracticePhase")
        {
            InitializePracticeTrials();
        }

        // Ensure movement is enabled if we're in GridWorld scene
        if (SceneManager.GetActiveScene().name == "PracticeGridWorld")
        {
            Debug.Log("Attempting to setup PracticeGridWorld scene");
            StartCoroutine(SetupPracticeGridWorldScene());
        }

        // Retrieve effort level for practice trials
        if (isPracticingTrials)
        {
            currentEffortLevel = PlayerPrefs.GetInt("CurrentPracticeEffortLevel", currentEffortLevel);
            Debug.Log($"Retrieved Practice Trial Effort Level: {currentEffortLevel}");
        }

    }

    private void ValidateTrialMode()
    {
        // If we're in GridWorld scene (not PracticeGridWorld)
        if (SceneManager.GetActiveScene().name == "GridWorld")
        {
            // Ensure we're in formal trial mode
            if (PlayerPrefs.GetInt("IsFormalTrial", 0) != 1)
            {
                Debug.LogWarning("Formal trial scene detected but not in formal trial mode. Correcting...");
                PlayerPrefs.DeleteKey("IsPracticeTrial");
                PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");
                PlayerPrefs.DeleteKey("CurrentPracticeTrial");
                PlayerPrefs.DeleteKey("PracticeTrialCount");
                PlayerPrefs.DeleteKey("LastPracticeScore");
                PlayerPrefs.DeleteKey("PracticeModeActive");
                PlayerPrefs.SetInt("IsFormalTrial", 1);
                PlayerPrefs.Save();
            }
            isPracticingTrials = false;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (experimentManager != null)
        {
            experimentManager.OnTrialEnded -= OnTrialEnd;
        }
    }
    #endregion

    #region Initialization Methods
    /// <summary>
    /// Initializes the GameController, finding necessary components and subscribing to events.
    /// </summary>
    private void Initialize()
    {
        // Ensure this is called in Awake
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // Only try to find/create GridWorldManager if we're in a scene that needs it
        if (SceneManager.GetActiveScene().name == "GridWorld" ||
            SceneManager.GetActiveScene().name == "PracticeGridWorld")
        {
            if (gridWorldManager == null)
            {
                gridWorldManager = GridWorldManager.Instance;
                if (gridWorldManager == null)
                {
                    Debug.LogWarning("GridWorldManager not found in GridWorld scene!");
                    return;
                }
            }
        }

        // Ensure PracticeManager is initialized
        practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager == null)
        {
            // Debug.LogWarning("PracticeManager not found. Falling back to formal trial logic.");
        }

        // Ensure ExperimentManager is initialized
        experimentManager = ExperimentManager.Instance;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found!");
        }

        // Subscribe to ExperimentManager events
        experimentManager.OnTrialEnded += OnTrialEnd;
    }

    /// <summary>
    /// Initializes the practice trial sequence
    /// </summary>
    private void InitializePracticeTrials()
    {
        Debug.Log("GameController: Initializing Practice Trials");
        isPracticingTrials = true;
        currentPracticeTrialCount = 0;
    }

    private IEnumerator EndTrialCoroutine(bool rewardCollected)
    {
        // Only log practice trial info if we're actually in practice mode
        if (isPracticingTrials)
        {
            Debug.Log($"Ending practice trial {PracticeManager.Instance.GetCurrentPracticeTrialIndex()}");
        }
        else
        {
            Debug.Log("Ending formal trial");
        }

        yield return new WaitForSeconds(endTrialDelay);

        // Reset trial state
        ResetTrialState();

        // CRITICAL FIX: NEVER call HandleGridWorldOutcome from here
        // Practice trials are already handled in EndTrial method
        if (isPracticingTrials)
        {
            Debug.Log("Practice trial ended in EndTrialCoroutine - no additional action needed");
            yield break;
        }

        // Existing logic for non-practice trials
        if (experimentManager != null)
        {
            experimentManager.EndTrial(rewardCollected);
        }
        else
        {
            Debug.LogError("ExperimentManager is null when trying to end trial!");
        }

        StartCoroutine(ForceSceneTransition());
    }
    /// <summary>
    /// Initializes the LogManager component.
    /// </summary>
    private void InitializeLogManager()
    {
        logManager = FindAnyObjectByType<LogManager>();
        if (logManager == null)
        {
            Debug.LogWarning("LogManager not found in the scene. Creating a new instance.");
            GameObject logManagerObject = new GameObject("LogManager");
            logManager = logManagerObject.AddComponent<LogManager>();
            Debug.Log("LogManager initialized in GameController");
        }
    }

    /// <summary>
    /// Loads the calibrated presses per effort level from PlayerPrefs.
    /// </summary>
    private void LoadCalibratedPressesPerEffortLevel()
    {
        Debug.Log("LoadCalibratedPressesPerEffortLevel method called");
        bool calibrationFound = false;

        for (int i = 0; i < pressesPerEffortLevel.Length; i++)
        {
            int savedValue = PlayerPrefs.GetInt($"CalibratedPressesPerEffortLevel_{i}", -1);
            if (savedValue != -1)
            {
                pressesPerEffortLevel[i] = savedValue;
                calibrationFound = true;
            }
        }

        if (calibrationFound)
        {
            Debug.Log($"Loaded calibrated presses per effort level: {string.Join(", ", pressesPerEffortLevel)}");
        }
        else
        {
            Debug.Log($"No calibration found. Using default values: {string.Join(", ", pressesPerEffortLevel)}");
        }
    }

    #endregion

    #region Scene Management
    /// <summary>
    /// Modified OnSceneLoaded to handle practice trial scenes
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Debug.Log($"GameController detected scene load: {scene.name}");

        // Reset trial state when loading new scenes
        ResetTrialState();

        if (scene.name == "PracticeGridWorld")
        {
            StartCoroutine(SetupPracticeGridWorldScene());
        }
        else if (scene.name == "PracticeDecisionPhase")
        {
            SetupPracticePhaseTrial();
        }
        else if (scene.name == "GridWorld")
        {
            StartCoroutine(SetupGridWorldScene());
        }
        else if (scene.name == "DecisionPhase")
        {
            SetupDecisionPhaseScene();
            decisionPhaseStartTime = Time.time;
        }
    }

    /// <summary>
    /// Setup method for Practice Decision Phase
    /// </summary>
    private void SetupPracticePhaseTrial()
    {
        Debug.Log("Setting up Practice Decision Phase");
        // Add any specific setup for practice decision phase
        // For example, setting up different reward prefabs or UI
        InitializePracticeTrials();
    }


    private void SpawnPracticeReward()
    {
        // Clear any existing reward first
        if (rewardSpawner != null)
        {
            rewardSpawner.ClearReward();
        }

        // Custom reward spawning logic for practice trials
        Debug.Log($"Spawning Practice Trial Reward - Practice Trial {currentPracticeTrialCount}, Effort Level: {currentEffortLevel}");

        // If you have a PracticeManager with a method to get the correct sprite
        if (practiceManager != null)
        {
            // Get the sprite based on the current practice trial count or effort level
            Sprite practiceRewardSprite = practiceManager.GetCurrentPracticeTrialSprite();

            // Pass this sprite to the reward spawner
            rewardSpawner.SpawnReward(
                playerInitialPosition,
                currentPracticeTrialCount,
                currentEffortLevel,
                GetPressesRequired(currentEffortLevel),
                Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
            );
        }
        else
        {
            Debug.LogError("PracticeManager is not assigned!");

            // Fallback to regular reward spawning
            rewardSpawner.SpawnReward(
                playerInitialPosition,
                experimentManager.GetCurrentBlockNumber(),
                experimentManager.GetCurrentTrialIndex(),
                GetPressesRequired(currentEffortLevel),
                Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
            );
        }
    }

    /// <summary>
    /// Sets up components specific to the GridWorld scene.
    /// </summary>
    // Modify SetupGridWorldScene method to ensure proper component initialization
    private IEnumerator SetupGridWorldScene()
    {
        // Add this at the beginning of the method
        if (SceneManager.GetActiveScene().name == "GridWorld")
        {
            ValidateTrialMode();
        }

        Debug.Log("Setting up GridWorld Scene...");

        // Wait for scene to fully load
        yield return new WaitForSeconds(0.1f);

        // Find and validate GridManager first since other components depend on it
        gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found. Creating new instance...");
            GameObject gridObj = new GameObject("GridManager");
            gridManager = gridObj.AddComponent<GridManager>();
            yield return new WaitForSeconds(0.1f); // Give time for initialization
        }

        // Find or create RewardSpawner with proper GridManager reference
        rewardSpawner = FindAnyObjectByType<RewardSpawner>();
        if (rewardSpawner == null)
        {
            rewardSpawner.ClearReward();
            yield return new WaitForEndOfFrame();

            Debug.LogError("RewardSpawner not found. Creating new instance...");
            GameObject rewardSpawnerObj = new GameObject("RewardSpawner");
            rewardSpawner = rewardSpawnerObj.AddComponent<RewardSpawner>();

            yield return new WaitForSeconds(0.1f);
        }

        // Clear any existing rewards before proceeding
        rewardSpawner.ClearReward();
        Debug.Log("Clear any existing rewards before proceeding...");

        // Wait a frame to ensure reward clearing is complete
        yield return new WaitForEndOfFrame();

        // Ensure RewardSpawner has reference to GridManager
        rewardSpawner.SetGridManager(gridManager);
        Debug.Log("GridManager reference set in RewardSpawner");

        // Find or create PlayerSpawner
        playerSpawner = FindAnyObjectByType<PlayerSpawner>();
        if (playerSpawner == null)
        {
            Debug.LogError("PlayerSpawner not found. Creating new instance...");
            GameObject playerSpawnerObj = new GameObject("PlayerSpawner");
            playerSpawner = playerSpawnerObj.AddComponent<PlayerSpawner>();
        }

        // Find remaining components
        countdownTimer = FindAnyObjectByType<CountdownTimer>();
        scoreManager = FindAnyObjectByType<ScoreManager>();

        // Validate critical components
        if (!ValidateComponents())
        {
            Debug.LogError("Critical components missing. Attempting recovery...");
            yield return StartCoroutine(RecoverMissingComponents());

            // Check again after recovery attempt
            if (!ValidateComponents())
            {
                Debug.LogError("Failed to recover missing components. Cannot proceed with trial.");
                yield break;
            }
        }

        // Spawn player with explicit movement enabling
        if (!SpawnPlayerAndInitialize())
        {
            Debug.LogError("Failed to spawn and initialize player. Aborting setup.");
            yield break;
        }

        // Add a small delay to ensure everything is properly initialized
        yield return new WaitForSeconds(0.1f);

        // // Only spawn reward if we have a valid player position
        // if (playerInitialPosition != Vector2.zero)
        // {
        //     yield return StartCoroutine(SpawnRewardAtPosition());
        // }
        // else
        // {
        //     Debug.LogError("Invalid player position. Cannot spawn reward.");
        //     yield break;
        // }

        // Start trial if everything is ready
        StartCoroutine(StartTrial());
    }

    // Helper method to handle player spawning and initialization
    private bool SpawnPlayerAndInitialize()
    {
        try
        {
            SpawnPlayer();

            if (currentPlayer == null)
            {
                Debug.LogError("Failed to spawn player.");
                return false;
            }

            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController not found on spawned player!");
                return false;
            }

            int currentEffortLevel = PlayerPrefs.GetInt("CurrentPracticeEffortLevel", 1);
            int pressesRequired = GetPressesRequired(currentEffortLevel);
            playerController.SetPressesPerStep(pressesRequired);
            playerController.EnableMovement();
            Debug.Log($"Player initialized with {pressesRequired} presses required. Movement enabled.");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during player spawn and initialization: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    private IEnumerator SetupPracticeGridWorldScene()
    {
        Debug.Log("Setting up Practice GridWorld Scene...");

        // Wait for scene to fully load
        yield return new WaitForSeconds(0.1f);

        // Initialize PracticeManager first since we depend on it
        practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager not found! Cannot proceed with practice scene setup.");
            yield break;
        }

        // // Ensure the practice trials are initialized
        // if (practiceManager.GetTotalPracticeTrials() == 0)
        // {
        //     Debug.LogError("No practice trials generated. Cannot proceed with practice scene setup.");
        //     yield break;
        // }

        // Ensure the trial index is valid
        // if (practiceManager.GetCurrentPracticeTrialIndex() < 0)
        // {
        //     Debug.LogError("Invalid practice trial index. Resetting to 0.");
        //     practiceManager.SetCurrentPracticeTrialIndex(0); // Reset the index
        // }

        // Find and validate GridManager first since other components depend on it
        gridManager = FindAnyObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("GridManager not found. Creating new instance...");
            GameObject gridObj = new GameObject("GridManager");
            gridManager = gridObj.AddComponent<GridManager>();
            yield return new WaitForSeconds(0.1f); // Give time for initialization
        }

        // Find or create RewardSpawner with proper GridManager reference
        rewardSpawner = FindAnyObjectByType<RewardSpawner>();
        if (rewardSpawner == null)
        {
            Debug.LogError("RewardSpawner not found. Creating new instance...");
            GameObject rewardSpawnerObj = new GameObject("RewardSpawner");
            rewardSpawner = rewardSpawnerObj.AddComponent<RewardSpawner>();
        }

        // Ensure RewardSpawner has reference to GridManager
        if (rewardSpawner != null && gridManager != null)
        {
            rewardSpawner.SetGridManager(gridManager);
        }

        // Find or create PlayerSpawner
        playerSpawner = FindAnyObjectByType<PlayerSpawner>();
        if (playerSpawner == null)
        {
            Debug.LogError("PlayerSpawner not found. Creating new instance...");
            GameObject playerSpawnerObj = new GameObject("PlayerSpawner");
            playerSpawner = playerSpawnerObj.AddComponent<PlayerSpawner>();
        }

        // Find remaining components with null checks
        countdownTimer = FindAnyObjectByType<CountdownTimer>();
        if (countdownTimer == null)
        {
            Debug.LogError("CountdownTimer not found!");
            yield break;
        }

        countdownTimer.Initialize();
        StartCoroutine(StartTrial());

        scoreManager = FindAnyObjectByType<ScoreManager>();
        experimentManager = ExperimentManager.Instance;

        // Validate critical components
        if (!ValidateComponents())
        {
            Debug.LogError("Critical components missing. Attempting recovery...");
            yield return StartCoroutine(RecoverMissingComponents());

            // Check again after recovery attempt
            if (!ValidateComponents())
            {
                Debug.LogError("Failed to recover missing components. Cannot proceed with trial.");
                yield break;
            }
        }

        // Reset trial state
        isTrialActive = true;
        isTrialEnded = false;
        rewardCollected = false;
        scoreAdded = false;
        trialStartTime = Time.time;

        yield return StartCoroutine(SpawnPracticePlayerAndRewardSynchronized(currentEffortLevel));

        // Get current effort level from PracticeManager
        currentEffortLevel = practiceManager != null ? practiceManager.GetCurrentTrialEffortLevel() : 1;

        // Start trial if everything is ready
        StartCoroutine(StartTrial());

    }

    private IEnumerator SpawnPracticePlayerAndRewardSynchronized(int effortLevel)
    {
        Debug.Log("Starting synchronized spawn of practice player and reward...");

        // Spawn the player
        SpawnPlayer();
        yield return new WaitForEndOfFrame(); // Wait for the frame to complete

        // Ensure the player is spawned before proceeding
        if (currentPlayer == null)
        {
            Debug.LogError("Player spawning failed during synchronized spawn.");
            yield break;
        }

        // Get the player's position
        Vector2 playerPosition = currentPlayer.transform.position;

        // Set player effort level
        PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
        if (playerController != null)
        {
            // Explicitly reset counter
            playerController.ResetCounters();

            // Set required presses and enable movement
            int pressesRequired = GetPressesRequired(effortLevel);
            playerController.SetPressesPerStep(pressesRequired);
            playerController.EnableMovement();
            Debug.Log($"Practice Player initialized with {pressesRequired} presses required. Movement enabled.");
        }
        else
        {
            Debug.LogError("PlayerController not found on spawned player!");
            yield break;
        }

        // Spawn practice reward
        SpawnPracticeReward(); // Call the method to spawn the practice reward
        Debug.Log("Synchronized spawn of practice player and reward completed.");
    }
    private bool ValidateComponents()
    {
        bool isValid = true;

        if (gridManager == null)
        {
            Debug.LogError("GridManager is null");
            isValid = false;
        }

        if (rewardSpawner == null)
        {
            Debug.LogError("RewardSpawner is null");
            isValid = false;
        }

        if (playerSpawner == null)
        {
            Debug.LogError("PlayerSpawner is null");
            isValid = false;
        }

        return isValid;
    }

    // Add recovery method
    private IEnumerator RecoverMissingComponents()
    {
        if (gridManager == null)
        {
            Debug.Log("Creating new GridManager...");
            GameObject gridObj = new GameObject("GridManager");
            gridManager = gridObj.AddComponent<GridManager>();
            yield return new WaitForSeconds(0.1f); // Give time for initialization
        }

        if (rewardSpawner == null)
        {
            Debug.Log("Creating new RewardSpawner...");
            GameObject rewardSpawnerObj = new GameObject("RewardSpawner");
            rewardSpawner = rewardSpawnerObj.AddComponent<RewardSpawner>();
            rewardSpawner.SetGridManager(gridManager);
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// Sets up components specific to the DecisionPhase scene.
    /// </summary>
    private void SetupDecisionPhaseScene()
    {
        // Add any necessary setup for the DecisionPhase scene here
    }
    #endregion

    #region Trial Control Methods
    /// <summary>
    /// Starts a new trial, spawning player and reward, and setting up the timer.
    /// </summary>

    private IEnumerator StartTrial()
    {
        Debug.Log("StartTrial called. Initializing trial...");

        // If it's a practice trial, log additional details
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            if (PracticeManager.Instance != null && PracticeManager.Instance.GetCurrentPracticeTrial() != null)
            {
                Debug.Log($"Practice Trial Details - Effort Level: {PracticeManager.Instance.GetCurrentPracticeTrial().effortLevel}");
                Debug.Log($"Presses Required: {PracticeManager.Instance.GetCurrentTrialPressesRequired()}");
            }
        }

        if (isInitializingTrial || isTrialActive)
        {
            Debug.Log("Trial initialization already in progress or trial already active. Skipping.");
            yield break;
        }

        isInitializingTrial = true;

        // Initialize components if they're null
        if (countdownTimer == null)
        {
            countdownTimer = FindAnyObjectByType<CountdownTimer>();
            if (countdownTimer == null)
            {
                Debug.LogError("CountdownTimer not found and could not be created!");
                isInitializingTrial = false;
                yield break;
            }
        }

        // Reset trial state
        isTrialActive = true;
        isTrialEnded = false;
        rewardCollected = false;
        scoreAdded = false;
        trialStartTime = Time.time;
        hasLoggedTrialOutcome = false;

        // Synchronized spawn of player and reward
        yield return StartCoroutine(SpawnPlayerAndRewardSynchronized());

        // Handle any errors that occurred during spawning
        if (currentPlayer == null || currentReward == null)
        {
            Debug.LogError("Failed to spawn player or reward during synchronized spawn.");
            isInitializingTrial = false;
            isTrialActive = false;
            yield break;
        }

        try
        {
            // Initialize and start timer
            if (countdownTimer != null)
            {
                countdownTimer.OnTimerExpired -= OnTimerExpired;
                countdownTimer.OnTimerExpired += OnTimerExpired;

                if (!countdownTimer.IsInitialized)
                {
                    countdownTimer.Initialize();
                }
                countdownTimer.StartTimer(maxTrialDuration);
                StartCoroutine(CheckTimerAccuracyRoutine());
                Debug.Log($"Countdown timer started. Duration: {maxTrialDuration}");
            }

            // Enable player movement
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.EnableMovement();
                Debug.Log("Player movement explicitly enabled in StartTrial.");
            }

            // Log trial start
            // LogTrialStart();

            // Log debug information
            if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
            {
                // Log practice trial details
                if (practiceManager != null)
                {
                    Debug.Log($"Starting Practice Trial {practiceManager.GetCurrentPracticeTrialIndex() + 1}");
                    Debug.Log($"- Effort Level: {practiceManager.GetCurrentTrialEffortLevel()}");
                    Debug.Log($"- Presses Required: {practiceManager.GetCurrentTrialPressesRequired()}");
                }
                else
                {
                    Debug.LogError("PracticeManager is null during practice trial!");
                }
            }
            else
            {
                // Log formal trial details
                Debug.Log($"Starting Trial {experimentManager.GetCurrentTrialIndex()} in Block {experimentManager.GetCurrentBlockNumber()}.");
                Debug.Log($"- Effort Level: {currentEffortLevel}");
                Debug.Log($"- Presses Required: {GetPressesRequired(currentEffortLevel)}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during trial setup: {e.Message}\n{e.StackTrace}");
            isInitializingTrial = false;
            isTrialActive = false;
            yield break;
        }
        finally
        {
            isInitializingTrial = false;
        }
    }

    private IEnumerator SpawnPlayerAndRewardSynchronized()
    {
        Debug.Log("Starting synchronized spawn of player and reward...");

        // Spawn the player
        SpawnPlayer();
        // yield return null; // Wait for one frame to ensure the player is spawned
        yield return new WaitForEndOfFrame(); // Wait for the frame to complete

        // Ensure the player is spawned before proceeding
        if (currentPlayer == null)
        {
            Debug.LogError("Player spawning failed during synchronized spawn.");
            yield break;
        }

        // Get the player's position
        Vector2 playerPosition = currentPlayer.transform.position;

        // Spawn the reward at the correct distance from the player
        if (rewardSpawner != null)
        {
            currentReward = rewardSpawner.SpawnReward(
                playerPosition,
                experimentManager.GetCurrentBlockNumber(),
                experimentManager.GetCurrentTrialIndex(),
                GetPressesRequired(currentEffortLevel),
                Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
            );

            if (currentReward == null)
            {
                Debug.LogError("Reward spawning failed during synchronized spawn.");
                yield break;
            }
        }
        else
        {
            Debug.LogError("RewardSpawner is null during synchronized spawn.");
            yield break;
        }

        Debug.Log("Synchronized spawn of player and reward completed.");
    }

    /// <summary>
    /// Ends the current trial and notifies the ExperimentManager.
    /// </summary>
    // public void EndTrial(bool rewardCollected)
    // {
    //     if (!isTrialActive || isTrialEnded)
    //     {
    //         Debug.Log("EndTrial called but trial is not active or already ended");
    //         return;
    //     }

    //     Debug.Log($"Ending trial. Reward collected: {rewardCollected}, Practice mode: {IsPracticeMode()}");

    //     // If in practice mode, let PracticeManager handle it
    //     if (IsPracticeMode() && PracticeManager.Instance != null)
    //     {
    //         Debug.Log("Practice mode detected, delegating to PracticeManager");
    //         PracticeManager.Instance.HandleGridWorldOutcome(!rewardCollected);
    //         return;
    //     }


    //     isTrialActive = false;
    //     isTrialEnded = true;
    //     trialEndTime = Time.time;
    //     float trialDuration = trialEndTime - trialStartTime;

    //     // Stop all coroutines and cleanup
    //     StopAllCoroutines();
    //     StopCountdownTimer();
    //     FreezePlayer();
    //     HideRewardIfCollected(rewardCollected);

    //     // Log trial data
    //     // LogTrialEnd(rewardCollected, trialDuration);

    //     // Clean up the scene
    //     CleanupTrial(rewardCollected);

    //     // Reset trial state flags
    //     isTrialActive = false;
    //     isTrialEnded = false;
    //     isInitializingTrial = false;

    //     // Debug.Log($"GameController EndTrial method called: isTrialActive: {isTrialActive}, isTrialEnded: {isTrialEnded}, isInitializingTrial: {isInitializingTrial}");

    //     // Notify ExperimentManager that the trial has ended
    //     StartCoroutine(EndTrialCoroutine(rewardCollected));

    //     // Notify GridWorldManager
    //     if (gridWorldManager != null)
    //     {
    //         gridWorldManager.EndTrial(rewardCollected);
    //     }
    //     else
    //     {
    //         Debug.LogError("GridWorldManager is null when trying to end trial!");
    //         StartCoroutine(ForceSceneTransition());
    //     }
    // }

    public void EndTrial(bool rewardCollected)
    {
        if (!isTrialActive || isTrialEnded)
        {
            Debug.Log("EndTrial called but trial is not active or already ended");
            return;
        }

        Debug.Log($"Ending trial. Reward collected: {rewardCollected}, Practice mode: {IsPracticeMode()}");

        // Set trial ended flags immediately to prevent multiple calls
        isTrialActive = false;
        isTrialEnded = true;
        trialEndTime = Time.time;
        float trialDuration = trialEndTime - trialStartTime;

        // Stop all coroutines and cleanup
        StopAllCoroutines();
        StopCountdownTimer();
        FreezePlayer();
        HideRewardIfCollected(rewardCollected);

        // Clean up the scene
        CleanupTrial(rewardCollected);

        // CRITICAL FIX: Only handle practice trials in ONE place to prevent double advancement
        if (IsPracticeMode() && PracticeManager.Instance != null)
        {
            Debug.Log("Practice mode detected, delegating to PracticeManager");
            // Generate unique transaction ID to prevent duplicate processing
            string transactionId = System.Guid.NewGuid().ToString();
            Debug.Log($"Generated transaction ID: {transactionId}");
            PracticeManager.Instance.HandleGridWorldOutcome(!rewardCollected, transactionId);
            return; // Exit early - don't call EndTrialCoroutine for practice trials
        }

        // Reset trial state flags - redundant but keeping for safety
        isInitializingTrial = false;

        // ONLY handle experiment trials here - practice trials are already handled above
        StartCoroutine(EndTrialCoroutine(rewardCollected));

        // Notify GridWorldManager only for non-practice trials
        if (gridWorldManager != null)
        {
            gridWorldManager.EndTrial(rewardCollected);
        }
        else
        {
            Debug.LogError("GridWorldManager is null when trying to end trial!");
            StartCoroutine(ForceSceneTransition());
        }
    }
    /// <summary>
    /// Forces a scene transition to the DecisionPhase scene if the automatic transition fails.
    /// </summary>
    private IEnumerator ForceSceneTransition()
    {
        Debug.Log("Attempting forced scene transition to DecisionPhase.");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("DecisionPhase");
        asyncLoad.allowSceneActivation = true;

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            Debug.Log($"Scene load progress: {asyncLoad.progress * 100}%");
            yield return null;
        }

        if (SceneManager.GetActiveScene().name != "DecisionPhase")
        {
            Debug.LogError("Failed to transition to DecisionPhase scene even after forced transition!");
        }
        else
        {
            Debug.Log("Successfully transitioned to DecisionPhase scene.");
        }
    }

    private void CleanupTrial(bool rewardCollected)
    {
        Debug.Log("CleanupTrial method called.");

        // Reset all state flags
        isTrialActive = false;
        isTrialEnded = false;
        isInitializingTrial = false;
        scoreAdded = false;
        rewardCollected = false;

        // Clean up player
        FreezePlayer();
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        // Clean up reward
        if (rewardSpawner != null)
        {
            rewardSpawner.ClearReward();
        }

        // Stop and clean up timer
        StopCountdownTimer();

        // Hide grid
        HideGrid();

        Debug.Log("Trial cleanup completed");
    }

    public void ResetTrialState()
    {
        isTrialActive = false;
        isTrialEnded = false;
        isInitializingTrial = false;
        scoreAdded = false;
        rewardCollected = false;

        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
        }

        if (rewardSpawner != null)
        {
            rewardSpawner.ClearReward();
        }

        Debug.Log("Trial state reset completed");
    }

    /// <summary>
    /// Callback for when a trial ends. This method is subscribed to the ExperimentManager's OnTrialEnded event.
    /// </summary>
    private void OnTrialEnd(bool rewardCollected)
    {
        // This method is called by the ExperimentManager, so we don't need to do anything here
        // The next trial will be started by the ExperimentManager
    }

    #endregion

    #region Spawning Methods
    /// <summary>
    /// Spawns the player at a position determined by the PlayerSpawner.
    /// </summary>
    private void SpawnPlayer()
    {
        Debug.Log("SpawnPlayer method called.");

        // First, try to find an existing player in the scene
        PlayerController existingPlayerController = FindAnyObjectByType<PlayerController>();

        if (existingPlayerController != null)
        {
            // If a player already exists, use that instead of spawning a new one
            currentPlayer = existingPlayerController.gameObject;
            Debug.Log("Existing player found. Using existing player.");
        }
        else if (playerSpawner != null)
        {
            // If no player exists, spawn a new one
            currentPlayer = playerSpawner.SpawnPlayer();
            Debug.Log($"Player spawned at position: {currentPlayer.transform.position}");
        }
        else
        {
            Debug.LogError("PlayerSpawner is not assigned!");
            return;
        }

        // Validate and setup the player
        if (currentPlayer != null)
        {
            playerInitialPosition = currentPlayer.transform.position;

            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                currentPlayer.SetActive(true);

                // Set the required presses 
                int pressesRequired = GetPressesRequired(currentEffortLevel);
                playerController.SetPressesPerStep(pressesRequired);

                // Enable movement explicitly
                playerController.EnableMovement();
                Debug.Log($"Player setup complete at position: {playerInitialPosition}");
            }
            else
            {
                Debug.LogError("PlayerController component not found on spawned/existing player!");
            }
        }
        else
        {
            Debug.LogError("Failed to find or spawn player!");
        }
    }

    /// <summary>
    /// Determines the number of button presses required based on the effort level.
    /// </summary>
    private int GetPressesRequired(int effortLevel)
    {
        effortLevel = Mathf.Clamp(effortLevel, 1, pressesPerEffortLevel.Length);
        return pressesPerEffortLevel[effortLevel - 1];
    }
    #endregion

    #region Player and Reward Management
    /// <summary>
    /// Freezes the player's movement.
    /// </summary>
    private void FreezePlayer()
    {
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.DisableMovement();
                playerFinalPosition = currentPlayer.transform.position;
                Debug.Log($"Player frozen at position: {playerFinalPosition}");
            }
            else
            {
                Debug.LogError("PlayerController component not found on currentPlayer!");
            }
        }
        else
        {
            Debug.LogWarning("currentPlayer is null when trying to freeze player.");
        }
    }

    /// <summary>
    /// Hides the reward object if it was collected.
    /// </summary>
    private void HideRewardIfCollected(bool rewardCollected)
    {
        if (rewardCollected && currentReward != null)
        {
            currentReward.SetActive(false);
            Debug.Log("Reward hidden after collection.");
        }
        // logManager.LogCollisionTime(currentTrialIndex);
    }
    #endregion


    #region Timer Management
    /// <summary>
    /// Stops the countdown timer and unsubscribes from its event.
    /// </summary>
    private void StopCountdownTimer()
    {
        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
            countdownTimer.OnTimerExpired -= OnTimerExpired;
            Debug.Log("Countdown timer stopped.");
        }
        else
        {
            Debug.LogWarning("countdownTimer is null during cleanup.");
        }
    }


    /// Only end the trial when timer expires
    // private void OnTimerExpired()
    // {
    //     if (isTrialActive && !isTrialEnded)
    //     {
    //         Debug.Log("Time's up! Ending trial.");
    //         EndTrial(rewardCollected);
    //     }
    // }

    private void OnTimerExpired()
    {
        if (isTrialActive && !isTrialEnded && !hasLoggedTrialOutcome)
        {
            Debug.Log("Time's up! Ending trial.");
            EndTrial(rewardCollected);
        }
    }

    /// <summary>
    /// Periodically checks the accuracy of the countdown timer.
    /// </summary>
    private IEnumerator CheckTimerAccuracyRoutine()
    {
        while (isTrialActive)
        {
            yield return new WaitForSeconds(1f);
            countdownTimer.CheckTimerAccuracy();
        }
    }
    #endregion

    #region Grid Management
    /// <summary>
    /// Shows the grid in the scene.
    /// </summary>
    // private void ShowGrid()
    // {
    //     if (gridManager != null)
    //     {
    //         gridManager.ShowGrid();
    //     }
    //     else
    //     {
    //         Debug.LogError("GridManager is null. Cannot show grid.");
    //     }
    // }

    /// <summary>
    /// Hides the grid in the scene.
    /// </summary>
    private void HideGrid()
    {
        if (gridManager != null)
        {
            gridManager.HideGrid();
        }
        else
        {
            Debug.LogError("GridManager is null. Cannot hide grid.");
        }
    }
    #endregion

    #region Reward Collection
    /// <summary>
    /// Called when the player collects the reward.
    /// </summary>
    public void RewardCollected(bool collision)
    {
        Debug.Log($"RewardCollected called. Collision: {collision}, isTrialActive: {isTrialActive}, rewardCollected: {rewardCollected}, isTrialEnded: {isTrialEnded}");

        // Early exit conditions
        if (!isTrialActive || rewardCollected || isTrialEnded) return;

        if (collision && !scoreAdded)
        {
            rewardCollected = true;
            scoreAdded = true;

            float rewardValue = experimentManager != null
                ? experimentManager.GetCurrentTrialRewardValue()
                : 0f;

            try
            {
                string currentScene = SceneManager.GetActiveScene().name;
                bool isPracticeTrial = currentScene.Contains("Practice") || PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

                // Get scores BEFORE adding points
                int preTotalScore = ScoreManager.Instance?.GetTotalScore() ?? 0;
                int prePracticeScore = isPracticeTrial
                    ? (PracticeScoreManager.Instance?.GetCurrentScore() ?? 0)
                    : (ScoreManager.Instance?.GetPracticeScore() ?? 0);

                if (isPracticeTrial)
                {
                    if (PracticeScoreManager.Instance != null)
                    {
                        PracticeScoreManager.Instance.AddScore(Mathf.RoundToInt(rewardValue));
                    }
                    else if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.AddScore(Mathf.RoundToInt(rewardValue), isFormalTrial: false);
                    }
                }
                else
                {
                    if (ScoreManager.Instance != null)
                    {
                        ScoreManager.Instance.AddScore(Mathf.RoundToInt(rewardValue), isFormalTrial: true);
                    }
                }

                // Get scores AFTER adding points
                int postTotalScore = ScoreManager.Instance?.GetTotalScore() ?? preTotalScore;
                int postPracticeScore = isPracticeTrial
                    ? (PracticeScoreManager.Instance?.GetCurrentScore() ?? prePracticeScore)
                    : (ScoreManager.Instance?.GetPracticeScore() ?? prePracticeScore);

                Debug.Log($"Score updated - Before: {preTotalScore}/{prePracticeScore}, After: {postTotalScore}/{postPracticeScore}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error adding score: {e.Message}\n{e.StackTrace}");
            }

            // Hide the reward immediately upon collection
            if (currentReward != null)
            {
                currentReward.SetActive(false);
                Destroy(currentReward);
                Debug.Log("Reward hidden upon collection.");
            }
        }

        // Log collision time with the correct trial index
        if (ExperimentManager.Instance != null)
        {
            int trialIndex = ExperimentManager.Instance.GetCurrentTrialIndex();
            // logManager.LogCollisionTime(trialIndex);
        }
        else
        {
            Debug.LogError("ExperimentManager is null when trying to log collision time!");
        }
    }
    #endregion

    #region Logging
    /// <summary>
    /// Logs the start of a trial.
    /// </summary>
    // private void LogTrialStart() { }
    // private void LogTrialEnd(bool rewardCollected, float trialDuration) { }
    public float GetActionReactionTime()
    {
        return actionReactionTime;
    }

    #endregion

    #region Utility Methods
    /// <summary>
    /// Sets the number of presses required per step based on the effort level.
    /// </summary>
    private void SetPressesPerStep(int effortLevel)
    {
        Debug.Log($"SetPressesPerStep called with effort level: {effortLevel}");

        int pressesRequired;

        // Check if it's a practice trial
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1 && practiceManager != null)
        {
            // Use PracticeManager to get presses required for practice trials
            pressesRequired = practiceManager.GetCurrentTrialPressesRequired();
            Debug.Log($"Practice Trial - Effort Level: {effortLevel}, Presses Required: {pressesRequired}");
        }
        else
        {
            // Use ExperimentManager or default logic for formal trials
            pressesRequired = experimentManager.GetCurrentTrialEV();
            Debug.Log($"Formal Trial - Effort Level: {effortLevel}, Presses Required: {pressesRequired}");
        }

        // Set presses per step in the PlayerController
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.SetPressesPerStep(pressesRequired);
                Debug.Log($"Set presses per step to {pressesRequired} for effort level {effortLevel}");
            }
            else
            {
                Debug.LogError("PlayerController component not found on currentPlayer!");
            }
        }
        else
        {
            Debug.Log("currentPlayer is null when trying to set presses per step!");
        }
    }
    #endregion

    // Add this method to ensure proper initialization order
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (countdownTimer != null)
        {
            countdownTimer.OnTimerExpired += OnTimerExpired;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (countdownTimer != null)
        {
            countdownTimer.OnTimerExpired -= OnTimerExpired;
        }
    }
}