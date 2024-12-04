using UnityEngine;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


/// <summary>
/// Controls the gameplay mechanics for each trial in the GridWorld scene and manages the interaction
/// with ExperimentManager for seamless flow between DecisionPhase and GridWorld scenes.
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

    #region Serialized Fields
    [SerializeField] private float maxTrialDuration = 5f;
    [SerializeField] private float startTrialDelay = 0.1f;
    [SerializeField] private float endTrialDelay = 0.1f;
    [SerializeField] private int[] pressesPerEffortLevel = { 1, 2, 3 }; // Default values
    [SerializeField] private float sceneTransitionTimeout = 5f; // Maximum time to wait for scene transition
    // [SerializeField] private float constantRewardDistance = 5f;
    [SerializeField] private ExperimentManager experimentManager;
    [SerializeField] private GridWorldManager gridWorldManager;
    #endregion

    #region Private Fields
    private PlayerSpawner playerSpawner;
    private RewardSpawner rewardSpawner;
    private GridManager gridManager;
    private GameObject currentPlayer;
    private GameObject currentReward;
    private bool rewardCollected = false;
    private int buttonPressCount;
    private bool isTrialActive = false;
    private bool isTrialEnded = false;
    private bool scoreAdded = false;
    private Vector2 playerInitialPosition;
    private Vector2 playerFinalPosition;
    private Vector2 actualRewardPosition;
    private CountdownTimer countdownTimer;
    private ScoreManager scoreManager;
    private LogManager logManager;
    private float trialStartTime;
    private float trialEndTime;
    private int currentBlockNumber = 0;
    private int currentTrialIndex = 0;
    private int currentEffortLevel = 0;
    private float decisionReactionTime = 0f;
    private float actionReactionTime = 0f;
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Initialize ExperimentManager and GridWorldManager
        experimentManager = ExperimentManager.Instance;
        gridWorldManager = GridWorldManager.Instance;

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

        experimentManager.OnTrialEnded += OnTrialEnd;


        // Ensure movement is enabled if we're in GridWorld scene
        if (SceneManager.GetActiveScene().name == "GridWorld")
        {
            StartCoroutine(SetupGridWorldScene());
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

        // Find GridWorldManager (should be in DontDestroyOnLoad)
        if (gridWorldManager == null)
        {
            gridWorldManager = GridWorldManager.Instance;
            if (gridWorldManager == null)
            {
                Debug.LogError("GridWorldManager not found! Creating new instance...");
                GameObject gwmObject = new GameObject("GridWorldManager");
                gridWorldManager = gwmObject.AddComponent<GridWorldManager>();
                DontDestroyOnLoad(gwmObject);
            }
        }

        // Find ExperimentManager (should be in DontDestroyOnLoad)
        experimentManager = ExperimentManager.Instance;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found!");
            return;
        }

        // Subscribe to ExperimentManager events
        experimentManager.OnTrialEnded += OnTrialEnd;
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
    /// Called when a new scene is loaded. Sets up necessary components for the current scene.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GridWorld")
        {
            StartCoroutine(SetupGridWorldScene());
        }
        else if (scene.name == "DecisionPhase")
        {
            SetupDecisionPhaseScene();
        }
    }

    /// <summary>
    /// Sets up components specific to the GridWorld scene.
    /// </summary>
    // GameController.cs - Modified SetupGridWorldScene method
    // Modify SetupGridWorldScene method to ensure proper component initialization
    private IEnumerator SetupGridWorldScene()
    {
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
            Debug.LogError("RewardSpawner not found. Creating new instance...");
            GameObject rewardSpawnerObj = new GameObject("RewardSpawner");
            rewardSpawner = rewardSpawnerObj.AddComponent<RewardSpawner>();
        }

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

        // Spawn sequence with validation
        try
        {
            // Spawn player first and validate
            // SpawnPlayer();
            // if (currentPlayer == null)
            // {
            //     Debug.LogError("Failed to spawn player. Aborting spawn sequence.");
            //     yield break;
            // }

            // // Get initial position before spawning reward
            // playerInitialPosition = currentPlayer.transform.position;
            // Debug.Log($"Player initial position: {playerInitialPosition}");

            // Spawn player with explicit movement enabling
            SpawnPlayer();
            if (currentPlayer != null)
            {
                PlayerController controller = currentPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    // Set required presses and enable movement
                    int pressesRequired = GetPressesRequired(currentEffortLevel);
                    controller.SetPressesPerStep(pressesRequired);
                    controller.EnableMovement();
                    Debug.Log($"Player initialized with {pressesRequired} presses required. Movement enabled.");
                }
                else
                {
                    Debug.LogError("PlayerController not found on spawned player!");
                }
            }

            // Only spawn reward if we have a valid player position
            if (playerInitialPosition != Vector2.zero)
            {
                // yield return new WaitForSeconds(0.1f); // Give components time to initialize
                // SpawnReward();
                // rewardSpawner.SpawnReward();
                // currentReward = 
                // gridWorldManager.SetRewardSpriteFromDecisionPhase();

                rewardSpawner.SpawnReward(
                            playerInitialPosition,
                            experimentManager.GetCurrentBlockNumber(),
                            experimentManager.GetCurrentTrialIndex(),
                            GetPressesRequired(currentEffortLevel),
                            Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue()));
            }
            else
            {
                Debug.LogError("Invalid player position. Cannot spawn reward.");
                yield break;
            }

            // countdownTimer.StartTimer(5f);

            // Start trial if everything is ready
            StartCoroutine(StartTrial());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during spawn sequence: {e.Message}\n{e.StackTrace}");
            yield break;
        }
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

    /// <summary>
    /// Validates that all required components for the GridWorld scene are assigned.
    /// </summary>
    private void ValidateGridWorldComponents()
    {
        if (countdownTimer == null) throw new Exception("CountdownTimer not found in the GridWorld scene!");
        if (scoreManager == null) throw new Exception("ScoreManager not found in the GridWorld scene!");
        if (gridManager == null) throw new Exception("GridManager not found in the GridWorld scene!");
        if (playerSpawner == null) throw new Exception("PlayerSpawner not found in the GridWorld scene!");
        if (rewardSpawner == null) throw new Exception("RewardSpawner not found in the GridWorld scene!");
    }
    #endregion

    #region Trial Control Methods
    /// <summary>
    /// Starts a new trial, spawning player and reward, and setting up the timer.
    /// </summary>
    public IEnumerator StartTrial()
    {
        Debug.Log("StartTrial called. Initializing trial...");
        yield return new WaitForSeconds(startTrialDelay);

        // Early exit if trial is already active
        if (isTrialActive)
        {
            Debug.LogWarning("Trial already active, ignoring start request.");
            yield break;
        }

        // Find or create CountdownTimer
        if (countdownTimer == null)
        {
            countdownTimer = FindAnyObjectByType<CountdownTimer>();
            if (countdownTimer == null)
            {
                Debug.Log("Creating new CountdownTimer instance...");
                GameObject timerObj = new GameObject("CountdownTimer");
                countdownTimer = timerObj.AddComponent<CountdownTimer>();
                countdownTimer.Initialize(); // Make sure the Initialize method exists in CountdownTimer
            }
        }

        // Ensure ExperimentManager exists
        if (experimentManager == null)
        {
            experimentManager = ExperimentManager.Instance;
            if (experimentManager == null)
            {
                Debug.LogError("ExperimentManager not found! Cannot start trial.");
                yield break;
            }
        }

        // Ensure GridWorldManager exists
        if (gridWorldManager == null)
        {
            gridWorldManager = GridWorldManager.Instance;
            if (gridWorldManager == null)
            {
                Debug.LogError("GridWorldManager not found! Cannot start trial.");
                yield break;
            }
        }

        try
        {
            // Reset trial variables
            isTrialActive = true;
            isTrialEnded = false;
            rewardCollected = false;
            scoreAdded = false;
            buttonPressCount = 0;
            trialStartTime = Time.time;

            // Setup environment
            ShowGrid();
            SpawnPlayer();

            // SpawnReward();
            // rewardSpawner.SpawnReward();
            rewardSpawner.SpawnReward(
                playerInitialPosition,
                experimentManager.GetCurrentBlockNumber(),
                experimentManager.GetCurrentTrialIndex(),
                GetPressesRequired(currentEffortLevel),
                Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
                );

            // Set current effort level and presses per step
            currentEffortLevel = experimentManager.GetCurrentTrialEV();
            SetPressesPerStep(currentEffortLevel);

            // Initialize and start the timer directly instead of through GridWorldManager
            if (countdownTimer != null)
            {
                // Unsubscribe first to prevent duplicate subscriptions
                countdownTimer.OnTimerExpired -= OnTimerExpired;
                countdownTimer.OnTimerExpired += OnTimerExpired;

                // Start the timer with safety checks
                if (!countdownTimer.IsInitialized)
                {
                    countdownTimer.Initialize();
                }
                countdownTimer.StartTimer(maxTrialDuration);
                StartCoroutine(CheckTimerAccuracyRoutine());
                Debug.Log($"Countdown timer started. Duration: {maxTrialDuration}");
            }
            else
            {
                Debug.LogError("CountdownTimer is still null after initialization attempt!");
                yield break;
            }

            // Enable player movement
            EnablePlayerMovement();

            // Log trial start
            LogTrialStart();

            // Log debug information
            int currentTrialIndex = experimentManager.GetCurrentTrialIndex();
            int currentBlockNumber = experimentManager.GetCurrentBlockNumber();
            int pressesRequired = GetPressesRequired(currentEffortLevel);

            Debug.Log($"Starting Trial {currentTrialIndex} in Block {currentBlockNumber}. " +
                     $"Effort Level: {currentEffortLevel}, Presses Required: {pressesRequired}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during trial setup: {e.Message}\n{e.StackTrace}");
            isTrialActive = false;
            yield break;
        }
    }

    /// <summary>
    /// Ends the current trial and notifies the ExperimentManager.
    /// </summary>
    public void EndTrial(bool rewardCollected)
    {
        if (!isTrialActive || isTrialEnded) return;

        Debug.Log($"Ending trial. Reward collected: {rewardCollected}");

        isTrialActive = false;
        isTrialEnded = true;
        trialEndTime = Time.time;
        float trialDuration = trialEndTime - trialStartTime;

        // Stop all coroutines and cleanup
        StopAllCoroutines();
        StopCountdownTimer();
        FreezePlayer();
        HideRewardIfCollected(rewardCollected);

        // Log trial data
        LogTrialEnd(rewardCollected, trialDuration);

        // Clean up the scene
        CleanupTrial(rewardCollected);

        // Notify ExperimentManager that the trial has ended
        StartCoroutine(EndTrialCoroutine(rewardCollected));

        // Notify GridWorldManager directly instead of having it call back
        if (gridWorldManager != null)
        {
            gridWorldManager.EndTrial(rewardCollected);
        }
        else
        {
            Debug.LogError("GridWorldManager is null when trying to end trial!");
            // Fallback: try to transition scenes directly
            StartCoroutine(ForceSceneTransition());
        }
        // Log trial outcome
        string outcome = rewardCollected ? "Completed" : "TimedOut";
    }

    /// <summary>
    /// Coroutine to handle the end of a trial, including cleanup and notification to ExperimentManager.
    /// </summary>
    private IEnumerator EndTrialCoroutine(bool rewardCollected)
    {
        Debug.Log("EndTrialCoroutine started.");
        yield return new WaitForSeconds(endTrialDelay);

        Debug.Log("Attempting to end trial via ExperimentManager.");
        if (experimentManager != null)
        {
            experimentManager.EndTrial(rewardCollected);
        }
        else
        {
            Debug.LogError("ExperimentManager is null when trying to end the trial!");
        }

        // Wait for scene transition with a timeout
        float startTime = Time.time;
        while (SceneManager.GetActiveScene().name == "GridWorld" && Time.time - startTime < sceneTransitionTimeout)
        {
            Debug.Log($"Waiting for scene transition... Time elapsed: {Time.time - startTime}s");
            yield return new WaitForSeconds(0.5f);
        }

        // Force scene transition if it hasn't happened
        if (SceneManager.GetActiveScene().name == "GridWorld")
        {
            Debug.LogWarning($"Forced scene transition after {sceneTransitionTimeout}s wait.");
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

    /// <summary>
    /// Cleans up the scene after a trial ends.
    /// </summary>
    // private void CleanupTrial(bool rewardCollected)
    // {
    //     Debug.Log("CleanupTrial method called.");
    //     FreezePlayer();
    //     HideRewardIfCollected(rewardCollected);
    //     StopCountdownTimer();
    //     HideGrid();
    // }

    private void CleanupTrial(bool rewardCollected)
    {
        Debug.Log("CleanupTrial method called.");
        FreezePlayer();

        if (rewardCollected)
        {
            HideRewardIfCollected(rewardCollected);
            Debug.Log("Trial ended: Reward was collected");
        }
        else
        {
            rewardSpawner.ClearReward();
            Debug.Log("Trial ended: Reward not collected, clearing reward");
        }

        StopCountdownTimer();
        HideGrid();
    }

    /// <summary>
    /// Callback for when a trial ends. This method is subscribed to the ExperimentManager's OnTrialEnded event.
    /// </summary>
    private void OnTrialEnd(bool rewardCollected)
    {
        // This method is called by the ExperimentManager, so we don't need to do anything here
        // The next trial will be started by the ExperimentManager
    }
    // Add this method to update block and trial information
    // public void UpdateBlockAndTrialInfo(int blockNumber, int trialIndex, int effortLevel)
    // {
    //     currentBlockNumber = blockNumber;
    //     currentTrialIndex = trialIndex;
    //     currentEffortLevel = effortLevel;
    // }

    #endregion

    #region Spawning Methods
    /// <summary>
    /// Spawns the player at a position determined by the PlayerSpawner.
    /// </summary>
    private void SpawnPlayer()
    {
        Debug.Log("SpawnPlayer method called.");
        if (currentPlayer != null)
        {
            Debug.LogWarning("A player already exists. Destroying the old one before spawning a new one.");
            Destroy(currentPlayer);
        }

        if (playerSpawner != null)
        {
            Vector2 playerPosition = playerSpawner.SpawnPlayer().transform.position;
            // playerPosition = playerSpawnPosition;

            // Vector2 playerPosition = playerSpawner.GetRandomSpawnPosition();
            currentPlayer = playerSpawner.SpawnPlayer();

            if (currentPlayer != null)
            {
                Debug.Log($"Player spawned at position: {playerPosition}");
                playerInitialPosition = playerPosition;

                PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.gameObject.SetActive(true);
                    Debug.Log("PlayerController activated.");

                    // Set the required presses immediately
                    int pressesRequired = GetPressesRequired(currentEffortLevel);
                    playerController.SetPressesPerStep(pressesRequired);

                    // Enable movement explicitly
                    playerController.EnableMovement();
                    Debug.Log($"Player spawned and movement enabled at position: {playerPosition}");
                }
                else
                {
                    Debug.LogError("PlayerController component not found on spawned player!");
                }
            }
            else
            {
                Debug.LogError("Failed to spawn player!");
            }
        }
        else
        {
            Debug.LogError("PlayerSpawner is not assigned!");
        }
    }


    /// <summary>
    /// Spawns the reward at a position determined by the RewardSpawner.
    /// </summary>
    // private void SpawnReward()
    // {
    //     Debug.Log("SpawnReward method called.");
    //     if (rewardSpawner == null)
    //     {
    //         Debug.LogError("rewardSpawner is null. Attempting to find RewardSpawner in scene.");
    //         rewardSpawner = FindAnyObjectByType<RewardSpawner>();
    //         if (rewardSpawner == null)
    //         {
    //             Debug.LogError("RewardSpawner not found in scene. Cannot spawn reward.");
    //             return;
    //         }
    //     }

    //     if (experimentManager == null)
    //     {
    //         Debug.LogError("experimentManager is null. Attempting to find ExperimentManager.");
    //         experimentManager = ExperimentManager.Instance;
    //         if (experimentManager == null)
    //         {
    //             Debug.LogError("ExperimentManager not found. Cannot spawn reward.");
    //             return;
    //         }
    //     }

    //     // Get current block distance
    //     // float blockDistance = experimentManager.GetCurrentBlockDistance();
    //     float currentBlockDistance = experimentManager.GetCurrentBlockDistance();

    //     // Spawn reward at the specified distance

    //     Vector2 rewardPosition = rewardSpawner.GetSpawnPositionAtDistance(playerInitialPosition, currentBlockDistance);
    //     currentReward = rewardSpawner.SpawnReward(rewardPosition, experimentManager.GetCurrentBlockNumber(), experimentManager.GetCurrentTrialIndex(), GetPressesRequired(currentEffortLevel), Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue()));

    //     if (currentReward != null)
    //     {
    //         actualRewardPosition = currentReward.transform.position;
    //         Debug.Log($"Reward spawned at position: {actualRewardPosition}, Value: {experimentManager.GetCurrentTrialRewardValue()}, Presses required: {GetPressesRequired(currentEffortLevel)}");

    //         // Log the reward position
    //         experimentManager.LogRewardPosition(actualRewardPosition);
    //     }
    //     else
    //     {
    //         Debug.LogError("Failed to spawn reward!");
    //     }
    // }

    // private void SpawnReward()
    // {
    //     Debug.Log("SpawnReward method called.");

    //     // Validate required components
    //     if (gridManager == null || rewardSpawner == null || experimentManager == null)
    //     {
    //         Debug.LogError($"Missing required components: GridManager: {gridManager != null}, RewardSpawner: {rewardSpawner != null}, ExperimentManager: {experimentManager != null}");
    //         return;
    //     }

    //     // Validate player initial position
    //     if (playerInitialPosition == Vector2.zero)
    //     {
    //         Debug.LogError("Invalid player initial position");
    //         return;
    //     }

    //     try
    //     {
    //         // float currentBlockDistance = experimentManager.GetCurrentBlockDistance();
    //         float currentBlockDistance = 5f; // make it constant of 5 cells
    //         Debug.Log($"Getting spawn position at distance: {currentBlockDistance} from player position: {playerInitialPosition}");

    //         // Get spawn position with explicit error checking
    //         Vector2 rewardPosition = rewardSpawner.GetSpawnPositionAtDistance(playerInitialPosition, currentBlockDistance);

    //         if (rewardPosition == Vector2.zero)
    //         {
    //             Debug.LogError("Invalid reward position calculated");
    //             return;
    //         }

    //         currentReward = rewardSpawner.SpawnReward(
    //             rewardPosition,
    //             experimentManager.GetCurrentBlockNumber(),
    //             experimentManager.GetCurrentTrialIndex(),
    //             GetPressesRequired(currentEffortLevel),
    //             Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
    //         );

    //         if (currentReward != null)
    //         {
    //             actualRewardPosition = currentReward.transform.position;
    //             Debug.Log($"Reward successfully spawned at position: {actualRewardPosition}");
    //             experimentManager.LogRewardPosition(actualRewardPosition);
    //         }
    //         else
    //         {
    //             Debug.LogError("Failed to spawn reward object");
    //         }
    //     }
    //     catch (System.Exception e)
    //     {
    //         Debug.LogError($"Error spawning reward: {e.Message}\n{e.StackTrace}");
    //     }
    // }
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
    /// Enables player movement by activating the PlayerController.
    /// </summary>
    private void EnablePlayerMovement()
    {
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.EnableMovement();
                Debug.Log("Player movement enabled.");
                Debug.Log($"EnablePlayerMovement called - Player: {currentPlayer != null}, Controller: {currentPlayer?.GetComponent<PlayerController>() != null}");

            }
            else
            {
                Debug.LogError("PlayerController component not found on currentPlayer!");
            }
        }
        else
        {
            Debug.LogError("currentPlayer is null when trying to enable movement!");
        }
    }

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
        logManager.LogCollisionTime(currentTrialIndex);
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

    /// <summary>
    /// Callback for when the timer expires.
    /// </summary>
    // private void OnTimerExpired()
    // {
    //     if (isTrialActive && !rewardCollected && !isTrialEnded)
    //     {
    //         Debug.Log("Time's up! Ending trial.");
    //         EndTrial(false);
    //     }
    // }

    /// Only end the trial when timer expires
    private void OnTimerExpired()
    {
        if (isTrialActive && !isTrialEnded)
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
    private void ShowGrid()
    {
        if (gridManager != null)
        {
            gridManager.ShowGrid();
        }
        else
        {
            Debug.LogError("GridManager is null. Cannot show grid.");
        }
    }

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

        if (!isTrialActive || rewardCollected || isTrialEnded) return;

        if (collision && !scoreAdded)
        {
            rewardCollected = true;
            scoreAdded = true;

            float rewardValue = experimentManager.GetCurrentTrialRewardValue();
            // HideRewardIfCollected(rewardCollected);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(Mathf.RoundToInt(rewardValue), isFormalTrial: true); // it has been added in the ExperimentManager but commented out
                Debug.Log($"Score added in GameController: {Mathf.RoundToInt(rewardValue)}");
            }
            else
            {
                Debug.LogError("ScoreManager.Instance is null when trying to add score!");
            }

            // Hide the reward immediately upon collection
            if (currentReward != null)
            {
                currentReward.SetActive(false);
                // Destroy(currentReward);
                // HideRewardIfCollected(true);
                Debug.Log("Reward hidden upon collection.");
            }
            // HideRewardIfCollected(rewardCollected);
        }
        // Important change: Do NOT immediately end the trial
        // Let the countdown timer handle the trial end
        ExperimentManager.Instance.logManager.LogCollisionTime(ExperimentManager.Instance.GetCurrentTrialIndex());
        // Debug.Log("Calling EndTrial from RewardCollected");
        // EndTrial(collision);
    }
    #endregion

    #region Logging
    /// <summary>
    /// Logs the start of a trial.
    /// </summary>
    private void LogTrialStart() { }
    private void LogTrialEnd(bool rewardCollected, float trialDuration) { }
    public float GetActionReactionTime()
    {
        return actionReactionTime;
    }

    /// <summary>
    /// Logs the timing information for reward collection.
    /// </summary>
    public void LogRewardCollectionTiming(float collisionTime, float movementDuration)
    {
        Debug.Log($"Reward collected at time: {collisionTime}, Movement duration: {movementDuration}");
        experimentManager.LogRewardCollectionTiming(collisionTime, movementDuration);
    }

    private float CalculateEuclideanDistance(Vector2 start, Vector2 end)
    {
        return Vector2.Distance(start, end);
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Sets the number of presses required per step based on the effort level.
    /// </summary>
    private void SetPressesPerStep(int effortLevel)
    {
        if (effortLevel < 0 || effortLevel >= pressesPerEffortLevel.Length)
        {
            Debug.Log($"Invalid effort level: {effortLevel}");
            return;
        }

        int pressesRequired = pressesPerEffortLevel[effortLevel];
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

    /// <summary>
    /// Increments the button press count for the current trial.
    /// </summary>
    public void IncrementButtonPressCount()
    {
        buttonPressCount++;
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

    private void ValidateAndEnablePlayerMovement()
    {
        if (currentPlayer != null)
        {
            PlayerController controller = currentPlayer.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.EnableMovement();
                Debug.Log("Player movement enabled in GameController");
            }
            else
            {
                Debug.LogError("PlayerController component missing!");
            }
        }
        else
        {
            Debug.LogError("Player object is null!");
        }
    }
}