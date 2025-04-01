using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;


/// <summary>
/// Manages the overall flow of the experiment, including trial generation, scene transitions, and data logging.
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    #region Singleton
    public static ExperimentManager Instance { get; private set; }
    #endregion

    #region Constants
    public const float BLOCK_DURATION = 300.0f; // 5 minutes per block
    private const float MIN_TRIAL_DURATION = 0.0f; // Minimum time needed for a trial
    private const int TOTAL_BLOCKS = 4; // Total number of blocks
    private const int REWARD_VALUE = 10; // Value of the reward for each trial
    // private const int SKIP_REWARD_VALUE = 0; // Value awarded for skipping
    // private const int NO_DECISION_REWARD_VALUE = 0; // Value for no decision (explicit for clarity)
    private const float SKIP_DELAY = 3f; // Delay before showing the next trial after skipping
    private const float DECISION_TIMEOUT = 2.5f; // Time allowed for making a decision
    private const float NO_DECISION_PENALTY_DURATION = 5f; // Duration of no-decision penalty
    private const string PENALTY_SCENE = "TimePenalty";
    // private const float MIN_WAIT_TIME = 1f;
    // private const float MAX_WAIT_TIME = 5f;
    // private const string WAITING_ROOM_SCENE = "GetReadyITI";

    private string currentDecisionType = string.Empty;
    // Add this property to track the current trial's decision
    public string CurrentDecisionType
    {
        get { return currentDecisionType; }
        private set { currentDecisionType = value; }
    }

    // Add block type enum
    public enum BlockType
    {
        HighLowRatio,  // 3:2:1 ratio (more oranges)
        LowHighRatio   // 1:2:3 ratio (more cherries)
        // EqualRatio     // 1:1:1
    }
    public BlockType currentBlockType;
    public BlockType[] randomizedBlockOrder;
    private bool hasInitialized = false;
    private Dictionary<int, int> effortLevelCounts = new Dictionary<int, int>
    {
        { 1, 0 }, // Low effort
        { 2, 0 }, // Medium effort
        { 3, 0 }  // High effort
    };
    private Dictionary<int, int> blockRatios = new Dictionary<int, int>();
    private Dictionary<int, int> rushCountPerBlock = new Dictionary<int, int>();
    private int totalRatioParts = 0;
    private int[] trialCountsByEffortLevel = new int[4]; // Index 0 unused, 1-3 for effort levels
    private Dictionary<int, Dictionary<int, int>> blockEffortCounts = new Dictionary<int, Dictionary<int, int>>();
    #endregion


    #region Serialized Fields
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level2Sprite;
    [SerializeField] private Sprite level3Sprite;
    [SerializeField] private Sprite currentTrialSprite;
    [SerializeField] private string decisionPhaseScene = "DecisionPhase";
    [SerializeField] private string gridWorldScene = "GridWorld";
    [SerializeField] private string restBreakScene = "RestBreak";
    #endregion

    #region Scene Flow Configuration
    [System.Serializable]
    public class SceneConfig
    {
        public string sceneName;
        public float minimumDisplayTime = 0f;
        public bool requiresButtonPress = false;
        public string nextScene;
        public bool isOptional = false;
    }

    [SerializeField] private List<SceneConfig> introductorySceneFlow = new List<SceneConfig>();
    private Queue<SceneConfig> sceneQueue = new Queue<SceneConfig>();
    private float currentSceneStartTime;
    private SceneConfig currentSceneConfig;
    private bool hasInitializedFlow = false;
    #endregion

    #region Audio Configuration
    [SerializeField] private bool playBackgroundMusic = true;
    [SerializeField] private float musicVolume = 1f;
    #endregion

    #region Private Fields
    private List<Trial> trials;
    private Dictionary<Sprite, int> spriteToEffortMap;
    private int globalTrialIndex = 0; // Tracks trials across all blocks
    private int currentTrialIndex = 0;
    private int currentBlockNumber = 0;
    private bool experimentStarted = false;
    private bool isTourCompleted = false;
    private bool isPractice = false;
    private float trialStartTime;
    public GridManager gridManager;
    public ScoreManager scoreManager;
    public LogManager logManager;
    private bool decisionMade = false;
    private float decisionStartTime;
    private Coroutine decisionTimeoutCoroutine;
    private bool isExperimentActive = true;
    // private bool hasShownBlockInstructions = false;

    #endregion

    #region Tracking Fields
    private float blockStartTime;
    // private float currentBlockRemainingTime;
    private bool isBlockActive = false;
    private bool isBlockTimeUp = false;
    public int trialsCompletedInCurrentBlock;
    #endregion


    #region Events
    public event System.Action<bool> OnTrialEnded;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Hide cursor and lock it to center of screen
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Hide Caps Lock indicator
        // Input.ForceInitialize(); // Ensure Input system is initialized
        // Input.simulateMouseWithTouches = false; // Disable touch simulation

        // Existing Awake code
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureLogManagerExists();
            InitializeComponents();
            InitializeSceneQueue();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool trialStarted = false;  // Add this field to track if trial is started
    private void Update()
    {
        // WebGL-specific timing debug (commented out by default)
#if UNITY_WEBGL && DEBUG_TIMING
    if (Time.frameCount % 60 == 0) // Log once per second at 60fps
    {
        Debug.Log($"WebGL Timing - Frame: {Time.frameCount}, " +
                 $"Unscaled: {Time.unscaledTime:F2}, " +
                 $"Delta: {Time.unscaledDeltaTime:F4}");
    }
#endif

        if (isBlockActive)
        {
            float rawElapsed = Time.unscaledTime - blockStartTime;

            // Safety check for time anomalies
            if (rawElapsed < 0)
            {
                Debug.LogError($"TIME ANOMALY DETECTED! Block {currentBlockNumber} " +
                             $"elapsed={rawElapsed} start={blockStartTime} now={Time.unscaledTime}");
                blockStartTime = Time.unscaledTime; // Emergency reset
                rawElapsed = 0;
            }

            if (!isBlockTimeUp)
            {
                // Calculate remaining time with WebGL buffer
                float remainingTime = BLOCK_DURATION - rawElapsed;
#if UNITY_WEBGL
                remainingTime -= WEBGL_TIME_BUFFER;
#endif

                // Log block status periodically
                if (Time.unscaledTime % 10 < Time.unscaledDeltaTime)
                {
                    logManager.LogBlockTimeStatus(
                        currentBlockNumber,
                        trialsCompletedInCurrentBlock
                    );
                }

                // Check if block should end
                if (remainingTime <= MIN_TRIAL_DURATION)
                {
                    Debug.Log($"Block {currentBlockNumber + 1} time expired. " +
                            $"RawRemaining: {BLOCK_DURATION - rawElapsed:F1}s " +
                            $"AfterBuffer: {remainingTime:F1}s");
                    isBlockTimeUp = true;
                    EndCurrentBlock();
                    return;
                }

                // Start new trial if conditions are met
                if (!trialStarted && !decisionMade &&
                    SceneManager.GetActiveScene().name == decisionPhaseScene)
                {
                    StartTrial();
                }
            }
        }

        // Additional debug checks (commented out by default)
#if FALSE
    if (isBlockActive && Time.unscaledTime - lastDebugLog > 5f)
    {
        lastDebugLog = Time.unscaledTime;
        Debug.Log($"Block {currentBlockNumber} Status: " +
                $"Active={isBlockActive} " +
                $"TimeUp={isBlockTimeUp} " +
                $"Elapsed={Time.unscaledTime - blockStartTime:F1}s " +
                $"Remaining={GetBlockTimeRemaining():F1}s");
    }
#endif
    }


