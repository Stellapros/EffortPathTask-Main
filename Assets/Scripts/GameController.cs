using UnityEngine;
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
    [SerializeField] private float maxTrialDuration = 10f;
    [SerializeField] private float startTrialDelay = 0.5f;
    [SerializeField] private float endTrialDelay = 0.5f;
    [SerializeField] private int[] pressesPerEffortLevel = { 1, 2, 3 }; // Default values
    [SerializeField] private float sceneTransitionTimeout = 5f; // Maximum time to wait for scene transition
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
        experimentManager = ExperimentManager.Instance;
        gridWorldManager = GridWorldManager.Instance;

        // Get initial block and trial information from ExperimentManager
        currentBlockNumber = experimentManager.GetCurrentBlockNumber();
        currentTrialIndex = experimentManager.GetCurrentTrialIndex();
        // currentEffortLevel = (int)experimentManager.GetCurrentTrialEV();
        // currentEffortLevel = Mathf.RoundToInt(experimentManager.GetCurrentTrialEV());

        // Set current effort level and presses per step
        currentEffortLevel = (int)experimentManager.GetCurrentTrialEV();
        SetPressesPerStep(currentEffortLevel);

        // if (enabled)
        // {
        //     StartCoroutine(StartTrial());
        // }

        // Initialize components
        Initialize();
        InitializeLogManager();
        LoadCalibratedPressesPerEffortLevel();
    }

    private void Update()
    {
        if (isTrialActive && countdownTimer != null)
        {
            Debug.Log($"Time left: {countdownTimer.TimeLeft}");
            if (countdownTimer.TimeLeft <= 0)
            {
                Debug.Log("Time's up! Ending trial.");
                EndTrial(false);
            }
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
    }

    /// <summary>
    /// Initializes the LogManager component.
    /// </summary>
    private void InitializeLogManager()
    {
        logManager = FindObjectOfType<LogManager>();
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

        // LogManager.instance.LogEvent("GridWorldStart", new Dictionary<string, object>
        // {
        //     {"TrialNumber", currentTrialIndex + 1},
        //     {"BlockNumber", currentBlockNumber}
        // });
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
        yield return new WaitForSeconds(startTrialDelay); // Short delay to ensure all components are ready

        if (isTrialActive)
        {
            Debug.Log("Trial already active, ignoring start request.");
            yield break;
        }

        // Reset trial variables
        isTrialActive = true;
        isTrialEnded = false;
        rewardCollected = false;
        scoreAdded = false;
        buttonPressCount = 0;
        trialStartTime = Time.time;

        // Setup the trial environment
        ShowGrid();
        SpawnPlayer();
        SpawnReward();

        // Set current effort level and presses per step
        // currentEffortLevel = experimentManager != null ? (int)experimentManager.GetCurrentTrialEV() : 1;
        // int pressesRequired = GetPressesRequired(currentEffortLevel);
        // SetPressesPerStep(currentEffortLevel);

        // Update LogManager with the current trial info
        // LogManager.Instance.UpdateTrialInfo(currentTrialIndex, currentEffortLevel, pressesRequired);
        // Debug.Log($"Current effort level set to: {currentEffortLevel}, Presses required: {pressesRequired}");

        // Start the countdown timer
        if (countdownTimer != null)
        {
            countdownTimer.StartTimer(maxTrialDuration);
            countdownTimer.OnTimerExpired += OnTimerExpired;
            StartCoroutine(CheckTimerAccuracyRoutine());
            Debug.Log($"Countdown timer started. Duration: {maxTrialDuration}");
        }
        else
        {
            Debug.LogError("CountdownTimer is not assigned!");
        }

        // Enable player movement
        EnablePlayerMovement();

        // Log the trial start
        LogTrialStart();
        Debug.Log("Trial fully initialized and started.");

        // Get the current trial index and block number from ExperimentManager
        int currentTrialIndex = ExperimentManager.Instance.GetCurrentTrialIndex();
        int currentBlockNumber = ExperimentManager.Instance.GetCurrentBlockNumber();

        // Log the start of the Grid World phase
        // LogManager.instance.LogGridWorldPhaseStart(currentTrialIndex);
        // Get the current effort level and presses required
        int currentEffortLevel = ExperimentManager.Instance.GetCurrentTrialEV();
        int pressesRequired = ExperimentManager.Instance.GetCurrentTrialEV(); // This should return the presses required

        Debug.Log($"Current effort level set to: {currentEffortLevel}");
        Debug.Log($"Starting Trial {currentTrialIndex} in Block {currentBlockNumber}. Effort Level: {currentEffortLevel}, Presses Required: {pressesRequired}");


        // LogManager.instance.LogTrialInfo(currentTrialIndex, currentBlockNumber, currentEffortLevel,
        //                          GetPressesRequired(currentEffortLevel), "Start", decisionReactionTime);

        // Add this at the end of the method
        gridWorldManager.InitializeGridWorld(maxTrialDuration);
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

        // Notify ExperimentManager that the trial has ended
        // ExperimentManager.Instance.EndTrial(rewardCollected);
        // Replace the direct call to ExperimentManager with:
        gridWorldManager.EndTrial(rewardCollected);

        // Transition to DecisionPhase scene
        // StartCoroutine(TransitionToDecisionPhase());

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
    private void CleanupTrial(bool rewardCollected)
    {
        Debug.Log("CleanupTrial method called.");
        FreezePlayer();
        HideRewardIfCollected(rewardCollected);
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
    public void UpdateBlockAndTrialInfo(int blockNumber, int trialIndex, int effortLevel)
    {
        currentBlockNumber = blockNumber;
        currentTrialIndex = trialIndex;
        currentEffortLevel = effortLevel;
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
            //         LogManager.instance.LogEvent("SpawnPositions", new Dictionary<string, object>
            // {
            //     {"TrialNumber", currentTrialIndex + 1},
            //     {"BlockNumber", currentBlockNumber},
            //     {"PlayerPosition", playerPosition.ToString()},
            // });

            if (currentPlayer != null)
            {
                Debug.Log($"Player spawned at position: {playerPosition}");
                playerInitialPosition = playerPosition;
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
        // Vector2 rewardPosition = rewardSpawner.GetRandomSpawnPosition();
        int pressesRequired = GetPressesRequired(currentEffortLevel);
        float rewardValue = experimentManager.GetCurrentTrialRewardValue();
        int currentBlockNumber = experimentManager.GetCurrentBlockNumber();
        int currentTrialIndex = experimentManager.GetCurrentTrialIndex();

        // currentReward = rewardSpawner.SpawnReward(rewardPosition, currentBlockNumber, currentTrialIndex, pressesRequired, Mathf.RoundToInt(rewardValue));
        // currentReward = rewardSpawner.SpawnReward(playerInitialPosition, currentBlockNumber, currentTrialIndex, pressesRequired, Mathf.RoundToInt(rewardValue));
        currentReward = rewardSpawner.SpawnReward(currentBlockNumber, currentTrialIndex, pressesRequired, Mathf.RoundToInt(rewardValue));
        // currentReward = rewardSpawner.SpawnReward(rewardPosition, currentBlockNumber, currentTrialIndex, pressesRequired, Mathf.RoundToInt(rewardValue));
        // LogManager.instance.LogEvent("SpawnPositions", new Dictionary<string, object>
        // {
        //     {"TrialNumber", currentTrialIndex + 1},
        //     {"BlockNumber", currentBlockNumber},
        //     {"RewardPosition", rewardPosition.ToString()}
        // });

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
    private void OnTimerExpired()
    {
        if (isTrialActive && !rewardCollected && !isTrialEnded)
        {
            Debug.Log("Time's up! Ending trial.");
            EndTrial(false);
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

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(Mathf.RoundToInt(rewardValue), isFormalTrial: true);
                // ScoreManager.Instance.AddScore(points, isFormalTrial: true); // For formal trials
                // ScoreManager.Instance.AddScore(points, isFormalTrial: false); // For practice trials

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

        ExperimentManager.Instance.logManager.LogCollisionTime(ExperimentManager.Instance.GetCurrentTrialIndex());
        Debug.Log("Calling EndTrial from RewardCollected");
        EndTrial(collision);
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
            Debug.LogError($"Invalid effort level: {effortLevel}");
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
            Debug.LogError("currentPlayer is null when trying to set presses per step!");
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
}