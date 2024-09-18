using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

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

        // Initialize components
        // Initialize();

    }
    #endregion

    #region Serialized Fields
    [SerializeField] private float maxTrialDuration = 10f;
    // [SerializeField] private float restDuration = 3f;
    [SerializeField] private float endTrialDelay = 0.5f;
    [SerializeField] private int[] pressesPerEffortLevel = { 1, 2, 3 };
    [SerializeField] private float sceneTransitionTimeout = 5f; // Maximum time to wait for scene transition

    #endregion

    #region Private Fields
    private PlayerSpawner playerSpawner;
    private PlayerController playerController;
    private RewardSpawner rewardSpawner;
    private GridManager gridManager;
    private DecisionManager decisionManager;
    private GameObject currentPlayer;
    private GameObject currentReward;
    private bool rewardCollected = false;
    private float trialStartTime;
    private float trialEndTime;
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
    private ExperimentManager experimentManager;
    private Coroutine trialTimerCoroutine;
    private int currentEffortLevel = 0;
    private int currentBlockIndex = 0;
    private int currentTrialIndex = 0;
    private bool isInitialized = false;
    #endregion

    #region Unity Lifecycle Methods
    private void Start()
    {
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Set current effort level
        currentEffortLevel = (int)experimentManager.GetCurrentTrialEV();

        // Set presses per step based on effort level
        SetPressesPerStep(currentEffortLevel);

        if (enabled)
        {
            StartCoroutine(StartTrial());
        }
        // Initialize components
        Initialize();
        InitializeLogManager();
    }

    private void Update()
    {
        if (isTrialActive)
        {
            // Check if trial time is up
            // if (Time.time - trialStartTime >= maxTrialDuration)
            // {
            //     EndTrial(false);
            // }
            // if (countdownTimer.timeLeft <= 0)
            // {
            //     EndTrial(false);
            // }
            if (countdownTimer != null && countdownTimer.TimeLeft <= 0)
            {
                EndTrial(false);
            }
        }
    }

    private void InitializeLogManager()
    {
        logManager = FindObjectOfType<LogManager>();
        if (logManager == null)
        {
            Debug.LogWarning("LogManager not found in the scene. Creating a new instance.");
            GameObject logManagerObject = new GameObject("LogManager");
            LogManager.instance = logManagerObject.AddComponent<LogManager>();
            // logManager = logManagerObject.AddComponent<LogManager>();

            Debug.Log("LogManager initialized in GameController");
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
        // Find ExperimentManager (should be in DontDestroyOnLoad)
        experimentManager = ExperimentManager.Instance;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found! Ensure it's properly initialized.");
            return;
        }

        // Subscribe to ExperimentManager events
        experimentManager.OnTrialEnded += OnTrialEnd;

        // Find ScoreManager
        scoreManager = ScoreManager.Instance;
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager not found! Ensure it's properly initialized.");
        }
        isInitialized = true;
    }

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

    // private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    // {
    //     if (scene.name == "GridWorld")
    //     {
    //         StartCoroutine(SetupGridWorldScene());
    //     }
    // }
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
    private IEnumerator SetupGridWorldScene()
    {
        yield return new WaitForSeconds(0.1f); // Short delay to ensure all components are ready

        // Find all necessary components in the GridWorld scene
        countdownTimer = FindObjectOfType<CountdownTimer>();
        scoreManager = FindObjectOfType<ScoreManager>();
        gridManager = FindObjectOfType<GridManager>();
        playerSpawner = FindObjectOfType<PlayerSpawner>();
        rewardSpawner = FindObjectOfType<RewardSpawner>();

        // Validate that all components are found
        ValidateGridWorldComponents();

        // Start the trial
        StartCoroutine(StartTrial());
    }

    /// <summary>
    /// Sets up components specific to the DecisionPhase scene.
    /// </summary>
    private void SetupDecisionPhaseScene()
    {
        decisionManager = FindObjectOfType<DecisionManager>();
        if (decisionManager == null) Debug.LogError("DecisionManager not found in the DecisionPhase scene!");
    }

    /// <summary>
    /// Validates that all required components for the GridWorld scene are assigned.
    /// </summary>
    private void ValidateGridWorldComponents()
    {
        if (countdownTimer == null) Debug.LogError("CountdownTimer not found in the GridWorld scene!");
        if (scoreManager == null) Debug.LogError("ScoreManager not found in the GridWorld scene!");
        if (gridManager == null) Debug.LogError("GridManager not found in the GridWorld scene!");
        if (playerSpawner == null) Debug.LogError("PlayerSpawner not found in the GridWorld scene!");
        if (rewardSpawner == null) Debug.LogError("RewardSpawner not found in the GridWorld scene!");
    }
    #endregion

    #region Trial Control Methods
    /// <summary>
    /// Starts a new trial, spawning player and reward, and setting up the timer.
    /// </summary>
    public IEnumerator StartTrial()
    {
        Debug.Log("StartTrial called. Initializing trial...");
        yield return new WaitForSeconds(0.5f); // Short delay to ensure all components are ready

        if (isTrialActive)
        {
            Debug.Log("Trial already active, ignoring start request.");
            yield break;
        }

        isTrialActive = true;
        isTrialEnded = false;
        rewardCollected = false;
        scoreAdded = false;
        buttonPressCount = 0;
        trialStartTime = Time.time;

        ShowGrid();
        SpawnPlayer();
        SpawnReward();

        // Start the countdown timer
        // if (countdownTimer != null)
        // {
        //     countdownTimer.StartTimer(maxTrialDuration);
        //     StartCoroutine(CheckTimerAccuracyRoutine());

        //     trialTimerCoroutine = StartCoroutine(TrialTimerRoutine());
        // }
        // else
        // {
        //     Debug.LogError("CountdownTimer is not assigned!");
        // }

        // Set current effort level
        currentEffortLevel = experimentManager != null ? (int)experimentManager.GetCurrentTrialEV() : 1;
        Debug.Log($"Current effort level set to: {currentEffortLevel}");

        // Set presses per step based on effort level
        SetPressesPerStep(currentEffortLevel);

        // Start the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StartTimer(maxTrialDuration);
            countdownTimer.OnTimerExpired += OnTimerExpired;
            StartCoroutine(CheckTimerAccuracyRoutine());
        }
        else
        {
            Debug.LogError("CountdownTimer is not assigned!");
        }

        // Check if reward was successfully spawned
        if (currentReward != null)
        {
            Debug.Log($"Reward spawned at: {currentReward.transform.position}");
        }
        else
        {
            Debug.LogError("Failed to spawn reward!");
        }

        // Set current effort level
        // currentEffortLevel = (int)experimentManager.GetCurrentTrialEV();

        // Log the trial start
        // Debug.Log($"Trial started - Player at: {currentPlayer.transform.position}, Reward at: {actualRewardPosition}, Effort Level: {currentEffortLevel}");

        // Enable player movement
        if (currentPlayer != null)
        {
            PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.EnableMovement();
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

        // Record initial player position
        // if (currentPlayer != null)
        // {
        //     playerInitialPosition = currentPlayer.transform.position;
        //     Debug.Log($"Initial player position: {playerInitialPosition}");
        // }
        // else
        // {
        //     Debug.LogError("Player not spawned correctly!");
        // }
        // Log the start of the trial
        LogTrialStart();
    }


    private void OnTimerExpired()
    {
        if (isTrialActive && !rewardCollected && !isTrialEnded)
        {
            Debug.Log("Time's up! Ending trial.");
            EndTrial(false);
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
        float trialDuration = Time.time - trialStartTime;

        // Stop all coroutines
        StopAllCoroutines();

        // Stop the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
            // unsubscribed from the OnTimerExpired event to prevent memory leaks
            // countdownTimer.OnTimerExpired -= () => EndTrial(false);
            countdownTimer.OnTimerExpired -= OnTimerExpired;
        }
        else
        {
            Debug.LogError("CountdownTimer is null in GameController!");
        }

        // Get final position and calculate distance
        // if (currentPlayer != null)
        // {
        //     PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
        //     if (playerController != null)
        //     {
        //         playerFinalPosition = playerController.GetCurrentPosition();
        //         float distanceMoved = CalculateEuclideanDistance(playerInitialPosition, playerFinalPosition);
        //         Debug.Log($"Player moved from {playerInitialPosition} to {playerFinalPosition}. Total distance: {distanceMoved}");

        //         // Log the distance moved
        //         logManager.LogPlayerMovement(playerInitialPosition, playerFinalPosition, distanceMoved);
        //     }
        // }

        // Freeze the player if it exists
        if (currentPlayer != null)
        {
            FreezePlayer();
            // playerFinalPosition = currentPlayer.transform.position;
            Debug.Log($"Final player position: {playerFinalPosition}");
            Debug.Log("Player frozen in place.");
        }
        else
        {
            Debug.LogWarning("currentPlayer is null when ending trial. Unable to freeze player.");
        }

        // Hide the reward only if it was collected
        if (rewardCollected && currentReward != null)
        {
            currentReward.SetActive(false);
            Debug.Log("Reward hidden after collection.");
        }

        // Log trial data
        experimentManager.LogTrialData(rewardCollected, trialDuration, buttonPressCount);

        // Log trial data
        LogTrialEnd(rewardCollected, trialDuration);

        // Clean up the scene
        CleanupTrial(rewardCollected);

        // Notify ExperimentManager that the trial has ended
        StartCoroutine(EndTrialCoroutine(rewardCollected));
    }

    private float CalculateEuclideanDistance(Vector2 start, Vector2 end)
    {
        return Vector2.Distance(start, end);
    }

    private void LogTrialStart()
    {
        string logMessage = $"Trial Start," +
                            $"Time: {System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}," +
                            $"TrialIndex: {currentTrialIndex}," +
                            $"BlockIndex: {currentBlockIndex}," +
                            $"EffortLevel: {currentEffortLevel}," +
                            $"RewardPosition: {actualRewardPosition}," +
                            $"InitialPlayerPosition: {playerInitialPosition}," +
                            $"RewardValue: {experimentManager.GetCurrentTrialRewardValue()}";

        SafeLog(logMessage);
    }

    private void LogTrialEnd(bool rewardCollected, float trialDuration)
    {
        float distanceMoved = Vector2.Distance(playerInitialPosition, playerFinalPosition);

        string logMessage = $"Trial End," +
                            $"Time: {System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}," +
                            $"TrialIndex: {currentTrialIndex}," +
                            $"BlockIndex: {currentBlockIndex}," +
                            $"EffortLevel: {currentEffortLevel}," +
                            $"RewardCollected: {rewardCollected}," +
                            $"TrialDuration: {trialDuration:F2}s," +
                            $"ButtonPressCount: {buttonPressCount}," +
                            $"InitialPlayerPosition: {playerInitialPosition}," +
                            $"FinalPlayerPosition: {playerFinalPosition}," +
                            $"DistanceMoved: {distanceMoved:F2}," +
                            $"RewardPosition: {actualRewardPosition}," +
                            $"RewardValue: {experimentManager.GetCurrentTrialRewardValue()}";

        SafeLog(logMessage);
    }

    private void SafeLog(string message)
    {
        LogManager.LogManagerHelper.Log(message);

        if (logManager != null)
        {
            logManager.WriteTimeStampedEntry(message);
        }
        else
        {
            Debug.LogWarning($"LogManager is null. Logging to Debug.Log instead. Message: {message}");
            Debug.Log(message);
        }
    }

    private void SetPressesPerStep(int effortLevel)
    {
        if (effortLevel <= 0 || effortLevel > pressesPerEffortLevel.Length)
        {
            Debug.LogError($"Invalid effort level: {effortLevel}. Using default value.");
            effortLevel = 1;
        }

        int pressesRequired = pressesPerEffortLevel[effortLevel - 1];

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
            Debug.LogError("currentPlayer is null when trying to set presses per step!");
        }
    }
    /// <summary>
    /// Freezes the player's movement.
    /// </summary>
    private void FreezePlayer()
    {
        PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.DisableMovement();
            Debug.Log("Player movement disabled.");
        }
        else
        {
            Debug.LogError("PlayerController component not found on currentPlayer!");
        }
    }

    /// <summary>
    /// Coroutine to handle the trial timer and end the trial if time runs out.
    /// </summary>
    private IEnumerator TrialTimerRoutine()
    {
        yield return new WaitForSeconds(maxTrialDuration);
        if (isTrialActive && !rewardCollected && !isTrialEnded)
        {
            Debug.Log("Time's up! Ending trial.");
            FreezePlayer();
            EndTrial(false);
        }
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

    private IEnumerator ForceSceneTransition()
    {
        Debug.Log("Attempting forced scene transition to DecisionPhase.");
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("DecisionPhase");
        asyncLoad.allowSceneActivation = true;

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

    /// <summary>
    /// Cleans up the scene after a trial ends.
    /// </summary>
    private void CleanupTrial(bool rewardCollected)
    {
        Debug.Log("CleanupTrial method called.");

        if (currentPlayer != null)
        {
            FreezePlayer();
            Debug.Log("Player frozen during cleanup.");
        }
        else
        {
            Debug.LogWarning("currentPlayer is null during cleanup.");
        }

        if (rewardCollected && currentReward != null)
        {
            currentReward.SetActive(false);
            Debug.Log("Reward hidden during cleanup.");
        }

        if (countdownTimer != null)
        {
            countdownTimer.StopTimer();
            Debug.Log("Countdown timer stopped during cleanup.");
        }
        else
        {
            Debug.LogWarning("countdownTimer is null during cleanup.");
        }

        HideGrid();
        Debug.Log("Grid hidden during cleanup.");
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
        if (currentPlayer != null)
        {
            Debug.LogWarning("A player already exists. Destroying the old one before spawning a new one.");
            Destroy(currentPlayer);
        }

        if (playerSpawner != null)
        {
            Vector2 playerPosition = playerSpawner.GetRandomSpawnPosition();
            currentPlayer = playerSpawner.SpawnPlayer(playerPosition);
            if (currentPlayer != null)
            {
                Debug.Log($"Player spawned at position: {playerPosition}");
                PlayerController playerController = currentPlayer.GetComponent<PlayerController>();
                if (playerController == null)
                {
                    Debug.LogError("PlayerController component not found on spawned player!");
                }
                else
                {
                    playerController.gameObject.SetActive(true);
                    Debug.Log("PlayerController activated.");
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

    private void SpawnReward()
    {
        Debug.Log("SpawnReward method called.");
        if (rewardSpawner == null)
        {
            Debug.LogError("rewardSpawner is null. Attempting to find RewardSpawner in scene.");
            rewardSpawner = FindObjectOfType<RewardSpawner>();
            if (rewardSpawner == null)
            {
                Debug.LogError("RewardSpawner not found in scene. Cannot spawn reward.");
                return;
            }
        }

        if (experimentManager == null)
        {
            Debug.LogError("experimentManager is null. Attempting to find ExperimentManager.");
            experimentManager = ExperimentManager.Instance;
            if (experimentManager == null)
            {
                Debug.LogError("ExperimentManager not found. Cannot spawn reward.");
                return;
            }
        }

        Vector2 rewardPosition = rewardSpawner.GetRandomSpawnPosition();
        int pressesRequired = GetPressesRequired(currentEffortLevel);
        float rewardValue = experimentManager.GetCurrentTrialRewardValue();

        currentReward = rewardSpawner.SpawnReward(rewardPosition, currentBlockIndex, currentTrialIndex, pressesRequired, Mathf.RoundToInt(rewardValue));

        if (currentReward != null)
        {
            actualRewardPosition = currentReward.transform.position;
            Debug.Log($"Reward spawned at position: {actualRewardPosition}, Value: {rewardValue}, Presses required: {pressesRequired}");

            // Log the reward position
            experimentManager.LogRewardPosition(actualRewardPosition);
        }
        else
        {
            Debug.LogError("Failed to spawn reward!");
        }
    }
    // private void SpawnReward()
    // {
    //     if (rewardSpawner != null)
    //     {
    //         Vector2 rewardPosition = rewardSpawner.GetRandomSpawnPosition();
    //         int pressesRequired = GetPressesRequired(currentEffortLevel);
    //         float rewardValue = experimentManager.GetCurrentTrialRewardValue();
    //         currentReward = rewardSpawner.SpawnReward(rewardPosition, currentBlockIndex, currentTrialIndex, pressesRequired, Mathf.RoundToInt(rewardValue));

    //         if (currentReward != null)
    //         {
    //             actualRewardPosition = currentReward.transform.position;
    //             Debug.Log($"Reward spawned at position: {actualRewardPosition}, Value: {rewardValue}, Presses required: {pressesRequired}");

    //             // Log the reward position
    //             experimentManager.LogRewardPosition(actualRewardPosition);
    //         }
    //         else
    //         {
    //             Debug.LogError("Failed to spawn reward!");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogError("RewardSpawner is not assigned!");
    //     }
    // }

    /// <summary>
    /// Logs the timing information for reward collection.
    /// </summary>
    public void LogRewardCollectionTiming(float collisionTime, float movementDuration)
    {
        Debug.Log($"Reward collected at time: {collisionTime}, Movement duration: {movementDuration}");
        experimentManager.LogRewardCollectionTiming(collisionTime, movementDuration);
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
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(Mathf.RoundToInt(rewardValue));
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
                Debug.Log("Reward hidden upon collection.");
            }
        }

        Debug.Log("Calling EndTrial from RewardCollected");
        EndTrial(collision);
    }
    // public void RewardCollected()
    // {
    //     if (!isTrialActive) return;

    //     int buttonPresses = PlayerController.Instance.GetButtonPressCount();
    //     int pressesRequired = GetPressesRequired(currentEffortLevel);
    //     if (buttonPresses >= pressesRequired)
    //     {
    //         rewardCollected = true;
    //         float rewardValue = experimentManager.GetCurrentTrialRewardValue();
    //         scoreManager.AddScore(Mathf.RoundToInt(rewardValue));
    //         Debug.Log($"Score added in GameController: {Mathf.RoundToInt(rewardValue)}");

    //         // Stop the timer when the reward is collected
    //         if (countdownTimer != null)
    //         {
    //             countdownTimer.StopTimer();
    //         }
    //         else
    //         {
    //             Debug.LogError("CountdownTimer is null in GameController!");
    //         }

    //         EndTrial(true);
    //     }
    //     else
    //     {
    //         Debug.Log($"Not enough button presses. Required: {pressesRequired}, Current: {buttonPresses}");
    //     }
    // }
    // public void RewardCollected(bool collision)
    // {
    //     if (!isTrialActive || rewardCollected || isTrialEnded) return;

    //     if (collision && !scoreAdded)
    //     {
    //         rewardCollected = true;
    //         scoreAdded = true;
    //         float rewardValue = experimentManager.GetCurrentTrialRewardValue();
    //         if (ScoreManager.Instance != null)
    //         {
    //             ScoreManager.Instance.AddScore(Mathf.RoundToInt(rewardValue));
    //             Debug.Log($"Score added in GameController: {Mathf.RoundToInt(rewardValue)}");
    //         }
    //         else
    //         {
    //             Debug.LogError("ScoreManager.Instance is null when trying to add score!");
    //         }
    //         // // Stop the timer when the reward is collected
    //         // if (countdownTimer != null)
    //         // {
    //         //     countdownTimer.StopTimer();
    //         // }
    //         // else
    //         // {
    //         //     Debug.LogError("CountdownTimer is null in GameController!");
    //         // }
    //     }

    //     EndTrial(collision);
    // }

    // public void RewardCollected(bool collision)
    // {
    //     if (!isTrialActive) return;

    //     PlayerController playerController = currentPlayer?.GetComponent<PlayerController>();
    //     if (playerController == null)
    //     {
    //         Debug.LogError("PlayerController not found when collecting reward!");
    //         return;
    //     }

    //     int buttonPresses = playerController.GetButtonPressCount();
    //     int pressesRequired = GetPressesRequired(currentEffortLevel);

    //     if (buttonPresses >= pressesRequired && collision)
    //     {
    //         rewardCollected = true;
    //         float rewardValue = experimentManager.GetCurrentTrialRewardValue();
    //         if (scoreManager != null)
    //         {
    //             scoreManager.AddScore(Mathf.RoundToInt(rewardValue));
    //             Debug.Log($"Score added in GameController: {Mathf.RoundToInt(rewardValue)}");
    //         }
    //         else
    //         {
    //             Debug.LogError("ScoreManager is null when trying to add score!");
    //         }

    //         // Stop the timer when the reward is collected
    //         if (countdownTimer != null)
    //         {
    //             countdownTimer.StopTimer();
    //         }
    //         else
    //         {
    //             Debug.LogError("CountdownTimer is null in GameController!");
    //         }

    //         EndTrial(true);
    //     }
    //     else if (!collision)
    //     {
    //         EndTrial(false);
    //     }
    //     else
    //     {
    //         Debug.Log($"Not enough button presses. Required: {pressesRequired}, Current: {buttonPresses}");
    //     }
    // }
    #endregion

    #region Public Methods
    /// <summary>
    /// Increments the button press count for the current trial.
    /// </summary>
    public void IncrementButtonPressCount()
    {
        buttonPressCount++;
    }
    #endregion
}