#if UNITY_WEBGL
    // Add small buffer to account for WebGL timing inconsistencies
    private const float WEBGL_TIME_BUFFER = 0.5f;
#else
    private const float WEBGL_TIME_BUFFER = 0f;
#endif


    private float blockTimer = 0f;

    private void UpdateBlockTimer()
    {
        if (isBlockActive && !isBlockTimeUp)
        {
            blockTimer += Time.unscaledDeltaTime;

            if (blockTimer >= BLOCK_DURATION)
            {
                isBlockTimeUp = true;
                EndCurrentBlock();
            }
        }
    }

    public float GetBlockTimeRemaining()
    {
        if (!isBlockActive) return 0f;
        float remaining = BLOCK_DURATION - (Time.unscaledTime - blockStartTime);
        return Mathf.Max(0f, remaining - WEBGL_TIME_BUFFER);
    }


    public (float remaining, float elapsed) GetBlockTime()
    {
        if (!isBlockActive) return (0, 0);

        // float elapsed = Time.time - blockStartTime;
        float elapsed = Time.unscaledTime - blockStartTime;
        float remaining = Mathf.Max(0, BLOCK_DURATION - elapsed);
        return (remaining, elapsed);
    }


    // private IEnumerator WebGLSafeTimeout(float duration, System.Action callback)
    // {
    //     float endTime = Time.unscaledTime + duration;

    //     while (Time.unscaledTime < endTime)
    //     {
    //         yield return null;
    //     }

    //     callback?.Invoke();
    // }

    private IEnumerator WebGLSafeTimeout(float duration, System.Action callback)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
    float endTime = Time.unscaledTime + duration + 0.5f; // Add buffer
    while (Time.unscaledTime < endTime)
    {
        yield return null;
    }
#else
        yield return new WaitForSeconds(duration);
