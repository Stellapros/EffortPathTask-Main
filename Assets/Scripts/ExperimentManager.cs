using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using System.Text; // Add this for StringBuilder


/// <summary>
/// Manages the overall flow of the experiment, including trial generation, scene transitions, and data logging.
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    #region Singleton
    public static ExperimentManager Instance { get; private set; }
    #endregion

    #region Constants
    private const int TOTAL_BLOCKS = 2; // Total number of blocks
    private const int TRIALS_PER_BLOCK = 9; // Number of trials in each block
    private const int TOTAL_TRIALS = TOTAL_BLOCKS * TRIALS_PER_BLOCK; // Calculated total trials
    // private const int PRACTICE_TRIALS = 6;
    private const float TRIAL_DURATION = 5f; // Duration of each tr ial in seconds
    private const int REWARD_VALUE = 10; // Value of the reward for each trial
    private const float SKIP_DELAY = 3f; // Delay before showing the next trial after skipping
                                         // private const float GRID_CELL_SIZE = 1f; // Size of each grid cell
    private string currentDecisionType = string.Empty;
    // Add this property to track the current trial's decision
    public string CurrentDecisionType
    {
        get { return currentDecisionType; }
        private set { currentDecisionType = value; }
    }

    #endregion

    #region Serialized Fields
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level2Sprite;
    [SerializeField] private Sprite level3Sprite;
    [SerializeField] private Sprite currentTrialSprite;
    // [SerializeField] public Sprite[] levelSprites = new Sprite[3];
    [SerializeField] private List<BlockConfig> blockConfigs;
    // [SerializeField] private float[] blockDistances = new float[3] { 3f, 5f, 7f };
    [SerializeField] private string block1InstructionScene = "Block1_Instructions";
    [SerializeField] private string block2InstructionScene = "Block2_Instructions";
    [SerializeField] private string decisionPhaseScene = "DecisionPhase";
    [SerializeField] private string gridWorldScene = "GridWorld";
    [SerializeField] private string restBreakScene = "RestBreak";
    [SerializeField] private string endExperimentScene = "EndExperiment";
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
    private int currentTrialIndex = 0;
    // private int practiceTrialIndex = 0;
    private int currentBlockNumber = 0;
    private bool experimentStarted = false;
    private bool isTourCompleted = false;
    private bool isPractice = true;
    // private int practiceTrialsCount = 6; // Number of practice trials
    private float trialStartTime;
    private List<Vector2> rewardPositions = new List<Vector2>();
    private List<(float collisionTime, float movementDuration)> rewardCollectionTimings = new List<(float, float)>();
    public GridManager gridManager;
    public ScoreManager scoreManager;
    public LogManager logManager;

    // Add new fields for decision phase timing
    private const float DECISION_TIMEOUT = 2.5f; // Time allowed for making a decision
    // private const float SKIP_PENALTY_DURATION = 3f; // Duration of skip/no-decision penalty
    private const float NO_DECISION_PENALTY_DURATION = 5f; // Duration of no-decision penalty
    private const string PENALTY_SCENE = "TimePenalty";
    private bool decisionMade = false;
    private float decisionStartTime;
    private Coroutine decisionTimeoutCoroutine;

    private bool isExperimentActive = true;
    public bool IsExperimentActive => isExperimentActive;

    // Add tracking variable
    private bool hasShownBlockInstructions = false;
    // private bool isFromPenalty = false;

    private int currentBlockTrialIndex => currentTrialIndex % TRIALS_PER_BLOCK;
    // private bool isLastTrialInBlock => currentBlockTrialIndex == TRIALS_PER_BLOCK - 1;
    // private bool isLastTrialInExperiment => currentTrialIndex == TOTAL_TRIALS - 1;
    // private bool isProcessingDecision = false; // Add this flag to prevent multiple decision processing

    #endregion

    #region Events
    // public event System.Action<bool> OnPracticeTrialsComplete;
    public event System.Action<bool> OnTrialEnded;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Implement the singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeComponents();
            InitializeSceneQueue();

        }
        else
        {
            Destroy(gameObject);
        }

        logManager = FindAnyObjectByType<LogManager>();
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
            logManager.LogExperimentStart(true);
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
        decisionMade = false;
        int effortLevel = GetCurrentTrialEffortLevel();
        int requiredPresses = GetCurrentTrialEV();

        logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, effortLevel, requiredPresses, isPractice);

        // Reset the decision type at the start of each trial
        CurrentDecisionType = string.Empty;

        // Start the decision phase
        decisionStartTime = Time.time;
        decisionTimeoutCoroutine = StartCoroutine(DecisionTimeout());
    }


    void OnRewardPlacement(Vector2 position)
    {
        int effortLevel = GetCurrentTrialEffortLevel();
        logManager.LogRewardPosition(currentTrialIndex, position, effortLevel);
    }

    void OnDecisionMade(string decisionType, float reactionTime)
    {
        logManager.LogDecisionMetrics(currentTrialIndex, currentBlockNumber, decisionType, reactionTime);
    }

    void OnTrialComplete(bool rewardCollected, float completionTime)
    {
        int effortLevel = GetCurrentTrialEffortLevel();

        // Now we can use CurrentDecisionType here
        logManager.LogTrialOutcome(currentTrialIndex, currentBlockNumber, CurrentDecisionType,
                                 rewardCollected, completionTime, effortLevel);

        if (IsLastTrialInBlock())
        {
            logManager.LogBehavioralSummary(currentBlockNumber);
        }

        // Reset the decision type for the next trial
        CurrentDecisionType = string.Empty;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion

    #region Initialization Methods
    // Modify InitializeComponents to ensure managers exist
    private void InitializeComponents()
    {
        InitializeTrials();
        InitializeSpriteToEffortMap();
        LogEffortLevels();
        LogPressesPerEffortLevel();

        // Initialize LogManager
        EnsureLogManagerExists();

        // Initialize ScoreManager
        scoreManager = FindAnyObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
        }

        // Modified intro scene flow with explicit configuration
        introductorySceneFlow = new List<SceneConfig>
    {
        new SceneConfig
        {
            sceneName = "TitlePage",
            minimumDisplayTime = 2f,
            requiresButtonPress = true,
            nextScene = "BeforeStartingScreen"
        },
        new SceneConfig
        {
            sceneName = "BeforeStartingScreen",
            minimumDisplayTime = 1f,
            requiresButtonPress = true,
            nextScene = "StartScreen"
        },
        new SceneConfig
        {
            sceneName = "StartScreen",
            minimumDisplayTime = 0f,
            requiresButtonPress = true,
            nextScene = "CalibrationCounter"
        },
        new SceneConfig
        {
            sceneName = "CalibrationCounter",
            minimumDisplayTime = 3f,
            nextScene = "TourGame"
        },
        new SceneConfig
        {
            sceneName = "TourGame",
            minimumDisplayTime = 3f,
            nextScene = "InstructionsScreen"
        }
    };
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

    // Add these validation methods
    private bool ValidateTrialAccess()
    {
        if (trials == null)
        {
            Debug.LogError("Trials list is null!");
            return false;
        }

        if (trials.Count == 0)
        {
            Debug.LogError("Trials list is empty!");
            return false;
        }

        if (currentTrialIndex < 0 || currentTrialIndex >= trials.Count)
        {
            Debug.LogError($"Invalid trial index: {currentTrialIndex}. Valid range is 0-{trials.Count - 1}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Initializes all trials for the experiment & Block Randomization
    /// </summary>
    private void InitializeTrials()
    {
        if (level1Sprite == null || level2Sprite == null || level3Sprite == null)
        {
            Debug.LogError("One or more effort level sprites not assigned!");
            return;
        }

        trials = new List<Trial>();
        List<int> blockOrder = new List<int>();

        for (int i = 0; i < TOTAL_BLOCKS; i++)
        {
            blockOrder.Add(i);
        }
        blockOrder = blockOrder.OrderBy(x => Random.value).ToList();

        for (int i = 0; i < TOTAL_BLOCKS; i++)
        {
            int blockIndex = blockOrder[i];
            List<Trial> blockTrials = GenerateBlockTrials(blockIndex, i);

            if (blockTrials == null || blockTrials.Count == 0)
            {
                Debug.LogError($"Failed to generate trials for block {i + 1}");
                continue;
            }

            trials.AddRange(blockTrials);
            Debug.Log($"Added {blockTrials.Count} trials for block {i + 1}");
        }

        // Validate final trial count
        if (trials.Count != TOTAL_TRIALS)
        {
            Debug.LogError($"Expected {TOTAL_TRIALS} trials but generated {trials.Count}");
        }

        Debug.Log($"Initialized {trials.Count} total trials");
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


    // Add this method to help with debugging
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

    // Add new field to prevent multiple transitions
    // private bool isTransitioning = false;

    // private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     LoadScene(sceneName);
    //     // isTransitioning = false;
    // }

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
        Debug.Log($"Scene loaded: {scene.name}");

        if (currentSceneConfig != null && scene.name == currentSceneConfig.sceneName)
        {
            SetupSceneButtons();

            if (!currentSceneConfig.requiresButtonPress)
            {
                StartCoroutine(AutoAdvanceScene());
            }
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
    // private void SetupGridWorldPhase()
    // {
    //     Debug.Log("Setting up Grid World Phase");
    //     logManager.LogGridWorldPhaseStart(currentTrialIndex);
    // }

    private void CheckAndHandleBlockTransition()
    {
        if (IsLastTrialInBlock())
        {
            logManager.LogBlockEnd(currentBlockNumber);

            if (currentBlockNumber < TOTAL_BLOCKS - 1)
            {
                currentBlockNumber++;
                logManager.LogBlockStart(currentBlockNumber);
            }
            else
            {
                EndExperiment();
            }
        }
    }
    #endregion


    #region Experiment Control Methods
    /// <summary>
    /// Block Generation: The GenerateBlockTrials() method is called 
    /// with both the original block index and its randomized order:
    /// </summary>
    /// <param name="blockIndex"></param>
    /// <param name="blockOrder"></param>
    /// <returns></returns>
    private List<Trial> GenerateBlockTrials(int blockIndex, int blockOrder)
    {
        List<Trial> blockTrials = new List<Trial>();
        List<int> effortLevels = GetEffortLevelsForBlock(blockIndex);

        if (gridManager == null)
        {
            Debug.LogError("GridManager is null in GenerateBlockTrials!");
            return blockTrials;
        }

        for (int i = 0; i < TRIALS_PER_BLOCK; i++)
        {
            try
            {
                // Ensure grid is initialized
                gridManager.EnsureInitialization();

                // Spawn player at random position using GridManager
                Vector2 playerSpawnPosition = gridManager.GetRandomAvailablePosition();
                Debug.Log($"Player spawn position: {playerSpawnPosition}");

                // Get reward position exactly 5 cells away
                Vector2 rewardPosition = gridManager.GetPositionAtDistance(playerSpawnPosition, 5);
                Debug.Log($"Reward position: {rewardPosition}");
                rewardPositions.Add(rewardPosition);

                // Get effort level for this trial
                int effortLevel = effortLevels[i];
                Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

                Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockOrder);
                blockTrials.Add(trial);

                Debug.Log($"Created trial {i + 1} in block {blockIndex + 1} with effort level {effortLevel}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating trial {i}: {e.Message}");
            }
        }

        return blockTrials;
    }

    /// <summary>
    /// Returns a list of effort levels for a specific block.
    /// </summary>
    private List<int> GetEffortLevelsForBlock(int blockIndex)
    {
        List<int> effortLevels = new List<int>();

        switch (blockIndex)
        {
            case 0: // Block 1: 3:2:1 ratio (scaled for 9 trials)
                effortLevels.AddRange(Enumerable.Repeat(1, 5)); // 5 low effort trials
                effortLevels.AddRange(Enumerable.Repeat(2, 3)); // 3 medium effort trials
                effortLevels.AddRange(Enumerable.Repeat(3, 1)); // 1 high effort trial
                break;
            case 1: // Block 2: 1:2:3 ratio (scaled for 9 trials)
                effortLevels.AddRange(Enumerable.Repeat(1, 1)); // 1 low effort trial
                effortLevels.AddRange(Enumerable.Repeat(2, 3)); // 3 medium effort trials
                effortLevels.AddRange(Enumerable.Repeat(3, 5)); // 5 high effort trials
                break;
            default:
                Debug.LogError($"Invalid block index: {blockIndex}");
                // Add default values to prevent empty list
                effortLevels.AddRange(Enumerable.Repeat(1, TRIALS_PER_BLOCK));
                break;
        }

        // Add validation
        if (effortLevels.Count != TRIALS_PER_BLOCK)
        {
            Debug.LogError($"Block {blockIndex} has {effortLevels.Count} trials instead of expected {TRIALS_PER_BLOCK}");
            // Log the actual distribution for debugging
            Debug.Log($"Current distribution - Low: {effortLevels.Count(x => x == 1)}, " +
                     $"Medium: {effortLevels.Count(x => x == 2)}, " +
                     $"High: {effortLevels.Count(x => x == 3)}");
        }

        return effortLevels.OrderBy(x => Random.value).ToList(); // Shuffle effort levels
    }

    // Add method to log effort levels for each trial
    // Update LogTrialEffortDistribution method with null checks
    // private void LogTrialEffortDistribution()
    // {
    //     if (trials == null || spriteToEffortMap == null)
    //     {
    //         Debug.LogError("Cannot log trial effort distribution: trials or spriteToEffortMap is null");
    //         return;
    //     }

    //     StringBuilder log = new StringBuilder("Trial Effort Level Distribution:\n");

    //     for (int block = 0; block < TOTAL_BLOCKS; block++)
    //     {
    //         log.AppendLine($"\nBlock {block + 1}:");
    //         var blockTrials = trials.Skip(block * TRIALS_PER_BLOCK).Take(TRIALS_PER_BLOCK);

    //         foreach (var (trial, index) in blockTrials.Select((t, i) => (t, i)))
    //         {
    //             if (trial?.EffortSprite != null && spriteToEffortMap.ContainsKey(trial.EffortSprite))
    //             {
    //                 int effortLevel = spriteToEffortMap[trial.EffortSprite] + 1;
    //                 log.AppendLine($"Trial {index + 1}: Effort Level {effortLevel}");
    //             }
    //             else
    //             {
    //                 log.AppendLine($"Trial {index + 1}: Invalid trial data");
    //             }
    //         }
    //     }

    //     Debug.Log(log.ToString());
    // }

    // Add helper method to get sprite for effort level
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

    // Update InitializeSpriteToEffortMap with validation
    private void InitializeSpriteToEffortMap()
    {
        if (level1Sprite == null || level2Sprite == null || level3Sprite == null)
        {
            Debug.LogError("One or more effort level sprites are not assigned!");
            return;
        }

        spriteToEffortMap = new Dictionary<Sprite, int>
    {
        { level1Sprite, 0 }, // Easy 
        { level2Sprite, 1 }, // Medium
        { level3Sprite, 2 }  // Hard
    };

        Debug.Log($"Initialized spriteToEffortMap: Easy={spriteToEffortMap[level1Sprite]}, " +
                  $"Medium={spriteToEffortMap[level2Sprite]}, Hard={spriteToEffortMap[level3Sprite]}");
    }

    /// <summary>
    /// Starts the experiment by transitioning to the DecisionPhase scene.
    /// </summary>
    // Update StartExperiment to show initial block instructions
    public void StartExperiment()
    {
        if (!experimentStarted)
        {
            Debug.Log("Starting experiment");
            experimentStarted = true;
            currentTrialIndex = 0;
            currentBlockNumber = 0;
            ScoreManager.Instance.ResetScore();

            logManager.LogExperimentStart(false);
            SetupNewTrial();
            ShowBlockInstructions(); // Show instructions before first block
        }
    }

    // Add this method to ensure LogManager exists
    private void EnsureLogManagerExists()
    {
        if (logManager == null)
        {
            logManager = FindAnyObjectByType<LogManager>();
            if (logManager == null)
            {
                GameObject logManagerObj = new GameObject("LogManager");
                logManager = logManagerObj.AddComponent<LogManager>();
            }
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
    /// Sets up the DecisionPhase scene.
    /// </summary>
    // public void SetupDecisionPhase()
    // {
    //     Debug.Log("Setting up Decision Phase");

    //     if (!ValidateTrialAccess())
    //     {
    //         Debug.LogError("Cannot setup decision phase - invalid trial state");
    //         return;
    //     }

    //     DecisionManager decisionManager = FindAnyObjectByType<DecisionManager>();
    //     if (decisionManager == null)
    //     {
    //         Debug.LogError("DecisionManager not found in the scene!");
    //         return;
    //     }

    //     try
    //     {
    //         // Setup the decision phase UI
    //         decisionManager.SetupDecisionPhase();

    //         // Reset decision state
    //         decisionStartTime = Time.time;
    //         decisionMade = false;

    //         // Start decision timeout countdown
    //         if (decisionTimeoutCoroutine != null)
    //         {
    //             StopCoroutine(decisionTimeoutCoroutine);
    //         }
    //         decisionTimeoutCoroutine = DecisionTimeout();
    //         StartCoroutine(decisionTimeoutCoroutine);

    //         // Log the start of decision phase
    //         logManager.LogDecisionPhaseStart(currentTrialIndex);
    //     }
    //     catch (System.Exception e)
    //     {
    //         Debug.LogError($"Error setting up decision phase: {e.Message}\n{e.StackTrace}");
    //     }
    // }

    // private void LogFirstTrial()
    // {
    //     // float currentBlockDistance = GetCurrentBlockDistance();
    //     // float currentBlockDistance = 5f;
    //     int effortLevel = GetCurrentTrialEffortLevel();
    //     int pressesRequired = GetCurrentTrialEV();
    //     // logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, currentBlockDistance, effortLevel, pressesRequired, isPractice);
    //     logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, effortLevel, pressesRequired, isPractice);
    // }

    private bool IsLastTrialInBlock()
    {
        return (currentTrialIndex + 1) % TRIALS_PER_BLOCK == 0;
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    public void HandleDecision(bool workDecision)
    {
        if (decisionMade) return;
        decisionMade = true;

        if (logManager == null)
        {
            Debug.LogError("LogManager is null in HandleDecision method!");
            return;
        }

        // Stop the timeout coroutine
        if (decisionTimeoutCoroutine != null)
        {
            StopCoroutine(decisionTimeoutCoroutine);
            decisionTimeoutCoroutine = null;
        }

        float decisionReactionTime = Time.time - decisionStartTime;
        string decisionType = workDecision ? "Work" : "Skip";
        CurrentDecisionType = decisionType; // Store the decision type

        logManager.LogDecisionMade(currentTrialIndex, decisionType);
        logManager.LogDecisionMetrics(currentTrialIndex, currentBlockNumber, CurrentDecisionType, decisionReactionTime);

        if (workDecision)
        {
            LoadScene(gridWorldScene);
        }
        else
        {
            StartCoroutine(HandleSkipPenalty());
        }
    }

    private IEnumerator HandleSkipPenalty()
    {
        float penaltyStartTime = Time.time;

        // Log skip penalty start
        logManager.LogPenaltyStart(currentTrialIndex, "Skip", SKIP_DELAY);

        // Load penalty scene
        // LoadScene(PENALTY_SCENE);

        // Wait for skip penalty duration (3 seconds)
        yield return new WaitForSeconds(SKIP_DELAY);

        // Log penalty end
        logManager.LogPenaltyEnd(currentTrialIndex, "Skip");

        // Calculate total duration including penalty
        float totalDuration = Time.time - penaltyStartTime;

        // Process trial completion with skip result
        ProcessTrialCompletion(false, totalDuration);
    }

    private void HandleNoDecision()
    {
        if (decisionMade) return;
        decisionMade = true;

        Debug.Log("No decision made - applying 5s penalty");
        CurrentDecisionType = "NoDecision"; // Store the decision type
        logManager.LogNoDecision(currentTrialIndex);

        StartCoroutine(HandleNoDecisionPenalty());
    }

    private IEnumerator HandleNoDecisionPenalty()
    {
        float penaltyStartTime = Time.time;

        // Log no-decision penalty start
        logManager.LogPenaltyStart(currentTrialIndex, "NoDecision", NO_DECISION_PENALTY_DURATION);

        // Load penalty scene
        LoadScene(PENALTY_SCENE);

        // Wait for no-decision penalty duration (5 seconds)
        yield return new WaitForSeconds(NO_DECISION_PENALTY_DURATION);

        // Log penalty end
        logManager.LogPenaltyEnd(currentTrialIndex, "NoDecision");

        // Calculate total duration including penalty
        float totalDuration = Time.time - penaltyStartTime;

        // Process trial completion with no-decision result
        ProcessTrialCompletion(false, totalDuration);
    }

    private void ProcessTrialCompletion(bool rewardCollected, float trialDuration)
    {
        // Log trial completion with timing metrics
        if (logManager != null)
        {
            // Get the actual reaction time from GameController if available
            float actionReactionTime = 0f;
            if (GameController.Instance != null)
            {
                actionReactionTime = GameController.Instance.GetActionReactionTime();
            }

            // Log trial end with the correct parameter types
            logManager.LogTrialEnd(currentTrialIndex, rewardCollected, trialDuration, actionReactionTime);
            
            // Separately log the decision type if needed
            logManager.LogDecisionOutcome(currentTrialIndex, CurrentDecisionType);
        }

        // Reset decision state
        decisionMade = false;
        decisionStartTime = 0f;

        Debug.Log($"Trial completed. Current trial index: {currentTrialIndex}, Current block: {currentBlockNumber}");

        // Check if we've completed all trials in the current block
        bool isBlockComplete = (currentTrialIndex + 1) % TRIALS_PER_BLOCK == 0;

        if (isBlockComplete)
        {
            Debug.Log($"Block {currentBlockNumber + 1} completed");
            // Don't increment block number here - it will be done after the rest break
            LoadScene(restBreakScene);
        }
        else if (currentTrialIndex + 1 >= TOTAL_TRIALS)
        {
            EndExperiment();
        }
        else
        {
            // Increment trial index for next trial within the same block
            currentTrialIndex++;
            SetupNewTrial();
            LoadScene(decisionPhaseScene);
        }

        // Reset the decision type for the next trial
        CurrentDecisionType = string.Empty;
    }

    // Revise ProcessNextTransition to handle all transition cases
    // private void ProcessNextTransition()
    // {
    //     currentTrialIndex++;

    //     if (isLastTrialInExperiment)
    //     {
    //         EndExperiment();
    //     }
    //     else if (isLastTrialInBlock)
    //     {
    //         currentBlockNumber++;
    //         LoadScene(restBreakScene);
    //     }
    //     else
    //     {
    //         SetupNewTrial();
    //         LoadScene(decisionPhaseScene);
    //     }
    // }

    /// <summary>
    /// Coroutine to show the next trial after a delay when skipping.
    /// </summary>
    // private IEnumerator ShowNextTrialAfterDelay()
    // {
    //     yield return new WaitForSeconds(SKIP_DELAY);

    //     if (currentTrialIndex < TOTAL_TRIALS - 1)
    //     {
    //         if (currentTrialIndex % TRIALS_PER_BLOCK == TRIALS_PER_BLOCK - 1)
    //         {
    //             // End of block
    //             EndCurrentBlock();
    //             currentBlockNumber++;

    //             if (currentBlockNumber < TOTAL_BLOCKS)
    //             {
    //                 LoadScene(restBreakScene);
    //             }
    //             else
    //             {
    //                 EndExperiment();
    //             }
    //         }
    //         else
    //         {
    //             // Move to next trial within the same block
    //             currentTrialIndex++;
    //             SetupNewTrial();
    //             LoadScene(decisionPhaseScene);
    //         }
    //     }
    //     else
    //     {
    //         EndExperiment();
    //     }
    // }

    // Update StartFormalExperiment to show initial block instructions
    // public void StartFormalExperiment()
    // {
    //     Debug.Log("Starting formal experiment");
    //     isPractice = false;
    //     currentTrialIndex = 0;
    //     currentBlockNumber = 0;
    //     ScoreManager.Instance.ResetScore();

    //     logManager.LogExperimentStart(false);
    //     LogFirstTrial();

    //     StartNewBlock();
    //     SetupNewTrial();
    //     ShowBlockInstructions(); // Show instructions before first block
    // }

    // public bool CheckExperimentStatus()
    // {
    //     if (!isExperimentActive)
    //     {
    //         Debug.Log("Experiment is not active");
    //         return false;
    //     }
    //     return true;
    // }

    // public string DetermineNextScene()
    // {
    //     Debug.Log($"Determining next scene - CurrentTrialIndex: {currentTrialIndex}, BlockNumber: {currentBlockNumber}");

    //     // If we're at the start of a new block and haven't shown instructions
    //     if (currentTrialIndex % TRIALS_PER_BLOCK == 0 && !hasShownBlockInstructions)
    //     {
    //         return (currentBlockNumber == 0) ? block1InstructionScene : block2InstructionScene;
    //     }

    //     // If we're coming from a penalty scene
    //     if (isFromPenalty)
    //     {
    //         isFromPenalty = false;
    //         return decisionPhaseScene;
    //     }

    //     // Default to decision phase
    //     return decisionPhaseScene;
    // }

    // Update MoveToNextTrial to handle block transitions correctly
    public void MoveToNextTrial()
    {
        if (!isExperimentActive)
        {
            Debug.Log("Experiment not active, ignoring MoveToNextTrial call");
            return;
        }

        currentTrialIndex++;
        Debug.Log($"Moving to next trial. Current index: {currentTrialIndex}, Total trials: {TOTAL_TRIALS}");

        // Check if we've completed all trials
        if (currentTrialIndex >= TOTAL_TRIALS)
        {
            Debug.Log("All trials completed - ending experiment");
            EndExperiment();
            return;
        }

        // Check if we're at a block boundary
        if (currentTrialIndex % TRIALS_PER_BLOCK == 0)
        {
            Debug.Log($"Block {GetCurrentBlockNumber()} complete - showing rest break");
            LoadScene(restBreakScene);
        }
        else
        {
            Debug.Log($"Continuing with next trial in block {GetCurrentBlockNumber()}");
            SetupNewTrial();
            LoadScene(decisionPhaseScene);
        }
    }

    // Add new method for decision timeout
    private IEnumerator DecisionTimeout()
    {
        yield return new WaitForSeconds(DECISION_TIMEOUT);

        if (!decisionMade)
        {
            Debug.Log("Decision timeout - handling as no decision");
            HandleNoDecision();
        }
    }

    // // New method to centralize trial completion and progression logic
    // private void EndTrialAndProgress(bool rewardCollected, float trialDuration = 0f, float actionReactionTime = 0f)
    // {
    //     // Log trial completion with timing metrics
    //     if (logManager != null)
    //     {
    //         logManager.LogTrialEnd(currentTrialIndex, rewardCollected, trialDuration, actionReactionTime);
    //     }

    //     // Increment trial counter
    //     currentTrialIndex++;

    //     // Handle transitions
    //     if (currentTrialIndex >= TOTAL_TRIALS)
    //     {
    //         EndExperiment();
    //     }
    //     else if (currentTrialIndex % TRIALS_PER_BLOCK == 0)
    //     {
    //         // End of block
    //         currentBlockNumber++;
    //         LoadScene(restBreakScene);
    //     }
    //     else
    //     {
    //         // Continue to next trial
    //         SetupNewTrial();
    //         LoadScene(decisionPhaseScene);
    //     }
    // }

    private void SetupNewTrial()
    {
        if (!IsValidTrialIndex(currentTrialIndex))
        {
            Debug.LogError("Cannot setup trial - invalid trial index");
            EndExperiment();
            return;
        }

        Debug.Log($"Setting up trial {currentTrialIndex} in block {currentBlockNumber}");

        if (trials == null || currentTrialIndex >= trials.Count)
        {
            Debug.LogError($"Invalid trial setup: trials={trials?.Count ?? 0}, currentTrialIndex={currentTrialIndex}");
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
        int pressesRequired = GetCurrentTrialEV();

        Debug.Log($"Trial {currentTrialIndex} setup - Effort Level: {effortLevel}, Presses Required: {pressesRequired}");

        if (logManager != null)
        {
            logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, effortLevel, pressesRequired, isPractice);
        }
        else
        {
            Debug.LogError("LogManager is null during trial setup!");
        }
    }

    // Add this validation method to check trial index before any critical operations
    private bool IsValidTrialIndex(int index)
    {
        if (index < 0 || index >= TOTAL_TRIALS)
        {
            Debug.LogError($"Invalid trial index: {index}. Valid range is 0-{TOTAL_TRIALS - 1}");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Continues the experiment after a break between blocks.
    /// </summary>
    // Revise ContinueAfterBreak to properly handle block transitions
    public void ContinueAfterBreak()
    {
        Debug.Log($"Continuing after break. Current block: {currentBlockNumber}");
        
        // Increment block number after the break
        currentTrialIndex++;
        currentBlockNumber++;
        
        Debug.Log($"Starting block {currentBlockNumber + 1}");
        
        hasShownBlockInstructions = false;
        ShowBlockInstructions();
    }

    /// <summary>
    /// Starts a new block of trials.
    /// </summary>
    // Update StartNewBlock to show instructions
    private void StartNewBlock()
    {
        Debug.Log($"Starting Block {currentBlockNumber + 1}");
        ShowBlockInstructions();
    }

    // Update ShowBlockInstructions for clearer block transition
    private void ShowBlockInstructions()
    {
        Debug.Log($"Showing instructions for block {GetCurrentBlockNumber()}");
        string instructionScene = (currentBlockNumber == 0) ? block1InstructionScene : block2InstructionScene;
        hasShownBlockInstructions = true;
        LoadScene(instructionScene);
    }

    // Revise ContinueAfterInstructions for clearer flow
    public void ContinueAfterInstructions()
    {
        Debug.Log($"Continuing after instructions for block {currentBlockNumber + 1}");
        SetupNewTrial();
        LoadScene(decisionPhaseScene);
    }

    /// <summary>
    /// Ends the current block of trials.
    /// </summary>
    // Update EndCurrentBlock for proper transitions
    private void EndCurrentBlock()
    {
        Debug.Log($"Ending block {currentBlockNumber + 1}");
        logManager.LogBlockEnd(currentBlockNumber);

        // Check if we've completed all blocks
        if (currentBlockNumber < TOTAL_BLOCKS - 1)
        {
            currentBlockNumber++; // Increment block number before rest break
            LoadScene(restBreakScene);
        }
        else
        {
            EndExperiment();
        }
    }

    /// <summary>
    /// Ends the current trial and logs the result.
    /// </summary>
    // Update EndTrial to use the centralized progression logic
    public void EndTrial(bool rewardCollected, bool isPracticeTrial = false)
    {
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

        if (!isExperimentActive)
        {
            Debug.Log("Experiment already ended, ignoring duplicate call");
            return;
        }

        isExperimentActive = false;
        StopAllCoroutines();

        if (logManager != null)
        {
            logManager.LogExperimentEnd();
            LogManager.Instance.DumpUnloggedTrials();
        }

        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.StopMusic();
        }

        float totalTime = Time.time - PlayerPrefs.GetFloat("ExperimentStartTime", 0f);
        Debug.Log($"Total experiment time: {totalTime} seconds");

        // Force load end scene with a small delay to ensure cleanup
        StartCoroutine(ForceLoadEndScene());
    }

    private IEnumerator ForceLoadEndScene()
    {
        yield return new WaitForSeconds(0.5f); // Short delay to ensure cleanup
        SceneManager.LoadScene(endExperimentScene);
    }
    #endregion

    #region Logging Methods
    // Add this method to log effort levels for each trial
    private void LogEffortLevels()
    {
        for (int i = 0; i < trials.Count; i++)
        {
            int blockIndex = i / TRIALS_PER_BLOCK;
            int trialInBlock = i % TRIALS_PER_BLOCK + 1;
            int effortLevel = spriteToEffortMap[trials[i].EffortSprite];
            Debug.Log($"Block {blockIndex + 1}, Trial {trialInBlock}: Effort Level {effortLevel}");
        }
    }

    /// <summary>
    /// Logs the player's decision for the current trial.
    /// </summary>
    // private void LogDecision(bool worked)
    // {
    //     Debug.Log($"Trial {currentTrialIndex}: Decision - {(worked ? "Worked" : "Skipped")}, Effort Level: {GetCurrentTrialEV()}");
    // }

    /// <summary>
    /// Logs the outcome of the current trial.
    /// </summary>
    // private void LogTrialOutcome(bool rewardCollected)
    // {
    //     Debug.Log($"Block {currentBlockNumber}, Trial {currentTrialIndex}: Outcome - {(rewardCollected ? "Reward Collected" : "Time Out")}, Effort Level: {GetCurrentTrialEV()}");
    // }

    private void LogPressesPerEffortLevel()
    {
        string logEntry = "Presses Per Effort Level:";
        foreach (var kvp in spriteToEffortMap)
        {
            string spriteName = kvp.Key.name;
            int effortLevel = kvp.Value;
            // int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 0);
            int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel - 1}", 0);
            logEntry += $"\n{spriteName}: {pressesRequired} presses";
        }
        Debug.Log(logEntry);

        // Log to a file for persistent record
        string fileLogEntry = $"{System.DateTime.Now}: {logEntry}";
        System.IO.File.AppendAllText("experiment_log.txt", fileLogEntry + System.Environment.NewLine);
    }

    /// <summary>
    /// Logs detailed data for the current trial.
    /// </summary>
    // public void LogTrialData(bool completed, float reactionTime, int buttonPresses)
    // {
    //     Debug.Log($"Trial {currentTrialIndex}: Completed - {completed}, Reaction Time - {reactionTime}, Button Presses - {buttonPresses}, Effort Level: {GetCurrentTrialEV()}");
    // }

    /// <summary>
    /// Logs the position of a spawned reward.
    /// </summary>
    // public void LogRewardPosition(Vector2 position)
    // {
    //     rewardPositions.Add(position);
    //     Debug.Log($"Logged reward position: {position}");
    // }

    /// <summary>
    /// Logs the timing information for reward collection.
    /// </summary>
    public void LogRewardCollectionTiming(float collisionTime, float movementDuration)
    {
        rewardCollectionTimings.Add((collisionTime, movementDuration));
        Debug.Log($"Logged reward collection timing - Collision Time: {collisionTime}, Movement Duration: {movementDuration}");
    }

    /// <summary>
    /// Clears all logged data. Call this at the end of the experiment or when starting a new one.
    /// </summary>
    // public void ClearLoggedData()
    // {
    //     rewardPositions.Clear();
    //     rewardCollectionTimings.Clear();
    //     Debug.Log("Cleared all logged experiment data.");
    // }
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
            Debug.LogWarning($"Trials not initialized or currentTrialIndex ({currentTrialIndex}) out of range.");
            return 0;
        }

        Sprite currentSprite = trials[currentTrialIndex].EffortSprite;
        if (spriteToEffortMap == null || !spriteToEffortMap.ContainsKey(currentSprite))
        {
            Debug.LogWarning($"spriteToEffortMap not initialized or doesn't contain the current sprite.");
            return 0;
        }

        int effortLevel = spriteToEffortMap[currentSprite] + 1; // Adding 1 to convert from 0-2 to 1-3 range
        Debug.Log($"Current trial (index: {currentTrialIndex}) Effort Level: {effortLevel}");
        return effortLevel;
    }

    public int GetCurrentTrialEV()
    {
        int effortLevel = GetCurrentTrialEffortLevel();
        int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel - 1}", 0); // Subtract 1 to match the PlayerPrefs keys

        Debug.Log($"Current trial (index: {currentTrialIndex}) Effort Level: {effortLevel}, Presses Required: {pressesRequired}");

        return pressesRequired;
    }

    // public float GetCurrentBlockDistance()
    // {
    //     if (currentBlockNumber < 0 || currentBlockNumber >= blockDistances.Length)
    //     {
    //         Debug.LogError($"Invalid block number: {currentBlockNumber}. Using default distance of 5f.");
    //         return 5f; // Default fallback distance
    //     }
    //     return blockDistances[currentBlockNumber];
    // }

    // Getter methods for various experiment parameters
    // public float GetCurrentBlockDistance() => blockDistances[currentBlockNumber];

    public int GetTotalTrials() => TOTAL_TRIALS;
    public int GetTotalBlocks() => TOTAL_BLOCKS;

    public bool IsCurrentTrialPractice() => isPractice;
    // public int GetCurrentBlockNumber() => currentBlockNumber;

    // Update display logic in GetCurrentBlockNumber to use 1-based indexing for display
    public int GetCurrentBlockNumber() => currentBlockNumber + 1; // Convert 0-based to 1-based indexing for display
    public int GetCurrentTrialIndex() => currentTrialIndex;
    // public int GetCurrentTrialInBlock() => currentTrialIndex % TRIALS_PER_BLOCK + 1;
    public int GetTotalTrialsInBlock() => TRIALS_PER_BLOCK;
    // public Sprite GetCurrentTrialSprite() => trials[currentTrialIndex].EffortSprite;
    public Sprite GetStoredTrialSprite(Sprite sprite) => currentTrialSprite;
    public Vector2 GetCurrentTrialPlayerPosition() => trials[currentTrialIndex].PlayerPosition;
    public Vector2 GetCurrentTrialRewardPosition() => trials[currentTrialIndex].RewardPosition;
    public void StoreCurrentTrialSprite(Sprite sprite)
    {
        currentTrialSprite = sprite;
        Debug.Log($"Stored trial sprite: {sprite?.name ?? "NULL"}");
    }

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
    public float GetTrialDuration() => TRIAL_DURATION;
    public List<Vector2> GetRewardPositions() => rewardPositions;
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

        // public Trial(Sprite effortSprite, Vector2 playerPosition, Vector2 rewardPosition, int blockIndex, int blockOrder)
        // {
        //     this.EffortSprite = effortSprite;
        //     this.PlayerPosition = playerPosition;
        //     this.RewardPosition = rewardPosition;
        //     this.BlockIndex = blockIndex;
        //     this.BlockOrder = blockOrder;
        // }
    }
    public class BlockConfig
    {
        public float playerRewardDistance;
        public int numberOfTrials;
        // Add other block-specific parameters here
    }
    #region Public Methods
    public void SkipToScene(string sceneName)
    {
        // Clear the queue and load the specified scene
        sceneQueue.Clear();
        SceneManager.LoadScene(sceneName);
    }

    public void RestartIntroFlow()
    {
        InitializeSceneQueue();
        StartSceneFlow();
    }

    public bool IsIntroScene(string sceneName)
    {
        return introductorySceneFlow.Any(config => config.sceneName == sceneName);
    }
    #endregion
}