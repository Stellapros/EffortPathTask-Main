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
    public const float BLOCK_DURATION = 20f; // 2 minutes per block
    private const float MIN_TRIAL_DURATION = 10f; // Minimum time needed for a trial
    private const int TOTAL_BLOCKS = 2; // Total number of blocks
    // private const int TRIALS_PER_BLOCK = 9; // Number of trials in each block
    private const int INITIAL_TRIALS_PER_BLOCK = 200; // Initial allocation, will grow as needed
    // private const int TOTAL_TRIALS = TOTAL_BLOCKS * TRIALS_PER_BLOCK; // Calculated total trials
    // private const int PRACTICE_TRIALS = 6;
    private const float TRIAL_DURATION = 5f; // Duration of each trial in seconds
    private const int REWARD_VALUE = 10; // Value of the reward for each trial
    private const int SKIP_REWARD_VALUE = 0; // Value awarded for skipping
    private const int NO_DECISION_REWARD_VALUE = 0; // Value for no decision (explicit for clarity)
    private const float SKIP_DELAY = 3f; // Delay before showing the next trial after skipping
    private const float DECISION_TIMEOUT = 2.5f; // Time allowed for making a decision
    private const float NO_DECISION_PENALTY_DURATION = 5f; // Duration of no-decision penalty
    private const string PENALTY_SCENE = "TimePenalty";
    private const float MIN_WAIT_TIME = 1f;
    private const float MAX_WAIT_TIME = 5f;
    private const string WAITING_ROOM_SCENE = "GetReadyITI";

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
    }
    public BlockType currentBlockType;
    public BlockType[] randomizedBlockOrder;
    private bool hasInitialized = false;

    public readonly Dictionary<BlockType, float[]> blockTypeProbabilities = new Dictionary<BlockType, float[]>
    {
        { BlockType.HighLowRatio, new float[] { 0.5f, 0.333f, 0.167f } },  // 3:2:1 ratio
        { BlockType.LowHighRatio, new float[] { 0.167f, 0.333f, 0.5f } }   // 1:2:3 ratio
    };
    //     public readonly Dictionary<BlockType, float[]> correctedBlockTypeProbabilities = new Dictionary<BlockType, float[]>
    // {
    //     { BlockType.HighLowRatio, new float[] { 0.5f, 0.333f, 0.167f } },     // 3:2:1 ratio (correct)
    //     { BlockType.LowHighRatio, new float[] { 0.167f, 0.333f, 0.5f } }      // 1:2:3 ratio (correct)
    // };

    private Dictionary<int, Dictionary<int, int>> blockEffortCounts = new Dictionary<int, Dictionary<int, int>>();


    #endregion

    #region Serialized Fields
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level2Sprite;
    [SerializeField] private Sprite level3Sprite;
    [SerializeField] private Sprite currentTrialSprite;
    // [SerializeField] public Sprite[] levelSprites = new Sprite[3];
    // [SerializeField] private List<BlockConfig> blockConfigs;
    // [SerializeField] private float[] blockDistances = new float[3] { 3f, 5f, 7f };
    // [SerializeField] private string block1InstructionScene = "Block1_Instructions";
    // [SerializeField] private string block2InstructionScene = "Block2_Instructions";
    [SerializeField] private string decisionPhaseScene = "DecisionPhase";
    [SerializeField] private string gridWorldScene = "GridWorld";
    [SerializeField] private string restBreakScene = "RestBreak";
    // [SerializeField] private string endExperimentScene = "EndExperiment";
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
    private int currentBlockNumber = 0;
    private bool experimentStarted = false;
    private bool isTourCompleted = false;
    private bool isPractice = false;
    private float trialStartTime;
    private bool[] blockCompleted;  // Track which blocks have been completed
    private List<Vector2> rewardPositions = new List<Vector2>();
    private Vector2 lastPlayerPosition;
    private List<(float collisionTime, float movementDuration)> rewardCollectionTimings = new List<(float, float)>();
    public GridManager gridManager;
    public ScoreManager scoreManager;
    public LogManager logManager;
    private bool decisionMade = false;
    private float decisionStartTime;
    private Coroutine decisionTimeoutCoroutine;
    private bool isExperimentActive = true;
    private bool hasShownBlockInstructions = false;

    #endregion

    #region Tracking Fields
    private float blockStartTime;
    private float currentBlockRemainingTime;
    private bool isBlockActive = false;
    private bool isBlockTimeUp = false;
    private int trialsCompletedInCurrentBlock;
    #endregion

    #region Data Structures
    private class TrialData
    {
        public int TrialNumber { get; set; }
        public Vector2 PlayerStartPosition { get; set; }
        public Vector2 RewardPosition { get; set; }
        public int EffortLevel { get; set; }
        public string DecisionType { get; set; }
        public float StartTime { get; set; }
        public float CompletionTime { get; set; }
        public bool WasSuccessful { get; set; }
        public List<PositionUpdate> PlayerMovement { get; set; } = new List<PositionUpdate>();
    }

    private class PositionUpdate
    {
        public Vector2 Position { get; set; }
        public float TimeStamp { get; set; }
    }

    private class BlockMetrics
    {
        public int TotalTrials { get; set; }
        public int CompletedTrials { get; set; }
        public int SkippedTrials { get; set; }
        public Dictionary<int, int> TrialsByEffortLevel { get; set; } = new Dictionary<int, int>();
        public Dictionary<int, int> SuccessesByEffortLevel { get; set; } = new Dictionary<int, int>();
        public float ActualDuration { get; set; }
        public float AverageTrialDuration { get; set; }
    }
    #endregion


    #region Events
    // public event System.Action<bool> OnPracticeTrialsComplete;
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

    // Add method to toggle cursor visibility if needed later
    public void SetCursorVisibility(bool visible)
    {
        Cursor.visible = visible;
        Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
    }



    private bool trialStarted = false;  // Add this field to track if trial is started

    // Add Update method to check block time
    private void Update()
    {
        if (isBlockActive && !isBlockTimeUp)
        {
            currentBlockRemainingTime = BLOCK_DURATION - (Time.time - blockStartTime);

            // Log block status every 10 seconds
            if (Time.time % 10 < Time.deltaTime)
            {
                logManager.LogBlockTimeStatus(
                    currentBlockNumber,
                    currentBlockRemainingTime,
                    trialsCompletedInCurrentBlock
                );
            }

            // Check if block should end
            if (currentBlockRemainingTime <= 0)
            {
                isBlockTimeUp = true;
                EndCurrentBlock();
            }
            // Check if there's enough time for a new trial
            else if (currentBlockRemainingTime >= MIN_TRIAL_DURATION)
            {
                // Only start trial if one hasn't been started and no decision has been made
                if (!trialStarted && !decisionMade &&
                    SceneManager.GetActiveScene().name == decisionPhaseScene)
                {
                    StartTrial();
                }
            }
        }
    }

    private bool IsInGridWorld()
    {
        return SceneManager.GetActiveScene().name == gridWorldScene;
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
        trialStarted = true;  // Set flag indicating trial has started
        decisionMade = false;

        // currentTrialActive = true;
        int effortLevel = GetCurrentTrialEffortLevel();
        int requiredPresses = GetCurrentTrialEV();

        // logManager.LogTrialStart(
        //     currentTrialIndex,
        //     currentBlockNumber,
        //     effortLevel,
        //     requiredPresses,
        //     isPractice
        // );

        // Reset the decision type at the start of each trial
        CurrentDecisionType = string.Empty;

        // Start the decision phase
        decisionStartTime = Time.time;
        trialStartTime = Time.time;
        decisionTimeoutCoroutine = StartCoroutine(DecisionTimeout());
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
        InitializeBlockOrder();

        // Convert block order to list for logging
        List<int> blockOrderForLogging = randomizedBlockOrder
            .Select(b => (int)b)
            .ToList();

        if (logManager != null && !string.IsNullOrEmpty(logManager.LogFilePath))
        {
            logManager.LogExperimentSetup(
                blockOrderForLogging,
                TOTAL_BLOCKS,
                -1  // Indicate variable trials with fixed duration
            );
        }
        else
        {
            Debug.LogError("LogManager not properly initialized");
            return;
        }

        InitializeSpriteToEffortMap();
        InitializeTrials();

        scoreManager = FindAnyObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            GameObject scoreManagerObj = new GameObject("ScoreManager");
            scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
        }

        LogEffortLevels();
        LogPressesPerEffortLevel();
    }

    private void InitializeBlockOrder()
    {
        // Only initialize if not already done
        if (hasInitialized) return;

        randomizedBlockOrder = new BlockType[TOTAL_BLOCKS];
        randomizedBlockOrder[0] = BlockType.HighLowRatio;
        randomizedBlockOrder[1] = BlockType.LowHighRatio;
        ShuffleBlockOrder();

        hasInitialized = true;

        // Log the initialized order
        Debug.Log($"Block order initialized: Block 1: {randomizedBlockOrder[0]}, Block 2: {randomizedBlockOrder[1]}");
    }

    private void ShuffleBlockOrder()
    {
        // Fisher-Yates shuffle
        System.Random rng = new System.Random();
        for (int i = randomizedBlockOrder.Length - 1; i > 0; i--)
        {
            int j = rng.Next(0, i + 1);
            BlockType temp = randomizedBlockOrder[i];
            randomizedBlockOrder[i] = randomizedBlockOrder[j];
            randomizedBlockOrder[j] = temp;
        }
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

    // Add these validation methods
    // Update ValidateTrialAccess to include more detailed logging
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
    // Update InitializeTrials to use the new INITIAL_TRIALS_PER_BLOCK value
    private void InitializeTrials()
    {
        if (gridManager == null)
        {
            gridManager = FindAnyObjectByType<GridManager>();
            if (gridManager == null)
            {
                Debug.LogError("GridManager not found! Cannot initialize trials.");
                return;
            }
        }

        // Initialize trials list with larger initial capacity
        trials = new List<Trial>(TOTAL_BLOCKS * INITIAL_TRIALS_PER_BLOCK);

        // Generate initial set of trials for each block
        for (int blockIndex = 0; blockIndex < TOTAL_BLOCKS; blockIndex++)
        {
            // gridManager.ResetAvailablePositions(); 

            for (int i = 0; i < INITIAL_TRIALS_PER_BLOCK; i++)
            {
                try
                {
                    gridManager.EnsureInitialization();
                    Vector2 playerSpawnPosition = gridManager.GetRandomAvailablePosition();
                    Vector2 rewardPosition = gridManager.GetPositionAtDistance(playerSpawnPosition, 5);

                    List<int> effortLevels = GetEffortLevelsForBlock(blockIndex);
                    int effortLevel = effortLevels[0];
                    Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

                    Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockIndex);
                    trials.Add(trial);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error generating trial {i} in block {blockIndex}: {e.Message}");
                }
            }
            Debug.Log($"Generated initial {INITIAL_TRIALS_PER_BLOCK} trials for block {blockIndex + 1}");
        }

        // Add method to generate more trials when needed
        EnsureEnoughTrials();
    }

    // Add method to ensure enough trials are available
    // Modify the EnsureEnoughTrials method to be more aggressive about generating new trials
    private void EnsureEnoughTrials()
    {
        int currentBlock = currentBlockNumber;
        int trialsInCurrentBlock = trials.Count(t => t.BlockIndex == currentBlock);
        int minimumTrialsAhead = 10; // Keep at least 10 trials ahead

        if (trialsInCurrentBlock - currentTrialIndex < minimumTrialsAhead)
        {
            int trialsToAdd = INITIAL_TRIALS_PER_BLOCK / 2; // Add 100 trials at a time
            Debug.Log($"Generating {trialsToAdd} additional trials for block {currentBlock + 1}");

            for (int i = 0; i < trialsToAdd; i++)
            {
                try
                {
                    gridManager.EnsureInitialization();
                    Vector2 playerSpawnPosition = gridManager.GetRandomAvailablePosition();
                    Vector2 rewardPosition = gridManager.GetPositionAtDistance(playerSpawnPosition, 5);

                    List<int> effortLevels = GetEffortLevelsForBlock(currentBlock);
                    int effortLevel = effortLevels[0];
                    Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

                    Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, currentBlock, currentBlock);
                    trials.Add(trial);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error generating additional trial: {e.Message}");
                }
            }
            Debug.Log($"Total trials after addition: {trials.Count}");
        }
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
        // Debug.Log($"Scene loaded: {scene.name}");

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

    #endregion


    #region Experiment Control Methods
    /// <summary>
    /// Block Generation: The GenerateBlockTrials() method is called 
    /// with both the original block index and its randomized order:
    /// </summary>
    /// <param name="blockIndex"></param>
    /// <param name="blockOrder"></param>
    /// <returns></returns>
    // Update GenerateBlockTrials to use time-based logic
    private List<Trial> GenerateBlockTrials(int blockIndex, int blockOrder)
    {
        List<Trial> blockTrials = new List<Trial>();
        List<int> effortLevels = GetEffortLevelsForBlock(blockIndex);

        if (gridManager == null)
        {
            Debug.LogError("GridManager is null in GenerateBlockTrials!");
            return blockTrials;
        }

        try
        {
            gridManager.EnsureInitialization();
            Vector2 playerSpawnPosition = gridManager.GetRandomAvailablePosition();
            Vector2 rewardPosition = gridManager.GetPositionAtDistance(playerSpawnPosition, 5);

            int effortLevel = effortLevels[0];
            Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

            Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockOrder);
            blockTrials.Add(trial);

            Debug.Log($"Created trial in block {blockIndex + 1} with effort level {effortLevel}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating trial: {e.Message}");
        }

        return blockTrials;
    }

    /// <summary>
    /// Returns a list of effort levels for a specific block.
    /// </summary>

    // Update GetEffortLevelsForBlock to use probability distribution
    private List<int> GetEffortLevelsForBlock(int blockIndex)
    {
        BlockType blockType = randomizedBlockOrder[blockIndex];
        float[] probabilities = blockTypeProbabilities[blockType];
        List<int> effortLevels = new List<int>();

        float random = Random.value;
        float cumulativeProbability = 0f;

        // Select effort level based on cumulative probability
        for (int i = 0; i < probabilities.Length; i++)
        {
            cumulativeProbability += probabilities[i];
            if (random <= cumulativeProbability)
            {
                effortLevels.Add(i + 1); // Convert to 1-based index
                break;
            }
        }

        // Fallback should rarely be needed with proper probabilities
        if (effortLevels.Count == 0)
        {
            effortLevels.Add(1);
        }

        return effortLevels;
    }

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
            blockStartTime = Time.time;
            isBlockActive = true;  // Ensure block is active at experiment start
            ScoreManager.Instance.ResetScore();

            logManager.LogExperimentStart(false);

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

        // float decisionTime = Time.time - decisionStartTime;
        string decisionType = workDecision ? "Work" : "Skip";
        CurrentDecisionType = decisionType;

        // Log decision phase metrics
        // logManager.LogDecisionPhaseStart(currentTrialIndex);
        // logManager.LogDecisionMade(
        //     trialNumber: currentTrialIndex,  // Pass the trial number
        //     decisionType: decisionType,      // Pass the decision type
        //     decisionTime: decisionReactionTime  // Pass the reaction time
        // );
        logManager.LogDecisionMetrics(currentTrialIndex, currentBlockNumber, decisionType, decisionTime);

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

        // Log the decision outcome
    logManager.LogDecisionOutcome(
        currentTrialIndex,
        currentBlockNumber,
        decisionType,
        false, // rewardCollected will be updated in the movement phase
        decisionTime,
        0, // movementTime will be updated in the movement phase
        0, // buttonPresses will be updated in the movement phase
        GetCurrentTrialEffortLevel(),
        GetCurrentTrialEV()
    );
    }

    private IEnumerator HandleSkipPenalty(float decisionTime)
    {
        Debug.Log("HandleSkipPenalty started");
        float penaltyStartTime = Time.time;

        logManager.LogPenaltyStart(currentTrialIndex, "Skip", SKIP_DELAY);
        // logManager.LogSkipDecision(currentTrialIndex, "Skip", Time.time - decisionStartTime);
        logManager.LogSkipDecision(currentTrialIndex, decisionTime);

        yield return new WaitForSeconds(SKIP_DELAY);

        // logManager.LogPenaltyEnd(currentTrialIndex, "Skip");
        float totalDuration = Time.time - penaltyStartTime;

        // Log trial outcome for skip
        int effortLevel = GetCurrentTrialEffortLevel();
        logManager.LogTrialOutcome(currentTrialIndex, currentBlockNumber);

        // Process skip as a completed trial
        ProcessTrialCompletion(false, totalDuration);
        Debug.Log("HandleSkipPenalty completed");
    }

    private void HandleNoDecision()
    {
        if (decisionMade) return;
        decisionMade = true;

        Debug.Log("No decision made - applying 5s penalty");
        CurrentDecisionType = "NoDecision";

        // Stop the timeout coroutine if it's still running
        if (decisionTimeoutCoroutine != null)
        {
            StopCoroutine(decisionTimeoutCoroutine);
            decisionTimeoutCoroutine = null;
        }

        StartCoroutine(HandleNoDecisionPenalty());
    }

    // Update HandleNoDecisionPenalty to handle time-based progression
    private IEnumerator HandleNoDecisionPenalty()
    {
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

        // logManager.LogPenaltyEnd(currentTrialIndex, "NoDecision");
        float totalDuration = Time.time - penaltyStartTime;

        // Process no-decision as a completed trial
        ProcessTrialCompletion(false, totalDuration);
    }

    // Update the ProcessTrialCompletion method to handle skip decisions correctly

    private void ProcessTrialCompletion(bool rewardCollected, float trialDuration)
    {
        if (!isExperimentActive) return;

        // Always increment completed trials counter
        trialsCompletedInCurrentBlock++;

        float remainingTime = BLOCK_DURATION - (Time.time - blockStartTime);
        int effortLevel = GetCurrentTrialEffortLevel();

        // Log various metrics
        logManager.LogBlockTimeStatus(
            currentBlockNumber,
            remainingTime,
            trialsCompletedInCurrentBlock
        );

        logManager.LogTrialOutcome(currentTrialIndex, currentBlockNumber);

        // logManager.LogTrialOutcome(
        //     currentTrialIndex,
        //     currentBlockNumber,
        //     CurrentDecisionType,
        //     rewardCollected,
        //     trialDuration,
        //     effortLevel
        // );

        logManager.LogTrialEnd(
            currentTrialIndex,
            rewardCollected,
            trialDuration,
            remainingTime
        );

        // Check if we should end the current block
        if (remainingTime <= MIN_TRIAL_DURATION)
        {
            Debug.Log($"Block {currentBlockNumber + 1} ending due to time limit. Remaining time: {remainingTime}");
            EndCurrentBlock();
        }
        else
        {
            // Move to next trial before loading decision phase
            MoveToNextTrial();
        }

        // Reset decision type for next trial
        CurrentDecisionType = string.Empty;
    }

    public void MoveToNextTrial()
    {
        if (!isExperimentActive || !HasTimeForNewTrial())
        {
            EndCurrentBlock();
            return;
        }

        // Increment trial index
        currentTrialIndex++;

        SetupNewTrial();
        LoadScene(decisionPhaseScene);
    }


    // Add method to check block time
    private bool IsBlockTimeExceeded()
    {
        return Time.time - blockStartTime >= BLOCK_DURATION;
    }

    // Add new method for decision timeout
    private IEnumerator DecisionTimeout()
    {
        yield return new WaitForSeconds(DECISION_TIMEOUT);

        if (!decisionMade && gameObject != null && isActiveAndEnabled)
        {
            HandleNoDecision();
        }
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

        EnsureEnoughTrials();

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
        TrackEffortLevel(currentBlockNumber, effortLevel);

        int pressesRequired = GetCurrentTrialEV();

        // logManager.LogTrialStart(
        //     currentTrialIndex,
        //     currentBlockNumber,
        //     effortLevel,
        //     pressesRequired,
        //     isPractice
        // );
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
        Debug.Log($"Continuing after break. Current block: {currentBlockNumber + 1}");

        if (currentBlockNumber >= TOTAL_BLOCKS)
        {
            EndExperiment();
            return;
        }

        // Reset block-specific variables
        isBlockActive = false;
        isBlockTimeUp = false;
        trialsCompletedInCurrentBlock = 0;
        hasShownBlockInstructions = false;

        // Load block instructions
        LoadScene("Block_Instructions");
    }

    private void ShowBlockInstructions()
    {
        Debug.Log($"Showing instructions for block {currentBlockNumber + 1}, type: {currentBlockType}");
        LoadScene("Block_Instructions");
    }

    // Revise ContinueAfterInstructions for clearer flow
    public void ContinueAfterInstructions()
    {
        Debug.Log($"Continuing after instructions for block {currentBlockNumber + 1}");

        // Ensure block is active before setting up trial
        if (!isBlockActive)
        {
            Debug.Log("Block was not active - starting new block");
            StartNewBlock();
        }

        SetupNewTrial();
        LoadScene(decisionPhaseScene);
    }

    /// <summary>
    /// Starts a new block of trials.
    /// </summary>
    private void StartNewBlock()
    {
        if (currentBlockNumber >= TOTAL_BLOCKS)
        {
            EndExperiment();
            return;
        }

        blockStartTime = Time.time;
        currentBlockRemainingTime = BLOCK_DURATION;
        isBlockActive = true;
        isBlockTimeUp = false;
        currentBlockType = randomizedBlockOrder[currentBlockNumber];

        Debug.Log($"Starting block {currentBlockNumber + 1} of type {currentBlockType}");
        logManager.LogBlockStart(currentBlockNumber);

        if (!hasShownBlockInstructions)
        {
            ShowBlockInstructions();
            hasShownBlockInstructions = true;
        }
        else
        {
            SetupNewTrial();
            LoadScene(decisionPhaseScene);
        }
    }

    /// <summary>
    /// Ends the current block of trials.
    /// </summary>
    // Update EndCurrentBlock to properly handle block transitions
    public void EndCurrentBlock()
    {
        float actualBlockDuration = Time.time - blockStartTime;
        isBlockActive = false;
        isBlockTimeUp = true;

        logManager.LogBlockEnd(currentBlockNumber);

        Debug.Log($"EndCurrentBlock: Current block {currentBlockNumber + 1} ended");

        // Check if this is the last block
        if (currentBlockNumber >= TOTAL_BLOCKS - 1)
        {
            Debug.Log("All blocks completed, ending experiment");
            EndExperiment(); // Directly end the experiment after the last block
        }
        else
        {
            // Increment block number and load rest break for the next block
            currentBlockNumber++;
            Debug.Log($"Moving to block {currentBlockNumber + 1}, type: {randomizedBlockOrder[currentBlockNumber]}");
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
        SceneManager.LoadScene("TirednessRating");
        // SceneManager.LoadScene(endExperimentScene);
    }

    #endregion

    #region Logging Methods

    private void LogEffortLevels()
    {
        if (trials == null)
        {
            Debug.LogError("Cannot log effort levels - trials list is null");
            return;
        }

        if (spriteToEffortMap == null)
        {
            Debug.LogError("Cannot log effort levels - spriteToEffortMap is null");
            return;
        }

        StringBuilder logBuilder = new StringBuilder();
        logBuilder.AppendLine("Initial Effort Levels Distribution:");

        for (int blockIndex = 0; blockIndex < TOTAL_BLOCKS; blockIndex++)
        {
            logBuilder.AppendLine($"\nBlock {blockIndex + 1} (Type: {randomizedBlockOrder[blockIndex]}):");

            var blockTrials = trials.Where(t => t.BlockIndex == blockIndex).ToList();
            var effortCounts = new int[3];

            foreach (var trial in blockTrials)
            {
                if (trial?.EffortSprite != null && spriteToEffortMap.ContainsKey(trial.EffortSprite))
                {
                    int effortLevel = spriteToEffortMap[trial.EffortSprite];
                    effortCounts[effortLevel]++;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                float percentage = blockTrials.Count > 0 ?
                    (float)effortCounts[i] / blockTrials.Count * 100 : 0;
                logBuilder.AppendLine($"Level {i + 1}: {effortCounts[i]} trials ({percentage:F1}%)");
            }
        }

        Debug.Log(logBuilder.ToString());
    }


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

    // public int GetTotalTrials() => TOTAL_TRIALS;
    public int GetTotalBlocks() => TOTAL_BLOCKS;
    public float GetBlockDuration() => BLOCK_DURATION;
    public BlockType GetNextBlockType() =>
        currentBlockNumber <= TOTAL_BLOCKS ? randomizedBlockOrder[currentBlockNumber] : randomizedBlockOrder[0];


    public BlockType GetCurrentBlockType()
    {
        if (currentBlockNumber > TOTAL_BLOCKS)
        {
            Debug.LogError("[ExperimentManager] Block index out of range!");
            return BlockType.HighLowRatio;
        }

        BlockType type = randomizedBlockOrder[currentBlockNumber];
        // Debug.Log($"[ExperimentManager] Current block type: {type} for block {currentBlockNumber + 1}");
        return type;
    }

    public bool IsCurrentTrialPractice() => isPractice;
    // public int GetCurrentBlockNumber() => currentBlockNumber;

    // Update display logic in GetCurrentBlockNumber to use 1-based indexing for display
    // public int GetCurrentBlockNumber() => currentBlockNumber + 1; // Convert 0-based to 1-based indexing for display
    // public int GetCurrentBlockNumber() => currentBlockNumber;
    public int GetCurrentBlockNumber()
    {
        // Ensure the block number does not exceed the total number of blocks
        // return Mathf.Min(currentBlockNumber + 1, TOTAL_BLOCKS);
        return Mathf.Min(currentBlockNumber, TOTAL_BLOCKS);
    }

    public int GetCurrentTrialIndex() => currentTrialIndex;
    // public int GetCurrentTrialInBlock() => currentTrialIndex % TRIALS_PER_BLOCK + 1;
    // public int GetTotalTrialsInBlock() => TRIALS_PER_BLOCK;
    // public Sprite GetCurrentTrialSprite() => trials[currentTrialIndex].EffortSprite;
    public Sprite GetStoredTrialSprite(Sprite sprite) => currentTrialSprite;
    public Vector2 GetCurrentTrialPlayerPosition() => trials[currentTrialIndex].PlayerPosition;
    public Vector2 GetCurrentTrialRewardPosition() => trials[currentTrialIndex].RewardPosition;
    public void StoreCurrentTrialSprite(Sprite sprite)
    {
        currentTrialSprite = sprite;
        Debug.Log($"Stored trial sprite: {sprite?.name ?? "NULL"}");
    }

    private Vector2 GetPlayerPosition()
    {
        var player = GameObject.FindWithTag("Player");
        return player != null ? new Vector2(player.transform.position.x, player.transform.position.y) : Vector2.zero;
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