#endif

        callback?.Invoke();
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeBackgroundMusic();

        // Ensure background music persists
        if (BackgroundMusicManager.Instance != null)
        {
            DontDestroyOnLoad(BackgroundMusicManager.Instance.gameObject);
            BackgroundMusicManager.Instance.PlayMusic();
        }
        else
        {
            Debug.LogWarning("BackgroundMusicManager instance not found!");
        }

        // Check if tour is already completed
        isTourCompleted = PlayerPrefs.GetInt("TourCompleted", 0) == 1;

        // Set experiment start time
        PlayerPrefs.SetFloat("ExperimentStartTime", Time.time);

        if (logManager == null)
        {
            Debug.LogError("LogManager is still null in Start method!");
        }
        else
        {
            // LogManager.instance.LogEvent("ExperimentStart");
            // logManager.LogExperimentStart(true);
        }
        VerifyPlayerPrefs();

        if (!hasInitializedFlow)
        {
            InitializeSceneQueue();
            StartSceneFlow();
            hasInitializedFlow = true;
        }
    }

    private void StartTrial()
    {
        // Only ensure trials if we don't have enough
        if (currentTrialIndex >= trials.Count)
        {
            EnsureEnoughTrials();
        }

        // Increment the global trial index
        globalTrialIndex++;

        // Validate trial belongs to current block
        if (trials[currentTrialIndex].BlockIndex != currentBlockNumber)
        {
            Debug.LogError($"Trial {currentTrialIndex} doesn't belong to Block {currentBlockNumber}");
            return;
        }

        if (!isBlockActive || isBlockTimeUp)
        {
            Debug.LogError($"Trial start prevented! Block Active: {isBlockActive}, Time Up: {isBlockTimeUp}");
            return;
        }

        trialStarted = true;  // Set flag indicating trial has started
        decisionMade = false;

        // Reset the decision type at the start of each trial
        CurrentDecisionType = string.Empty;

        // Start the decision phase
        decisionStartTime = Time.time;
        trialStartTime = Time.time;
        decisionTimeoutCoroutine = StartCoroutine(DecisionTimeout());

        Debug.Log($"Starting trial {globalTrialIndex} (Block {currentBlockNumber + 1}, Local Trial {currentTrialIndex + 1}) at time: {Time.time}");

        // Ensure there are enough trials for the next trial
        // EnsureEnoughTrials();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion

    #region Initialization Methods
    private void InitializeComponents()
    {
        // Initialize the trials list
        trials = new List<Trial>();

        // Initialize the effort level counts dictionary
        effortLevelCounts = new Dictionary<int, int>
        {
            { 1, 0 }, // Low effort
            { 2, 0 }, // Medium effort
            { 3, 0 }  // High effort
        };

        // Initialize block order and other components
        InitializeBlockOrder();
        InitializeSpriteToEffortMap();
        InitializeTrials();

        // Log effort levels for debugging
        LogEffortLevels();
    }

    // // For TOTAL randomization
    // private void InitializeBlockOrder()
    // {
    //     // Only initialize if not already done
    //     if (hasInitialized) return;

    //     randomizedBlockOrder = new BlockType[TOTAL_BLOCKS];
    //     randomizedBlockOrder[0] = BlockType.HighLowRatio;
    //     randomizedBlockOrder[1] = BlockType.LowHighRatio;
    //     // randomizedBlockOrder[2] = BlockType.EqualRatio;
    //     ShuffleBlockOrder();

    //     hasInitialized = true;

    //     // Log the initialized order
    //     Debug.Log($"Block order initialized: Block 1: {randomizedBlockOrder[0]}, Block 2: {randomizedBlockOrder[1]}");
    // }

    // For two FIXED Block order
    private void InitializeBlockOrder()
    {
        // Only initialize if not already done
        if (hasInitialized) return;

        randomizedBlockOrder = new BlockType[TOTAL_BLOCKS];

        // Randomly choose between the two fixed patterns
        bool usePattern1 = Random.Range(0, 2) == 0;

        if (usePattern1)
        {
            // Pattern 1: HighLow, LowHigh, HighLow, LowHigh
            randomizedBlockOrder[0] = BlockType.HighLowRatio;
            randomizedBlockOrder[1] = BlockType.LowHighRatio;
            randomizedBlockOrder[2] = BlockType.HighLowRatio;
            randomizedBlockOrder[3] = BlockType.LowHighRatio;
            Debug.Log("Using block pattern: HighLow, LowHigh, HighLow, LowHigh");
        }
        else
        {
            // Pattern 2: LowHigh, HighLow, LowHigh, HighLow
            randomizedBlockOrder[0] = BlockType.LowHighRatio;
            randomizedBlockOrder[1] = BlockType.HighLowRatio;
            randomizedBlockOrder[2] = BlockType.LowHighRatio;
            randomizedBlockOrder[3] = BlockType.HighLowRatio;
            Debug.Log("Using block pattern: LowHigh, HighLow, LowHigh, HighLow");
        }

        hasInitialized = true;
    }

    private void EnsureLogManagerExists()
    {
        // First try to find existing LogManager
        logManager = FindAnyObjectByType<LogManager>();

        // If not found, create one
        if (logManager == null)
        {
            GameObject logManagerObj = new GameObject("LogManager");
            logManager = logManagerObj.AddComponent<LogManager>();
            // Ensure it persists between scenes
            DontDestroyOnLoad(logManagerObj);
        }

        // Wait a frame to ensure LogManager's Awake has completed
        StartCoroutine(WaitForLogManagerInitialization());
    }

    private IEnumerator WaitForLogManagerInitialization()
    {
        yield return null; // Wait one frame

        // Now check if LogManager is properly initialized
        if (logManager != null && !string.IsNullOrEmpty(logManager.LogFilePath))
        {
            Debug.Log("LogManager successfully initialized");
        }
        else
        {
            Debug.LogError("LogManager initialization failed");
        }
    }

    private void InitializeSceneQueue()
    {
        sceneQueue.Clear();
        foreach (var sceneConfig in introductorySceneFlow)
        {
            sceneQueue.Enqueue(sceneConfig);
        }
        Debug.Log($"Initialized scene queue with {sceneQueue.Count} scenes");
    }


    private void InitializeBackgroundMusic()
    {
        if (!playBackgroundMusic) return;

        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.SetVolume(musicVolume);
            BackgroundMusicManager.Instance.PlayMusic();
        }
        else
        {
            Debug.LogWarning("BackgroundMusicManager instance not found! Please ensure it exists in the scene.");
        }
    }

    private bool ValidateTrialAccess()
    {
        if (trials == null)
        {
            Debug.LogError("Trials list is null! Attempting to regenerate trials...");
            InitializeTrials();
            return false;
        }

        if (trials.Count == 0)
        {
            Debug.LogError("Trials list is empty! Attempting to regenerate trials...");
            InitializeTrials();
            return false;
        }

        if (currentTrialIndex < 0 || currentTrialIndex >= trials.Count)
        {
            Debug.LogWarning($"Trial index {currentTrialIndex} out of range. Current trial count: {trials.Count}. Ensuring enough trials...");
            EnsureEnoughTrials();
            return currentTrialIndex >= 0 && currentTrialIndex < trials.Count;
        }

        return true;
    }

    /// <summary>
    /// Initializes all trials for the experiment & Block Randomization
    /// </summary>
    private void InitializeTrials()
    {
        trials = new List<Trial>();

        if (gridManager == null)
        {
            Debug.LogError("GridManager not found!");
            return;
        }

        gridManager.EnsureInitialization();
        gridManager.ResetAvailablePositions();


        Debug.Log($"Total trials generated: {trials.Count}");
        LogTrialDistribution();
    }

    private void ResetBlockRatios()
    {
        if (currentBlockNumber >= randomizedBlockOrder.Length)
        {
            Debug.LogError($"Invalid block number: {currentBlockNumber}, max: {randomizedBlockOrder.Length - 1}");
            return;
        }

        BlockType blockType = randomizedBlockOrder[currentBlockNumber];

        // Make sure dictionary is created
        if (blockRatios == null)
            blockRatios = new Dictionary<int, int>();
        else
            blockRatios.Clear();

        // Reset trial counts array
        trialCountsByEffortLevel = new int[4]; // Reset counts

        // Set ratios based on block type
        switch (blockType)
        {
            case BlockType.HighLowRatio:
                blockRatios[1] = 3; // Low effort
                blockRatios[2] = 2; // Medium effort
                blockRatios[3] = 1; // High effort
                break;
            case BlockType.LowHighRatio:
                blockRatios[1] = 1; // Low effort
                blockRatios[2] = 2; // Medium effort
                blockRatios[3] = 3; // High effort
                break;
            // case BlockType.EqualRatio:
            //     blockRatios[1] = 1; // Low effort
            //     blockRatios[2] = 1; // Medium effort
            //     blockRatios[3] = 1; // High effort
            //     break;
            default:
                Debug.LogError($"Unknown block type: {blockType}");
                blockRatios[1] = 1;
                blockRatios[2] = 1;
                blockRatios[3] = 1;
                break;
        }

        totalRatioParts = 0;
        foreach (var kvp in blockRatios)
        {
            totalRatioParts += kvp.Value;
        }

        Debug.Log($"Reset block ratios for Block {currentBlockNumber + 1} ({blockType}): " +
                  $"Low={blockRatios[1]}, Medium={blockRatios[2]}, High={blockRatios[3]}, Total parts={totalRatioParts}");
    }

    private int GetNextEffortLevelByRatio()
    {
        // Check if blockRatios has any entries
        if (blockRatios == null || blockRatios.Count == 0)
        {
            Debug.LogError("Block ratios not initialized! Initializing now...");
            ResetBlockRatios();
            return 1; // Default to low effort if all else fails
        }

        // Calculate the target distribution based on the ratio
        Dictionary<int, float> targetDistribution = new Dictionary<int, float>();
        Dictionary<int, float> currentDistribution = new Dictionary<int, float>();

        int totalTrials = trialCountsByEffortLevel.Sum();

        // If no trials yet, start with most common effort level
        if (totalTrials == 0)
        {
            // Find effort level with highest ratio value
            int highestRatioLevel = 1;
            int highestRatio = 0;

            foreach (var kvp in blockRatios)
            {
                if (kvp.Value > highestRatio)
                {
                    highestRatio = kvp.Value;
                    highestRatioLevel = kvp.Key;
                }
            }

            return highestRatioLevel;
        }

        // Calculate target and current distributions
        foreach (var kvp in blockRatios)
        {
            targetDistribution[kvp.Key] = (float)kvp.Value / totalRatioParts;
            currentDistribution[kvp.Key] = (float)trialCountsByEffortLevel[kvp.Key] / totalTrials;
        }

        // Find the effort level that's most below its target ratio
        int selectedLevel = 1; // Default to level 1
        float maxDiscrepancy = -1f;

        foreach (int level in blockRatios.Keys)
        {
            float discrepancy = targetDistribution[level] - currentDistribution[level];
            if (discrepancy > maxDiscrepancy)
            {
                maxDiscrepancy = discrepancy;
                selectedLevel = level;
            }
        }

        return selectedLevel;
    }

    private void LogTrialDistribution()
    {
        if (trials == null)
        {
            Debug.LogError("Cannot log trial distribution - trials list is null");
            return;
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine("Initial Trial Distribution:");

        for (int blockIndex = 0; blockIndex < TOTAL_BLOCKS; blockIndex++)
        {
            logBuilder.AppendLine($"\nBlock {blockIndex + 1} ({randomizedBlockOrder[blockIndex]}):");

            var blockTrials = trials.Where(t => t.BlockIndex == blockIndex).ToList();
            var effortCounts = new Dictionary<int, int>
            {
                { 1, 0 }, // Low effort
                { 2, 0 }, // Medium effort
                { 3, 0 }  // High effort
            };

            foreach (var trial in blockTrials)
            {
                if (trial?.EffortSprite != null && spriteToEffortMap.ContainsKey(trial.EffortSprite))
                {
                    int effortLevel = spriteToEffortMap[trial.EffortSprite];
                    effortCounts[effortLevel]++;
                }
            }

            for (int i = 1; i <= 3; i++)
            {
                float percentage = blockTrials.Count > 0 ?
                    (float)effortCounts[i] / blockTrials.Count * 100 : 0;
                logBuilder.AppendLine($"Level {i}: {effortCounts[i]} trials ({percentage:F1}%)");
            }
        }

        Debug.Log(logBuilder.ToString());
    }

    private bool ensuringTrials = false;
    private void EnsureEnoughTrials()
    {
        Debug.Log($"EnsureEnoughTrials called from: {new System.Diagnostics.StackTrace().ToString()}");
        // Prevent recursive or multiple calls
        if (ensuringTrials)
        {
            Debug.Log("Already ensuring trials, skipping duplicate call");
            return;
        }

        ensuringTrials = true;

        if (gridManager == null)
        {
            Debug.LogError("GridManager is null in EnsureEnoughTrials!");
            return;
        }

        // Make sure we have valid block ratios
        if (blockRatios == null || blockRatios.Count == 0)
        {
            Debug.LogWarning("Block ratios not set, initializing now...");
            ResetBlockRatios();
        }

        try
        {
            // Determine the next effort level based on the ratio
            int effortLevel = GetNextEffortLevelByRatio();
            Debug.Log($"Selected effort level for next trial: {effortLevel}");

            // Generate a trial with the selected effort level
            Vector2 playerPos = gridManager.GetRandomAvailablePosition();
            Vector2 rewardPos = gridManager.GetPositionAtDistance(playerPos, 5);
            Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

            if (playerPos == Vector2.zero || rewardPos == Vector2.zero)
            {
                Debug.LogError("Failed to get valid grid positions!");
                return;
            }

            // Add the trial to the trials list
            trials.Add(new Trial(
                effortSprite,
                playerPos,
                rewardPos,
                currentBlockNumber,  // Actual block index
                currentBlockNumber   // Original order index
            ));

            // Track the effort level for statistics
            trialCountsByEffortLevel[effortLevel]++;

            // Log the current distribution
            LogCurrentEffortDistribution();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in EnsureEnoughTrials: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            ensuringTrials = false;
        }
    }


    private void LogCurrentEffortDistribution()
    {
        // Only count trials from the current block
        var currentBlockTrials = trials.Where(t => t.BlockIndex == currentBlockNumber).ToList();
        int[] currentBlockCounts = new int[4]; // Index 0 is unused

        foreach (var trial in currentBlockTrials)
        {
            if (trial?.EffortSprite != null && spriteToEffortMap.ContainsKey(trial.EffortSprite))
            {
                int effortLevel = spriteToEffortMap[trial.EffortSprite];
                currentBlockCounts[effortLevel]++;
            }
        }

        int totalTrials = currentBlockCounts.Sum();
        if (totalTrials == 0) return;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Current Block {currentBlockNumber + 1} distribution:");

        for (int i = 1; i <= 3; i++)
        {
            float percentage = (float)currentBlockCounts[i] / totalTrials * 100;
            sb.AppendLine($"Level {i}: {currentBlockCounts[i]} trials ({percentage:F1}%)");
        }

        // Calculate how close we are to the target ratio
        sb.AppendLine("Target vs Actual:");
        for (int i = 1; i <= 3; i++)
        {
            float targetPct = (float)blockRatios[i] / totalRatioParts * 100;
            float actualPct = (float)currentBlockCounts[i] / totalTrials * 100;
            sb.AppendLine($"Level {i}: Target {targetPct:F1}% vs Actual {actualPct:F1}%");
        }

        Debug.Log(sb.ToString());
    }

    private void VerifyPlayerPrefs()
    {
        Debug.Log("Verifying PlayerPrefs for PressesPerEffortLevel:");
        for (int i = 1; i <= 3; i++)
        {
            int presses = PlayerPrefs.GetInt($"PressesPerEffortLevel_{i}", -1);
            Debug.Log($"Effort Level {i}: {presses} presses");
        }
    }
    #endregion


    #region Scene Management Methods
    private void StartSceneFlow()
    {
        if (sceneQueue.Count > 0)
        {
            LoadNextScene();
        }
        else
        {
            // Check if tour is completed
            if (isTourCompleted)
            {
                StartPracticeSequence();
            }
        }
    }

    /// <summary>
    /// Loads a new scene by name.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
        CleanupPlayer();
    }

    private IEnumerator AutoAdvanceScene()
    {
        Debug.Log($"Waiting {currentSceneConfig.minimumDisplayTime} seconds before auto-advancing");
        yield return new WaitForSeconds(currentSceneConfig.minimumDisplayTime);
        LoadNextScene();
    }


    public void DebugSceneQueue()
    {
        Debug.Log($"Current scene queue status:");
        Debug.Log($"Current scene: {(currentSceneConfig != null ? currentSceneConfig.sceneName : "none")}");
        Debug.Log($"Remaining scenes in queue: {sceneQueue.Count}");
        foreach (var scene in sceneQueue)
        {
            Debug.Log($"- {scene.sceneName} (Requires button: {scene.requiresButtonPress})");
        }
    }

    private void LoadNextScene()
    {
        if (sceneQueue.Count > 0)
        {
            currentSceneConfig = sceneQueue.Dequeue();
            currentSceneStartTime = Time.time;
            Debug.Log($"Loading next scene: {currentSceneConfig.sceneName}");
            SceneManager.LoadScene(currentSceneConfig.sceneName);
        }
        else if (!experimentStarted)
        {
            StartExperiment();
        }
    }

    /// <summary>
    /// Cleans up the player object when transitioning between scenes.
    /// </summary>
    private void CleanupPlayer()
    {
        if (PlayerController.Instance != null)
        {
            Destroy(PlayerController.Instance.gameObject);
        }
    }

    /// <summary>
    /// Called when a new scene is loaded.
    /// </summary>    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentSceneConfig != null && scene.name == currentSceneConfig.sceneName)
        {
            SetupSceneButtons();

            if (!currentSceneConfig.requiresButtonPress)
            {
                StartCoroutine(AutoAdvanceScene());
            }
        }

        if (isBlockActive)
        {
            Debug.Log($"Scene load during block! Adding offset...");
            blockStartTime += Time.deltaTime;
        }
    }

    private void SetupSceneButtons()
    {
        if (!currentSceneConfig.requiresButtonPress) return;

        var buttons = FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);
        foreach (var button in buttons)
        {
            button.onClick.RemoveAllListeners(); // Clear existing listeners
            button.onClick.AddListener(() => HandleButtonClick(button.gameObject.name));
            Debug.Log($"Setup button: {button.gameObject.name} in scene: {currentSceneConfig.sceneName}");
        }
    }

    private void HandleButtonClick(string buttonName)
    {
        // Debug.Log($"Button clicked: {buttonName} in scene: {currentSceneConfig.sceneName}");

        // Check if minimum display time has elapsed
        if (Time.time - currentSceneStartTime < currentSceneConfig.minimumDisplayTime)
        {
            Debug.Log("Minimum display time not yet elapsed");
            return;
        }
        if (currentSceneConfig.nextScene != null)
        {
            LoadNextScene();
        }
    }
    #endregion


    #region Experiment Control Methods
    private Sprite GetSpriteForEffortLevel(int effortLevel)
    {
        switch (effortLevel)
        {
            case 1: return level1Sprite;
            case 2: return level2Sprite;
            case 3: return level3Sprite;
            default:
                Debug.LogError($"Invalid effort level: {effortLevel}");
                return level1Sprite; // Return default sprite
        }
    }

    /// <summary>
    /// Initializes the mapping between sprites and effort levels.
    /// </summary>
    private void InitializeSpriteToEffortMap()
    {
        if (level1Sprite == null || level2Sprite == null || level3Sprite == null)
        {
            Debug.LogError("One or more effort level sprites are not assigned!");
            return;
        }

        spriteToEffortMap = new Dictionary<Sprite, int>
        {
            { level1Sprite, 1 }, // Low effort
            { level2Sprite, 2 }, // Medium effort
            { level3Sprite, 3 }  // High effort
        };

        Debug.Log($"Initialized spriteToEffortMap: Low={spriteToEffortMap[level1Sprite]}, Medium={spriteToEffortMap[level2Sprite]}, High={spriteToEffortMap[level3Sprite]}");
    }

    // Add method to track effort levels
    private void TrackEffortLevel(int blockIndex, int effortLevel)
    {
        if (blockEffortCounts.ContainsKey(blockIndex) &&
            blockEffortCounts[blockIndex].ContainsKey(effortLevel))
        {
            blockEffortCounts[blockIndex][effortLevel]++;
        }
    }


    /// <summary>
    /// Starts the experiment by transitioning to the DecisionPhase scene.
    /// </summary>
    public void StartExperiment()
    {
        if (!experimentStarted)
        {
            Debug.Log("Starting experiment");
            experimentStarted = true;
            currentTrialIndex = 0;
            currentBlockNumber = 0;
            // blockStartTime = Time.time;
            // isBlockActive = true;  // Ensure block is active at experiment start
            isBlockActive = false; // Don't activate block until after instructions
            ScoreManager.Instance.ResetScore();

            // logManager.LogExperimentStart(false);

            // Start first block before showing instructions
            StartNewBlock();
            ShowBlockInstructions();
        }
    }

    private void StartPracticeSequence()
    {
        Debug.Log("Starting practice sequence");
        isPractice = true;
        // practiceTrialIndex = 0;

        // Load GetReadyPractise scene
        LoadScene("GetReadyPractise");
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    public void HandleDecision(bool workDecision, float decisionTime)
    {
        if (decisionMade) return;
        decisionMade = true;

        string decisionType = workDecision ? "Work" : "Skip";
        CurrentDecisionType = decisionType;

        if (workDecision)
        {
            logManager.LogWorkDecision(currentTrialIndex, decisionTime);
            LoadScene(gridWorldScene);
        }
        else
        {
            logManager.LogSkipDecision(currentTrialIndex, decisionTime);
            StartCoroutine(HandleSkipPenalty(decisionTime));
        }
    }

    public void StopDecisionTimeout()
    {
        if (decisionTimeoutCoroutine != null)
        {
            StopCoroutine(decisionTimeoutCoroutine);
            decisionTimeoutCoroutine = null;
            Debug.Log("DecisionTimeout coroutine stopped.");
        }
        decisionMade = true; // Ensure this is set
    }

    private IEnumerator HandleSkipPenalty(float decisionTime)
    {
        Debug.Log("HandleSkipPenalty started");
        float penaltyStartTime = Time.time;

        // logManager.LogPenaltyStart(currentTrialIndex, "Skip", SKIP_DELAY);
        // logManager.LogSkipDecision(currentTrialIndex, "Skip", Time.time - decisionStartTime);
        // logManager.LogSkipDecision(currentTrialIndex, decisionTime);

        yield return new WaitForSeconds(SKIP_DELAY);

        // logManager.LogPenaltyEnd(currentTrialIndex, "Skip");
        float totalDuration = Time.time - penaltyStartTime;

        // Log trial outcome for skip
        // int effortLevel = GetCurrentTrialEffortLevel();
        // logManager.LogTrialOutcome(currentTrialIndex, currentBlockNumber);

        // Process skip as a completed trial
        ProcessTrialCompletion(false, totalDuration);
        Debug.Log("HandleSkipPenalty completed");
    }

    public bool DecisionMade { get; private set; }

    public void SetDecisionMade(bool value)
    {
        DecisionMade = value;
    }


    private bool isNoDecisionPenaltyRunning = false; // Add this flag to track if the penalty is already running

    public void HandleNoDecision()
    {
        // Check if the penalty is already running
        if (isNoDecisionPenaltyRunning)
        {
            // Debug.LogWarning("No-decision penalty is already running. Skipping duplicate call.");
            return;
        }

        Debug.Log($"HandleNoDecision called for Trial {currentTrialIndex}");

        // Start the no-decision penalty coroutine
        isNoDecisionPenaltyRunning = true; // Set the flag to indicate the penalty is running
        StartCoroutine(HandleNoDecisionPenalty());
    }

    // Update HandleNoDecisionPenalty to handle time-based progression
    private IEnumerator HandleNoDecisionPenalty()
    {
        Debug.Log($"HandleNoDecisionPenalty started for Trial {currentTrialIndex}");

        float penaltyStartTime = Time.time;
        logManager.LogPenaltyStart(currentTrialIndex, "NoDecision", NO_DECISION_PENALTY_DURATION);

        // Explicitly log that no points are awarded
        Debug.Log("No points awarded for no decision");

        // Load the penalty scene
        SceneManager.LoadScene(PENALTY_SCENE);

        // Wait for scene load to complete
        yield return new WaitForSeconds(0.1f);

        // Wait for the full penalty duration
        yield return new WaitForSeconds(NO_DECISION_PENALTY_DURATION);

        float totalDuration = Time.time - penaltyStartTime;

        // Process no-decision as a completed trial
        ProcessTrialCompletion(false, totalDuration);

        // Reset the flag
        isNoDecisionPenaltyRunning = false;

        Debug.Log($"HandleNoDecisionPenalty completed for Trial {currentTrialIndex}");
    }

    public void ProcessTrialCompletion(bool rewardCollected, float trialDuration)
    {
        Debug.Log($"Processing Trial {currentTrialIndex} Completion");

        if (!isExperimentActive) return;

        // Calculate remaining time
        float remainingTime = BLOCK_DURATION - (Time.time - blockStartTime);
        trialsCompletedInCurrentBlock++;

        // Log remaining time
        float elapsed = Time.time - blockStartTime;

        // logManager.LogBlockTimeElapsed(
        //     currentTrialIndex,
        //     currentBlockNumber,
        //     BLOCK_DURATION - elapsed,  // remainingTime
        //     elapsed                    // new parameter
        // );

        // Log various metrics
        logManager.LogBlockTimeStatus(
            currentBlockNumber,
            trialsCompletedInCurrentBlock
        );


        // Check if we should end the current block
        if (remainingTime <= MIN_TRIAL_DURATION)
        {
            Debug.Log($"Block {currentBlockNumber + 1} ending due to time limit.");
            EndCurrentBlock();
        }
        else
        {
            // Move to next trial before loading decision phase
            MoveToNextTrial();
        }

        // Reset decision type for next trial
        CurrentDecisionType = string.Empty;
        trialStarted = false;
    }

    public void MoveToNextTrial()
    {
        if (!isExperimentActive || !HasTimeForNewTrial())
        {
            EndCurrentBlock();
            return;
        }

        // Reset decision state for new trial
        DecisionMade = false;

        // Increment trial index
        currentTrialIndex++;

        // Check if we need to generate more trials
        EnsureEnoughTrials();

        // If we've reached the end of the trials list, reset the index
        if (currentTrialIndex >= trials.Count)
        {
            currentTrialIndex = 0;
        }

        // Setup and load the next trial
        SetupNewTrial();
        LoadScene(decisionPhaseScene);
    }


    // // Add method to check block time
    // private bool IsBlockTimeExceeded()
    // {
    //     return Time.time - blockStartTime >= BLOCK_DURATION;
    // }

    // // Add new method for decision timeout
    // private IEnumerator DecisionTimeout()
    // {
    //     yield return new WaitForSeconds(DECISION_TIMEOUT);

    //     if (!decisionMade && gameObject != null && isActiveAndEnabled)
    //     {
    //         HandleNoDecision();
    //     }
    // }

    private IEnumerator DecisionTimeout()
    {
        Debug.Log($"Starting decision timeout at {Time.time} (WebGL: {Application.platform == RuntimePlatform.WebGLPlayer})");

        yield return StartCoroutine(WebGLSafeTimeout(DECISION_TIMEOUT, () =>
        {
            Debug.Log($"Timeout reached at {Time.time}. Decision made: {decisionMade}");

            if (!decisionMade && this != null && isActiveAndEnabled)
            {
                Debug.Log("Processing no-decision timeout");
                HandleNoDecision();
            }
            else
            {
                Debug.Log("Skipping no-decision - " +
                         (decisionMade ? "decision already made" :
                          (this == null ? "manager destroyed" : "manager inactive")));
            }
        }));

        Debug.Log("Decision timeout coroutine completed");
    }


    private void SetupNewTrial()
    {
        if (!isBlockActive || isBlockTimeUp)
        {
            Debug.LogError("Attempting to setup trial while block is not active or time is up");
            return;
        }

        float remainingTime = BLOCK_DURATION - (Time.time - blockStartTime);
        if (remainingTime <= 0)
        {
            Debug.Log("No time remaining for new trial");
            EndCurrentBlock();
            return;
        }

        Debug.Log($"Setting up trial {currentTrialIndex} in block {currentBlockNumber}");

        // Ensure there are enough trials
        EnsureEnoughTrials();

        // Validate the current trial index
        if (currentTrialIndex >= trials.Count)
        {
            Debug.LogError($"Invalid trial setup: trials={trials.Count}, currentTrialIndex={currentTrialIndex}");
            EndExperiment();
            return;
        }

        Trial currentTrial = trials[currentTrialIndex];
        if (currentTrial == null)
        {
            Debug.LogError("Current trial is null!");
            EndExperiment();
            return;
        }

        int effortLevel = GetCurrentTrialEffortLevel();
        TrackEffortLevel(currentBlockNumber, effortLevel);
        int pressesRequired = GetCurrentTrialEV();

        Debug.Log($"Trial {currentTrialIndex}: Effort Level={effortLevel}, Presses Required={pressesRequired}");
    }

    // Add helper method to check time constraints
    public bool HasTimeForNewTrial()
    {
        if (!isBlockActive || isBlockTimeUp) return false;

        float currentTime = Time.time;
        float remainingTime = BLOCK_DURATION - (currentTime - blockStartTime);

        Debug.Log($"Remaining block time: {remainingTime}, Minimum needed: {MIN_TRIAL_DURATION}");
        return remainingTime >= MIN_TRIAL_DURATION;
    }

    /// <summary>
    /// Continues the experiment after a break between blocks.
    /// </summary>
    public void ContinueAfterBreak()
    {
        Debug.Log("ContinueAfterBreak called.");

        // Increment the block number
        currentBlockNumber++;

        // Validate block index before proceeding
        if (currentBlockNumber >= TOTAL_BLOCKS)
        {
            Debug.Log("All blocks completed - direct to survey");
            LoadScene("TirednessRating");
            return;
        }

        // Update the current block type based on the fixed order
        currentBlockType = randomizedBlockOrder[currentBlockNumber];

        // Reset trial count tracking
        trialsCompletedInCurrentBlock = 0;
        trials.Clear();

        // Clear and reset the effort counts for the new block
        trialCountsByEffortLevel = new int[4];

        // Initialize the new block
        StartNewBlock();

        // Show instructions for the new block
        ShowBlockInstructions();
    }

    private void ShowBlockInstructions()
    {
        Debug.Log($"Showing instructions for block {currentBlockNumber + 1}, type: {currentBlockType}");
        LoadScene("Block_Instructions");
    }

    private void LogBlockStatistics()
    {
        int totalTrials = trialCountsByEffortLevel.Sum();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Block {currentBlockNumber + 1} ({randomizedBlockOrder[currentBlockNumber]}) completed statistics:");
        sb.AppendLine($"Total trials: {totalTrials}");

        for (int i = 1; i <= 3; i++)
        {
            float percentage = totalTrials > 0 ? (float)trialCountsByEffortLevel[i] / totalTrials * 100 : 0;
            sb.AppendLine($"Effort Level {i}: {trialCountsByEffortLevel[i]} trials ({percentage:F1}%)");
        }

        Debug.Log(sb.ToString());

        // Optional: Log to a file for analysis later
        System.IO.File.AppendAllText(
            "block_statistics.txt",
            $"{System.DateTime.Now}: {sb.ToString()}\n\n"
        );
    }


    public void ContinueAfterInstructions()
    {
        Debug.Log("ContinueAfterInstructions called.");

        // Ensure gridManager is initialized
        if (gridManager == null)
        {
            Debug.LogError("GridManager is null in ContinueAfterInstructions!");
            return;
        }

        // Ensure block ratios are initialized
        ResetBlockRatios();

        // Start the block timer only after clicking "Continue"
        isBlockActive = true;
        blockStartTime = Time.time;
        // currentBlockRemainingTime = BLOCK_DURATION;
        trialsCompletedInCurrentBlock = 0;

        // Clear any left-over trials from previous blocks
        trials.Clear();
        currentTrialIndex = 0;

        // Setup and load the next trial
        SetupNewTrial();
        LoadScene(decisionPhaseScene);

        Debug.Log($"Block {currentBlockNumber + 1} timer started at: {blockStartTime}");
    }

    /// <summary>
    /// Starts a new block of trials.
    /// </summary>
    private void StartNewBlock()
    {
        // Reset all decision-related states
        decisionMade = false;
        trialStarted = false;

        if (decisionTimeoutCoroutine != null)
        {
            StopCoroutine(decisionTimeoutCoroutine);
            decisionTimeoutCoroutine = null;
        }

        // blockStartTime = Time.time; // MUST reset timer
        blockStartTime = Time.unscaledTime;
        blockTimer = 0f;

#if UNITY_WEBGL
        Debug.Log("WebGL build - applying timing adjustments");
#endif

        // currentBlockRemainingTime = BLOCK_DURATION;

        // Debug verification
        Debug.Log($"Block {currentBlockNumber} started at: {blockStartTime} | " +
                  $"Current Time: {Time.time}");

        // Fresh initialization for each block
        gridManager.EnsureInitialization();
        gridManager.ResetAvailablePositions();

        // Reset the trial counts and ratios for the new block
        ResetBlockRatios();

        // Reset additional tracking arrays/dictionaries
        trials.Clear(); // Clear existing trials
        currentTrialIndex = 0;

        // Log the ratio for the current block
        BlockType blockType = randomizedBlockOrder[currentBlockNumber];
        Debug.Log($"Starting Block {currentBlockNumber + 1} ({blockType}) with ratio: " +
                  $"Low={blockRatios[1]}, Medium={blockRatios[2]}, High={blockRatios[3]}");

        // State flags
        isBlockActive = false; // Block timer is not active yet
        isBlockTimeUp = false;
        trialStarted = false;
        decisionMade = false;

        Debug.Log($"Block {currentBlockNumber + 1} ready. Timer will start after clicking 'Continue'.");
    }

    /// <summary>
    /// Ends the current block of trials.
    /// </summary>
    private void EndCurrentBlock()
    {
        int completedBlock = currentBlockNumber;
        Debug.Log($"Completed Block {completedBlock + 1}");

        // Log block statistics
        LogBlockStatistics();

        // Get rush count (0 if none)
        int rushes = rushCountPerBlock.ContainsKey(completedBlock) ?
                    rushCountPerBlock[completedBlock] : 0;

        // Call the 3-parameter version
        logManager.LogBlockEnd(completedBlock,
                             trialsCompletedInCurrentBlock,
                             rushes);


        if (completedBlock >= TOTAL_BLOCKS - 1)
        {
            // Final block completed - direct to survey
            LoadScene("TirednessRating");
        }
        else
        {
            // Proceed to rest break for next block
            LoadScene(restBreakScene);
        }
    }

    /// <summary>
    /// Ends the current trial and logs the result.
    /// </summary>
    // Update EndTrial to use the centralized progression logic
    public void EndTrial(bool rewardCollected, bool isPracticeTrial = false)
    {
        trialStarted = false;  // Reset flag when trial ends

        if (isPracticeTrial)
        {
            Debug.Log($"Practice Trial Ended - Reward Collected: {rewardCollected}");
            return;
        }

        if (logManager == null)
        {
            Debug.LogError("LogManager is null in EndTrial method!");
            return;
        }

        Debug.Log($"Ending trial {currentTrialIndex}. Reward collected: {rewardCollected}");

        float trialDuration = Time.time - trialStartTime;
        float actionReactionTime = GameController.Instance.GetActionReactionTime();

        OnTrialEnded?.Invoke(rewardCollected);
        ProcessTrialCompletion(rewardCollected, trialDuration);
    }


    /// <summary>
    /// Ends the experiment and transitions to the EndExperiment scene.
    /// </summary>
    // Update EndExperiment to ensure proper cleanup and transition
    // Update EndExperiment to ensure proper cleanup
    public void EndExperiment()
    {
        Debug.Log("Ending experiment");

        if (!isExperimentActive) return;
        isExperimentActive = false;

        StopAllCoroutines();

        if (logManager != null)
        {
            logManager.LogExperimentEnd();
            LogManager.Instance.DumpUnloggedTrials();
        }

        // if (BackgroundMusicManager.Instance != null)
        // {
        //     BackgroundMusicManager.Instance.StopMusic();
        // }

        float totalTime = Time.time - PlayerPrefs.GetFloat("ExperimentStartTime", 0f);
        Debug.Log($"Total experiment time: {totalTime} seconds");

        // Force load end scene with a small delay to ensure cleanup
        StartCoroutine(ForceLoadEndScene());
    }

    private IEnumerator ForceLoadEndScene()
    {
        yield return new WaitForSeconds(0.5f); // Short delay to ensure cleanup
        SceneManager.LoadScene("TirednessRating");
        // SceneManager.LoadScene(endExperimentScene);
    }
    #endregion

    #region Logging Methods
    private void LogEffortLevels()
    {
        // Check if trials list is initialized
        if (trials == null)
        {
            Debug.LogError("Cannot log effort levels - trials list is null!");
            return;
        }

        // Check if effortLevelCounts dictionary is initialized
        if (effortLevelCounts == null)
        {
            Debug.LogError("Cannot log effort levels - effortLevelCounts dictionary is null!");
            return;
        }

        // Check if spriteToEffortMap is initialized
        if (spriteToEffortMap == null)
        {
            Debug.LogError("Cannot log effort levels - spriteToEffortMap is null!");
            return;
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine("Initial Effort Levels Distribution:");

        // Iterate through each block
        for (int blockIndex = 0; blockIndex < TOTAL_BLOCKS; blockIndex++)
        {
            // Get the block type
            BlockType blockType = randomizedBlockOrder[blockIndex];
            logBuilder.AppendLine($"\nBlock {blockIndex + 1} ({blockType}):");

            // Filter trials for the current block
            var blockTrials = trials.Where(t => t.BlockIndex == blockIndex).ToList();

            // Initialize effort level counts for the current block
            var effortCounts = new Dictionary<int, int>
        {
            { 1, 0 }, // Low effort
            { 2, 0 }, // Medium effort
            { 3, 0 }  // High effort
        };

            // Count effort levels for the current block
            foreach (var trial in blockTrials)
            {
                if (trial?.EffortSprite != null && spriteToEffortMap.ContainsKey(trial.EffortSprite))
                {
                    int effortLevel = spriteToEffortMap[trial.EffortSprite];
                    if (effortCounts.ContainsKey(effortLevel))
                    {
                        effortCounts[effortLevel]++;
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid effort level: {effortLevel} in trial {trial}");
                    }
                }
            }

            // Log effort level counts for the current block
            for (int i = 1; i <= 3; i++)
            {
                float percentage = blockTrials.Count > 0 ?
                    (float)effortCounts[i] / blockTrials.Count * 100 : 0;
                logBuilder.AppendLine($"Level {i}: {effortCounts[i]} trials ({percentage:F1}%)");
            }
        }

        // Output the log
        Debug.Log(logBuilder.ToString());
    }




    #endregion

    #region Getter Methods
    /// <summary>
    /// Gets the effort value for the current trial.
    /// </summary>

    // effort level
    public int GetCurrentTrialEffortLevel()
    {
        if (trials == null || currentTrialIndex >= trials.Count)
        {
            // Debug.LogWarning($"Trials not initialized or currentTrialIndex ({currentTrialIndex}) out of range.");
            return 0; // Return 0 for invalid effort level
        }

        Sprite currentSprite = trials[currentTrialIndex].EffortSprite;
        if (spriteToEffortMap == null || !spriteToEffortMap.ContainsKey(currentSprite))
        {
            // Debug.LogWarning($"spriteToEffortMap not initialized or doesn't contain the current sprite.");
            return 0; // Return 0 for invalid effort level
        }

        int effortLevel = spriteToEffortMap[currentSprite];
        if (effortLevel < 1 || effortLevel > 3)
        {
            // Debug.LogWarning($"Invalid effort level: {effortLevel} for trial {currentTrialIndex}");
            return 0; // Return 0 for invalid effort level
        }

        return effortLevel;
    }

    public int GetCurrentTrialEV()
    {
        int effortLevel = GetCurrentTrialEffortLevel(); // Returns 1, 2, or 3

        // In ArrowKeyCounter.cs, the keys are stored as _1, _2, and _3 (1-based):
        // PlayerPrefs.SetInt($"PressesPerEffortLevel_{i + 1}", pressesPerEffortLevel[i]);

        // int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel - 1}", 0); // Subtract 1 to match the PlayerPrefs keys
        int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 0); // Subtract 1 to match the PlayerPrefs keys
        Debug.Log($"Current trial (index: {currentTrialIndex}) Effort Level: {effortLevel}, Presses Required: {pressesRequired}");

        return pressesRequired;
    }

    // public int GetTotalTrials() => TOTAL_TRIALS;
    public int GetTotalBlocks() => TOTAL_BLOCKS;
    public float GetBlockDuration() => BLOCK_DURATION;
    // public BlockType GetNextBlockType() =>
    //     currentBlockNumber <= TOTAL_BLOCKS ? randomizedBlockOrder[currentBlockNumber] : randomizedBlockOrder[0];


    public BlockType GetNextBlockType()
    {
        if (currentBlockNumber + 1 >= TOTAL_BLOCKS)
        {
            Debug.Log("All blocks completed - no next block type.");
            return BlockType.HighLowRatio; // Default return value
        }

        return randomizedBlockOrder[currentBlockNumber + 1];
    }

    public BlockType GetCurrentBlockType()
    {
        if (currentBlockNumber < 0 || currentBlockNumber >= randomizedBlockOrder.Length)
        {
            Debug.LogError($"Invalid block access: {currentBlockNumber} | Array size: {randomizedBlockOrder.Length}");
            return BlockType.HighLowRatio; // Safe default
        }
        return randomizedBlockOrder[currentBlockNumber];
    }

    public bool IsCurrentTrialPractice() => isPractice;
    // public int GetCurrentBlockNumber() => currentBlockNumber;

    // Update display logic in GetCurrentBlockNumber to use 1-based indexing for display
    // public int GetCurrentBlockNumber() => currentBlockNumber + 1; // Convert 0-based to 1-based indexing for display
    // public int GetCurrentBlockNumber() => currentBlockNumber;

    public int GetCurrentBlockNumber()
    {
        // For practice trials, always return block 0
        if (IsCurrentTrialPractice())
        {
            Debug.Log("Practice trial - Block number: 0");
            return 0;
        }

        // For formal trials, return the current block number
        int blockNumber = Mathf.Min(currentBlockNumber, TOTAL_BLOCKS);
        Debug.Log($"GetCurrentBlockNumber: Formal trial - Block number: {blockNumber}");
        return blockNumber;

        //         // Return 1-based index for display
        // return currentBlockNumber + 1;
    }

    private float GetBlockElapsedTime()
    {
        float rawElapsed = Time.time - blockStartTime;
        return Mathf.Clamp(rawElapsed, 0, BLOCK_DURATION);
    }

    // public int GetCurrentBlockNumber()
    // {
    //     // For practice trials, always return block 0
    //     if (IsCurrentTrialPractice())
    //     {
    //         Debug.Log("Practice trial - Block number: 0");
    //         return 0;
    //     }

    //     // For formal trials, return the current block number (one-based index)
    //     int blockNumber = Mathf.Min(currentBlockNumber + 1, TOTAL_BLOCKS); // To return a one-based index directly
    //     Debug.Log($"Formal trial - Current block number: {blockNumber}");
    //     return blockNumber;
    // }

    public int GetCurrentTrialIndex() => currentTrialIndex;
    // public int GetCurrentTrialInBlock() => currentTrialIndex % TRIALS_PER_BLOCK + 1;
    // public int GetTotalTrialsInBlock() => TRIALS_PER_BLOCK;
    // public Sprite GetCurrentTrialSprite() => trials[currentTrialIndex].EffortSprite;
    // public Sprite GetStoredTrialSprite(Sprite sprite) => currentTrialSprite;
    // public Vector2 GetCurrentTrialPlayerPosition() => trials[currentTrialIndex].PlayerPosition;
    // public Vector2 GetCurrentTrialRewardPosition() => trials[currentTrialIndex].RewardPosition;
    public void StoreCurrentTrialSprite(Sprite sprite)
    {
        currentTrialSprite = sprite;
        Debug.Log($"Stored trial sprite: {sprite?.name ?? "NULL"}");
    }


    // private Vector2 GetPlayerPosition()
    // {
    //     var player = GameObject.FindWithTag("Player");
    //     return player != null ? new Vector2(player.transform.position.x, player.transform.position.y) : Vector2.zero;
    // }

    // Update the GetCurrentTrialSprite method with proper validation
    public Sprite GetCurrentTrialSprite()
    {
        if (!ValidateTrialAccess())
        {
            Debug.LogWarning("Returning default sprite due to validation failure");
            return level1Sprite; // Return a default sprite instead of null
        }

        Trial currentTrial = trials[currentTrialIndex];
        if (currentTrial == null)
        {
            Debug.LogError($"Trial at index {currentTrialIndex} is null!");
            return level1Sprite;
        }

        if (currentTrial.EffortSprite == null)
        {
            Debug.LogError($"EffortSprite is null for trial {currentTrialIndex}!");
            return level1Sprite;
        }

        return currentTrial.EffortSprite;
    }

    public int GetCurrentTrialRewardValue() => REWARD_VALUE;
    // public Sprite GetFormalCurrentTrialSprite() => trials[currentTrialIndex].EffortSprite;
    // public float GetTrialDuration() => TRIAL_DURATION;
    // public List<Vector2> GetRewardPositions() => rewardPositions;
    // public List<(float collisionTime, float movementDuration)> GetRewardCollectionTimings() => rewardCollectionTimings;
    #endregion

    /// <summary>
    /// Represents a single trial in the experiment.
    /// </summary>
    [System.Serializable]
    public class Trial
    {
        public Sprite EffortSprite { get; private set; }
        public Vector2 PlayerPosition { get; private set; }
        public Vector2 RewardPosition { get; private set; }
        public int BlockIndex { get; private set; }
        public int BlockOrder { get; private set; }

        public Trial(Sprite effortSprite, Vector2 playerPosition, Vector2 rewardPosition, int blockIndex, int blockOrder)
        {
            this.EffortSprite = effortSprite;
            // Round positions to ensure they align with grid cells
            this.PlayerPosition = new Vector2(
                Mathf.Round(playerPosition.x),
                Mathf.Round(playerPosition.y)
            );
            this.RewardPosition = new Vector2(
                Mathf.Round(rewardPosition.x),
                Mathf.Round(rewardPosition.y)
            );
            this.BlockIndex = blockIndex;
            this.BlockOrder = blockOrder;
        }
    }
}