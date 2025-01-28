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
    private readonly Dictionary<Type, UnityEngine.Component> cachedComponents =
        new Dictionary<Type, UnityEngine.Component>();
    #endregion

    #region Configuration
    [SerializeField] private float maxTrialDuration = 5f;
    [SerializeField] private float startTrialDelay = 0.1f;
    [SerializeField] private float endTrialDelay = 0.1f;
    [SerializeField] private int[] pressesPerEffortLevel = { 1, 2, 3 };
    private float decisionPhaseStartTime;
    private float decisionMadeTime;
    private float reactionTime;

    public enum DecisionType
    {
        Work,
        Skip,
        NoDecision
    }

    private readonly Dictionary<DecisionType, float> penaltyDurations = new Dictionary<DecisionType, float>()
    {
        { DecisionType.Skip, 3f },
        { DecisionType.NoDecision, 5f }
    };
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

    // State validation
    private readonly HashSet<string> requiredComponents = new HashSet<string>
    {
        "GridManager",
        "PlayerSpawner",
        "RewardSpawner",
        "CountdownTimer",
        "ScoreManager"
    };
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

        // // Check if we're starting practice trials
        // if (SceneManager.GetActiveScene().name == "PracticeDecisionPhase")
        // {
        //     InitializePracticeTrials();
        // }

        // Retrieve effort level for practice trials
        if (isPracticingTrials)
        {
            currentEffortLevel = PlayerPrefs.GetInt("CurrentPracticeEffortLevel", currentEffortLevel);
            Debug.Log($"Retrieved Practice Trial Effort Level: {currentEffortLevel}");
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
    /// Initializes the practice trial sequence
    /// </summary>
    private void InitializePracticeTrials()
    {
        Debug.Log("GameController: Initializing Practice Trials");
        isPracticingTrials = true;
        currentPracticeTrialCount = 0;
    }

    /// <summary>
    /// Modifies the EndTrialCoroutine to handle practice trial flow
    /// </summary>
    // private IEnumerator EndTrialCoroutine(bool rewardCollected)
    // {
    //     Debug.Log("EndTrialCoroutine started.");
    //     yield return new WaitForSeconds(endTrialDelay);

    //     // Existing end trial logic for both practice and formal trials
    //     if (experimentManager != null)
    //     {
    //         experimentManager.EndTrial(rewardCollected);
    //     }
    //     else
    //     {
    //         Debug.LogError("ExperimentManager is null when trying to end the trial!");
    //     }

    //     // Handle practice trial progression
    //     if (isPracticingTrials)
    //     {
    //         currentPracticeTrialCount++;
    //         Debug.Log($"Practice Trial {currentPracticeTrialCount} completed");

    //         if (currentPracticeTrialCount >= totalPracticeTrials)
    //         {
    //             Debug.Log("All practice trials completed. Moving to GetReadyCheck scene.");
    //             SceneManager.LoadScene("GetReadyCheck");
    //             isPracticingTrials = false;
    //             yield break;
    //         }

    //         // Move to next practice decision phase
    //         SceneManager.LoadScene("PracticeDecisionPhase");
    //     }

    //     // Scene transition logic
    //     float startTime = Time.time;
    //     while (SceneManager.GetActiveScene().name == "PracticeGridWorld" && Time.time - startTime < sceneTransitionTimeout)
    //     {
    //         Debug.Log($"Waiting for scene transition... Time elapsed: {Time.time - startTime}s");
    //         yield return new WaitForSeconds(0.5f);
    //     }

    //     // For practice trials, always return to PracticeDecisionPhase
    //     if (isPracticingTrials)
    //     {
    //         SceneManager.LoadScene("PracticeDecisionPhase");
    //     }
    //     else
    //     {
    //         // Existing force scene transition logic for formal trials
    //         StartCoroutine(ForceSceneTransition());
    //     }
    // }

    private IEnumerator EndTrialCoroutine(bool rewardCollected)
    {
        Debug.Log("EndTrialCoroutine started.");
        yield return new WaitForSeconds(endTrialDelay);

        // Reset trial state
        ResetTrialState();

        // Handle practice trial progression
        if (isPracticingTrials)
        {
            PracticeManager.Instance.HandleGridWorldOutcome(true);
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
    /// Called when a new scene is loaded. Sets up necessary components for the current scene.
    /// </summary>
    // private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    // {
    //     if (scene.name == "GridWorld")
    //     {
    //         StartCoroutine(SetupGridWorldScene());
    //     }
    //     else if (scene.name == "DecisionPhase")
    //     {
    //         SetupDecisionPhaseScene();
    //     }
    // }

    /// <summary>
    /// Modified OnSceneLoaded to handle practice trial scenes
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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

    /// <summary>
    /// Coroutine for setting up Practice GridWorld Scene
    /// </summary>
    // private IEnumerator SetupPracticeGridWorldScene()
    // {
    //     Debug.Log("Setting up Practice GridWorld Scene...");
    // Debug.Log($"Current Practice Trial Index: {PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1)}");

    //     // Use existing SetupGridWorldScene method logic
    //     // But with modifications for practice trials
    //     yield return StartCoroutine(SetupGridWorldScene());

    //     // Additional practice-specific setup if needed
    //     // For example, using different reward sprites or spawn logic
    //     // SpawnPracticeReward();
    //     // practiceManager.GetCurrentPracticeTrialSprite();
    //     SpawnPracticeReward();

    // // Explicitly enable player movement
    // EnablePlayerMovement();
    // Debug.Log("Player movement explicitly enabled in SetupPracticeGridWorldScene");

    //         Debug.Log($"GridManager found: {FindAnyObjectByType<GridManager>() != null}");
    // Debug.Log($"PlayerController found: {FindAnyObjectByType<PlayerController>() != null}");

    // }

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
            //         StartCoroutine(rewardSpawner.SpawnPlayerAndRewardSynchronized(
            //     playerInitialPosition,
            //     currentPracticeTrialCount,
            //     currentEffortLevel,
            //     GetPressesRequired(currentEffortLevel),
            //     Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
            // ));
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
    /// Optional: Method to customize reward spawning for practice trials
    /// </summary>
    // private void SpawnPracticeReward()
    // {
    //     // Custom reward spawning logic for practice trials
    //     // Could use different reward prefabs or spawn rules
    //     Debug.Log("Spawning Practice Trial Reward");

    //     // Example: Use a different reward spawning method
    //             rewardSpawner.SpawnReward(
    //                         playerInitialPosition,
    //                         experimentManager.GetCurrentBlockNumber(),
    //                         experimentManager.GetCurrentTrialIndex(),
    //                         GetPressesRequired(currentEffortLevel),
    //                         Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
    //     );
    // }

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
            // EnablePlayerMovement();

            if (currentPlayer != null)
            {
                PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    // Set required presses and enable movement
                    int currentEffortLevel = PlayerPrefs.GetInt("CurrentPracticeEffortLevel", 1);
                    int pressesRequired = GetPressesRequired(currentEffortLevel);
                    playerController.SetPressesPerStep(pressesRequired);
                    playerController.EnableMovement();
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

    // In GameController.cs

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

        try
        {
            // Get current effort level from PracticeManager with null check
            int currentEffortLevel = practiceManager != null ? practiceManager.GetCurrentTrialEffortLevel() : 1;

            // Spawn player with explicit movement enabling
            SpawnPlayer();
            if (currentPlayer != null)
            {
                PlayerController controller = currentPlayer.GetComponent<PlayerController>();
                if (controller != null)
                {
                    // Explicitly reset counter
                    controller.ResetCounters();

                    // Set required presses and enable movement
                    int pressesRequired = GetPressesRequired(currentEffortLevel);
                    controller.SetPressesPerStep(pressesRequired);
                    controller.EnableMovement();
                    Debug.Log($"Practice Player initialized with {pressesRequired} presses required. Movement enabled.");
                }
                else
                {
                    Debug.LogError("PlayerController not found on spawned player!");
                    yield break;
                }
            }
            else
            {
                Debug.LogError("Failed to spawn player!");
                yield break;
            }

            // Only spawn reward if player exists and is properly positioned
            if (currentPlayer != null)
            {
                SpawnPracticeReward();
            }
            else
            {
                Debug.LogError("Cannot spawn reward - player not properly initialized!");
                yield break;
            }

            // Start trial if everything is ready
            StartCoroutine(StartTrial());
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during practice spawn sequence: {e.Message}\n{e.StackTrace}");
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

    // private bool isInitializingTrial = false; // Add this field

    public IEnumerator StartTrial()
    {
        Debug.Log("StartTrial called. Initializing trial...");

        // Add a guard to prevent multiple simultaneous trial starts
        if (isInitializingTrial)
        {
            Debug.Log("Trial initialization already in progress. Skipping.");
            yield break;
        }

        // If it's a practice trial, log additional details
        if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
        {
            if (PracticeManager.Instance != null && PracticeManager.Instance.GetCurrentPracticeTrial() != null)
            {
                Debug.Log($"Practice Trial Details - Effort Level: {PracticeManager.Instance.GetCurrentPracticeTrial().effortLevel}");
            }
        }

        yield return new WaitForSeconds(startTrialDelay);

        // Early exit if trial is already active
        if (isTrialActive)
        {
            Debug.LogWarning("Trial already active, ignoring start request.");
            yield break;
        }

        try
        {
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

            if (experimentManager == null)
            {
                experimentManager = ExperimentManager.Instance;
                if (experimentManager == null)
                {
                    Debug.LogError("ExperimentManager not found!");
                    isInitializingTrial = false;
                    yield break;
                }
            }

            if (gridWorldManager == null)
            {
                gridWorldManager = GridWorldManager.Instance;
                if (gridWorldManager == null)
                {
                    Debug.LogError("GridWorldManager not found!");
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

            // Setup environment
            ShowGrid();
            SpawnPlayer();

            // Only proceed with reward spawning if player exists
            if (currentPlayer != null)
            {
                if (rewardSpawner != null)
                {
                    rewardSpawner.SpawnReward(
                        playerInitialPosition,
                        experimentManager.GetCurrentBlockNumber(),
                        experimentManager.GetCurrentTrialIndex(),
                        GetPressesRequired(currentEffortLevel),
                        Mathf.RoundToInt(experimentManager.GetCurrentTrialRewardValue())
                    );
                }
                else
                {
                    Debug.LogError("RewardSpawner is null!");
                    isInitializingTrial = false;
                    yield break;
                }

                // Set current effort level and presses per step
                currentEffortLevel = experimentManager.GetCurrentTrialEV();
                SetPressesPerStep(currentEffortLevel);

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
                LogTrialStart();

                // Log debug information
                Debug.Log($"Starting Trial {experimentManager.GetCurrentTrialIndex()} in Block {experimentManager.GetCurrentBlockNumber()}. " +
                         $"Effort Level: {currentEffortLevel}, Presses Required: {GetPressesRequired(currentEffortLevel)}");
            }
            else
            {
                Debug.LogError("Failed to spawn player!");
                isInitializingTrial = false;
                yield break;
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

    /// <summary>
    /// Ends the current trial and notifies the ExperimentManager.
    /// </summary>
    public void EndTrial(bool rewardCollected)
    {
        if (!isTrialActive || isTrialEnded)
        {
            Debug.Log("EndTrial called but trial is not active or already ended");
            return;
        }

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

        // Reset trial state flags
        isTrialActive = false;
        isTrialEnded = false;
        isInitializingTrial = false;

        // Notify ExperimentManager that the trial has ended
        StartCoroutine(EndTrialCoroutine(rewardCollected));

        // Notify GridWorldManager
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
    // private void SpawnPlayer()
    // {
    //     Debug.Log("SpawnPlayer method called.");
    //     if (currentPlayer != null)
    //     {
    //         Debug.LogWarning("A player already exists. Destroying the old one before spawning a new one.");
    //         Destroy(currentPlayer);
    //     }

    //     if (playerSpawner != null)
    //     {
    //         Vector2 playerPosition = playerSpawner.SpawnPlayer().transform.position;
    //         // playerPosition = playerSpawnPosition;

    //         // Vector2 playerPosition = playerSpawner.GetRandomSpawnPosition();
    //         currentPlayer = playerSpawner.SpawnPlayer();

    //         if (currentPlayer != null)
    //         {
    //             Debug.Log($"Player spawned at position: {playerPosition}");
    //             playerInitialPosition = playerPosition;

    //             PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
    //             if (playerController != null)
    //             {
    //                 playerController.gameObject.SetActive(true);
    //                 Debug.Log("PlayerController activated.");

    //                 // Set the required presses immediately
    //                 int pressesRequired = GetPressesRequired(currentEffortLevel);
    //                 Debug.Log($"Setting presses required to: {pressesRequired}");
    //                 playerController.SetPressesPerStep(pressesRequired);

    //                 // Enable movement explicitly
    //                 playerController.EnableMovement();
    //                 Debug.Log($"Player spawned and movement enabled at position: {playerPosition}");
    //             }
    //             else
    //             {
    //                 Debug.LogError("PlayerController component not found on spawned player!");
    //             }
    //         }
    //         else
    //         {
    //             Debug.LogError("Failed to spawn player!");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError("PlayerSpawner is not assigned!");
    //     }


    //     if (PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1)
    //     {
    //         int currentEffortLevel = PlayerPrefs.GetInt("CurrentPracticeEffortLevel", 1);
    //         Debug.Log($"Practice Player Spawn - Effort Level: {currentEffortLevel}");
    //     }
    // }

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
    /// Enables player movement by activating the PlayerController.
    /// </summary>
    private void EnablePlayerMovement()
    {
        Debug.Log("EnablePlayerMovement called in GameController");

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
                Destroy(currentReward);
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
        Debug.Log($"SetPressesPerStep called with effort level: {effortLevel}");
        Debug.Log($"pressesPerEffortLevel array: {string.Join(", ", pressesPerEffortLevel)}");

        // Clamp the effort level to ensure it's within bounds
        effortLevel = Mathf.Clamp(effortLevel, 0, pressesPerEffortLevel.Length - 1);


        if (effortLevel < 0 || effortLevel >= pressesPerEffortLevel.Length)
        {
            Debug.LogError($"Invalid effort level: {effortLevel}");
            return;
        }

        int pressesRequired = pressesPerEffortLevel[effortLevel];
        Debug.Log($"Presses required for effort level {effortLevel}: {pressesRequired}");

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