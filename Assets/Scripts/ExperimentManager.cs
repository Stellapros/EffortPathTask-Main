using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;


/// <summary>
/// Manages the overall flow of the experiment, including trial generation, scene transitions, and data logging.
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    #region Singleton
    public static ExperimentManager Instance { get; private set; }
    #endregion

    #region Constants
    private const int TOTAL_TRIALS = 6; // Total number of trials in the experiment
    private const int PRACTICE_TRIALS = 6;
    private const int TRIALS_PER_BLOCK = 1; // Number of trials in each block
    private const int TOTAL_BLOCKS = 4; // Total number of blocks
    private const float TRIAL_DURATION = 5f; // Duration of each trial in seconds
    private const int REWARD_VALUE = 10; // Value of the reward for each trial
    private const float SKIP_DELAY = 3f; // Delay before showing the next trial after skipping
    // private const float GRID_CELL_SIZE = 1f; // Size of each grid cell
    #endregion

    #region Serialized Fields
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level2Sprite;
    [SerializeField] private Sprite level3Sprite;
    [SerializeField] private Sprite currentTrialSprite;
    // [SerializeField] public Sprite[] levelSprites = new Sprite[3];
    [SerializeField] private List<BlockConfig> blockConfigs;
    // [SerializeField] private float[] blockDistances = new float[3] { 3f, 5f, 7f };
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
    private int practiceTrialIndex = 0;
    private int currentBlockNumber = 0;
    private bool experimentStarted = false;
    private bool isTourCompleted = false;
    private bool isPractice = true;
    private float decisionStartTime;
    private float trialStartTime;
    private List<Vector2> rewardPositions = new List<Vector2>();
    private List<(float collisionTime, float movementDuration)> rewardCollectionTimings = new List<(float, float)>();
    public RewardSpawner rewardSpawner;
    public GridManager gridManager;
    public ScoreManager scoreManager;
    public LogManager logManager;
    #endregion

    #region Events
    public event System.Action<bool> OnPracticeTrialsComplete;
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

    /// <summary>
    /// Initializes all trials for the experiment & Block Randomization
    /// </summary>
    private void InitializeTrials()
    {
        List<int> blockOrder = new List<int> { 0, 1, 2 };
        blockOrder = blockOrder.OrderBy(x => Random.value).ToList(); // Randomize block order

        trials = new List<Trial>();
        for (int i = 0; i < TOTAL_BLOCKS; i++)
        {
            int blockIndex = blockOrder[i];
            List<Trial> blockTrials = GenerateBlockTrials(blockIndex, i);
            trials.AddRange(blockTrials);

            // Debug.Log($"Generated Block {i + 1} (Original Index: {blockIndex}): {blockTrials.Count} trials, Distance: {blockDistances[blockIndex]}");
            Debug.Log($"Generated Block {i + 1} (Original Index: {blockIndex}): {blockTrials.Count} trials");

            Debug.Log($"Total trials generated: {trials.Count}");
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

    // Add new field to prevent multiple transitions
    private bool isTransitioning = false;
    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadScene(sceneName);
        isTransitioning = false;
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
        Debug.Log($"Button clicked: {buttonName} in scene: {currentSceneConfig.sceneName}");

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
    private void SetupGridWorldPhase()
    {
        Debug.Log("Setting up Grid World Phase");
        logManager.LogGridWorldPhaseStart(currentTrialIndex);
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
    // private List<Trial> GenerateBlockTrials(int blockIndex, int blockOrder)
    // {
    //     List<Trial> blockTrials = new List<Trial>();
    //     List<int> effortLevels = GetEffortLevelsForBlock(blockIndex);

    //     for (int i = 0; i < TRIALS_PER_BLOCK; i++)
    //     {
    //         Vector2 playerSpawnPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
    //         Vector2 rewardPosition = new Vector2(Random.Range(-6f, 6f), Random.Range(-6f, 6f));

    //         // Generate random player position within grid boundaries
    //         // Vector2 playerSpawnPosition = new Vector2(
    //         //     Random.Range(-4f, 4f), // Leave space for reward
    //         //     Random.Range(-4f, 4f)
    //         // );

    //         // Generate reward position exactly 5 cells away
    //         // Vector2 rewardPosition = gridManager.GetPositionAtDistance(playerSpawnPosition, 5f);

    //         int effortLevel = effortLevels[i];
    //         Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

    //         // Create a trial with additional block information
    //         // Trial Creation: When creating each trial, 
    //         // both the original block index and its order in the experiment are recorded:
    //         Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockOrder);
    //         blockTrials.Add(trial);
    //     }

    //     return blockTrials.OrderBy(x => Random.value).ToList(); // Shuffle trials within the block
    // }

// private List<Trial> GenerateBlockTrials(int blockIndex, int blockOrder)
// {
//     List<Trial> blockTrials = new List<Trial>();
//     List<int> effortLevels = GetEffortLevelsForBlock(blockIndex);

//     for (int i = 0; i < TRIALS_PER_BLOCK; i++)
//     {
//         // Spawn player at random position using GridManager
//         Vector2 playerSpawnPosition = gridManager.GetRandomAvailablePosition();

//         // Get reward position exactly 5 cells away
//         Vector2 rewardPosition = gridManager.GetPositionAtDistance(playerSpawnPosition, 5f);

//         int effortLevel = effortLevels[i];
//         Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

//         Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockOrder);
//         blockTrials.Add(trial);
//     }

//     return blockTrials.OrderBy(x => Random.value).ToList(); // Shuffle trials within the block
// }

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

            int effortLevel = effortLevels[i];
            Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

            Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockOrder);
            blockTrials.Add(trial);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error generating trial {i}: {e.Message}");
        }
    }

    return blockTrials.OrderBy(x => Random.value).ToList();
}

    /// <summary>
    /// Returns a list of effort levels for a specific block.
    /// </summary>
    private List<int> GetEffortLevelsForBlock(int blockIndex)
    {
        List<int> effortLevels = new List<int>();
        switch (blockIndex)
        {
            case 0: // Block 1: 3:2:1 ratio
                effortLevels.AddRange(Enumerable.Repeat(3, 3)); // 3 trials with effort level 3
                effortLevels.AddRange(Enumerable.Repeat(2, 2)); // 2 trials with effort level 2
                effortLevels.AddRange(Enumerable.Repeat(1, 1)); // 1 trial with effort level 1
                break;
            case 1: // Block 2: 1:1:1 ratio
                effortLevels.AddRange(Enumerable.Repeat(3, 2)); // 2 trials each with effort levels 3, 2, and 1
                effortLevels.AddRange(Enumerable.Repeat(2, 2));
                effortLevels.AddRange(Enumerable.Repeat(1, 2));
                break;
            case 2: // Block 3: 1:2:3 ratio
                effortLevels.AddRange(Enumerable.Repeat(3, 1)); // 1 trial with effort level 3
                effortLevels.AddRange(Enumerable.Repeat(2, 2)); // 2 trials with effort level 2
                effortLevels.AddRange(Enumerable.Repeat(1, 3)); // 3 trials with effort level 1
                break;
        }
        return effortLevels.OrderBy(x => Random.value).ToList(); // Shuffle effort levels within the block
    }

    private Sprite GetSpriteForEffortLevel(int effortLevel)
    {
        switch (effortLevel)
        {
            case 1: return level1Sprite;
            case 2: return level2Sprite;
            case 3: return level3Sprite;
            default: return level1Sprite;
        }
    }

    /// <summary>
    /// Initializes the mapping between sprites and effort levels.
    /// </summary>
    private void InitializeSpriteToEffortMap()
    {
        spriteToEffortMap = new Dictionary<Sprite, int>
    {
        { level1Sprite, 0 }, // Easy 
        { level2Sprite, 1 }, // Medium
        { level3Sprite, 2 }  // Hard
    };
        Debug.Log($"Initialized spriteToEffortMap: Easy={spriteToEffortMap[level1Sprite]}, Medium={spriteToEffortMap[level2Sprite]}, Hard={spriteToEffortMap[level3Sprite]}");
        LogPressesPerEffortLevel();
    }

    /// <summary>
    /// Starts the experiment by transitioning to the DecisionPhase scene.
    /// </summary>
    public void StartExperiment()
    {
        if (!experimentStarted)
        {
            Debug.Log("Starting experiment with practice trials");
            experimentStarted = true;
            // isPractice = true;
            // practiceTrialIndex = 0;
            currentTrialIndex = 0;
            currentBlockNumber = 0;

            // Check if ScoreManager exists, if not create it
            if (ScoreManager.Instance == null)
            {
                Debug.LogWarning("ScoreManager instance not found. Creating new instance.");
                GameObject scoreManagerObj = new GameObject("ScoreManager");
                scoreManager = scoreManagerObj.AddComponent<ScoreManager>();
            }
            ScoreManager.Instance.ResetScore();

            // Check if LogManager exists, if not create it
            if (logManager == null)
            {
                Debug.LogWarning("LogManager is null in StartExperiment. Attempting to find or create instance.");
                logManager = FindAnyObjectByType<LogManager>();
                if (logManager == null)
                {
                    GameObject logManagerObj = new GameObject("LogManager");
                    logManager = logManagerObj.AddComponent<LogManager>();
                }
            }

            try
            {
                logManager.LogExperimentStart(true); // Pass true for practice trials

                // Add explicit logging for first trial setup
                Debug.Log("Initializing first trial setup");
                SetupNewTrial(); // Add this line to ensure first trial is properly set up
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to log experiment start: {e.Message}");
            }

            // SetupPracticeTrial();
            LoadScene(decisionPhaseScene);
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

    // private void SetupPracticeTrial()
    // {
    //     Debug.Log($"Setting up practice trial {practiceTrialIndex + 1}");
    //     // Randomize effort level for practice trials
    //     int effortLevel = Random.Range(1, 4);
    //     int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 10);

    //     // Log practice trial start
    //     logManager.LogTrialStart(practiceTrialIndex, 0, 3f, effortLevel, pressesRequired, true);
    // }

    private void StartPracticeSequence()
    {
        Debug.Log("Starting practice sequence");
        isPractice = true;
        practiceTrialIndex = 0;

        // Load GetReadyPractise scene
        LoadScene("GetReadyPractise");
    }

    private void LogFirstTrial()
    {
        // float currentBlockDistance = GetCurrentBlockDistance();
        // float currentBlockDistance = 5f;
        int effortLevel = GetCurrentTrialEffortLevel();
        int pressesRequired = GetCurrentTrialEV();
        // logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, currentBlockDistance, effortLevel, pressesRequired, isPractice);
        logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, effortLevel, pressesRequired, isPractice);
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    public void HandleDecision(bool workDecision)
    {
        if (logManager == null)
        {
            Debug.LogError("LogManager is null in HandleDecision method!");
            logManager = FindAnyObjectByType<LogManager>();
            if (logManager == null)
            {
                Debug.LogError("Failed to find LogManager in the scene!");
                return;
            }
        }

        Trial currentTrial = trials[currentTrialIndex];
        float decisionReactionTime = Time.time - decisionStartTime;
        int pressesRequired = GetCurrentTrialEV();

        // Add additional logging
        Debug.Log($"Handling decision for trial {currentTrialIndex}: Decision={workDecision}, Presses Required={pressesRequired}");

        logManager.LogDecisionMade(currentTrialIndex, workDecision ? "Work" : "Skip");

        if (workDecision)
        {
            Debug.Log($"Player decided to work on trial {currentTrialIndex + 1}. Loading GridWorld scene after delay.");
            // Prevent multiple scene loads
            if (!isTransitioning)
            {
                isTransitioning = true;
                StartCoroutine(LoadSceneAfterDelay(gridWorldScene, 0.5f));
            }
        }
        else
        {
            Debug.Log($"Player decided to skip trial {currentTrialIndex + 1}. Moving to next trial after delay.");
            if (!isTransitioning)
            {
                isTransitioning = true;
                StartCoroutine(ShowNextTrialAfterDelay());
            }
        }
    }

    /// <summary>
    /// Coroutine to show the next trial after a delay when skipping.
    /// </summary>
    private IEnumerator ShowNextTrialAfterDelay()
    {
        yield return new WaitForSeconds(SKIP_DELAY);
        // if (isPractice)
        // {
        //     MoveToPracticeOrFormalExperiment();
        // }
        // else 
        if (currentTrialIndex < TOTAL_TRIALS - 1)
        {
            MoveToNextTrial();
        }
        else
        {
            EndExperiment();
        }
        isTransitioning = false;
    }

    // private void MoveToPracticeOrFormalExperiment()
    // {
    //     practiceTrialIndex++;
    //     if (practiceTrialIndex < PRACTICE_TRIALS)
    //     {
    //         SetupPracticeTrial();
    //         LoadScene(decisionPhaseScene);
    //     }
    //     else
    //     {
    //         isPractice = false;
    //         LoadScene("GetReadyFormal"); // Load a transition scene before starting formal experiment
    //     }
    // }

    public void StartFormalExperiment()
    {
        Debug.Log("Starting formal experiment");
        isPractice = false;
        currentTrialIndex = 0;
        currentBlockNumber = 0;
        ScoreManager.Instance.ResetScore();

        logManager.LogExperimentStart(false);
        LogFirstTrial();

        StartNewBlock();
        SetupNewTrial();
        LoadScene(decisionPhaseScene);
    }

    public void MoveToNextTrial()
    {
        currentTrialIndex++;
        if (currentTrialIndex >= TOTAL_TRIALS)
        {
            EndExperiment();
        }
        else
        {
            if (currentTrialIndex % TRIALS_PER_BLOCK == 0)
            {
                EndCurrentBlock();
                currentBlockNumber++;
                if (currentBlockNumber < TOTAL_BLOCKS)
                {
                    StartNewBlock();
                    LoadScene(restBreakScene);
                }
                else
                {
                    EndExperiment();
                }
            }
            else
            {
                SetupNewTrial();
                LoadScene(decisionPhaseScene);
            }
        }
    }

    /// <summary>
    /// Continues the experiment after a break between blocks.
    /// </summary>
    public void ContinueAfterBreak()
    {
        Debug.Log($"Continuing to Block {currentBlockNumber}");
        // StartNewBlock();
        SetupNewTrial();
        LoadScene(decisionPhaseScene);
    }

    /// <summary>
    /// Sets up the DecisionPhase scene.
    /// </summary>
    public void SetupDecisionPhase()
    {
        Debug.Log("Setting up Decision Phase");
        DecisionManager decisionManager = FindAnyObjectByType<DecisionManager>();
        if (decisionManager != null)
        {
            decisionManager.SetupDecisionPhase();
        }
        else
        {
            Debug.LogError("DecisionManager not found in the scene!");
        }

        decisionStartTime = Time.time;
        // SetupTrialForCurrentBlock(); // Add this line to ensure block info is set
        Debug.Log($"Decision phase started for trial {currentTrialIndex + 1}");
        logManager.LogDecisionPhaseStart(currentTrialIndex);
    }

    private void SetupNewTrial()
    {
        if (currentTrialIndex < TOTAL_TRIALS)
        {
            Debug.Log($"Setting up trial {currentTrialIndex} in block {currentBlockNumber}");
            // float currentBlockDistance = GetCurrentBlockDistance();

            // Validate trial data exists
            if (trials == null || currentTrialIndex >= trials.Count)
            {
                Debug.LogError($"Invalid trial setup: trials={trials?.Count ?? 0}, currentTrialIndex={currentTrialIndex}");
                return;
            }

            // Add validation for current trial
            Trial currentTrial = trials[currentTrialIndex];
            if (currentTrial == null)
            {
                Debug.LogError("Current trial is null!");
                return;
            }


            // float currentBlockDistance = 5f;
            int effortLevel = GetCurrentTrialEffortLevel();
            int pressesRequired = GetCurrentTrialEV();

            // Add additional logging
            Debug.Log($"Trial {currentTrialIndex} setup - Effort Level: {effortLevel}, Presses Required: {pressesRequired}");

            // Log trial start with additional validation
            if (logManager != null)
            {
                // logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, currentBlockDistance, effortLevel, pressesRequired, isPractice);
                logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, effortLevel, pressesRequired, isPractice);
            }
            else
            {
                Debug.LogError("LogManager is null during trial setup!");
            }
        }
        else
        {
            Debug.Log("All trials completed. Ending experiment.");
            EndExperiment();
        }
    }

    /// <summary>
    /// Ends the current trial and logs the result.
    /// </summary>
    public void EndTrial(bool rewardCollected)
    {
        if (logManager == null)
        {
            Debug.LogError("LogManager is null in EndTrial method!");
            return;
        }

        // Debug.Log($"Ending {(isPractice ? "practice " : "")}trial {(isPractice ? practiceTrialIndex : currentTrialIndex)}. Reward collected: {rewardCollected}");
        Debug.Log($"Ending trial {currentTrialIndex}. Reward collected: {rewardCollected}");

        float trialDuration = Time.time - trialStartTime;
        float actionReactionTime = GameController.Instance.GetActionReactionTime();

        if (rewardCollected)
        {
            // ScoreManager.Instance.AddScore(REWARD_VALUE, true); // Formal trial // Added in the GameController
        }

        OnTrialEnded?.Invoke(rewardCollected);
        MoveToNextTrial();
    }

    /// <summary>
    /// Starts a new block of trials.
    /// </summary>
    private void StartNewBlock()
    {
        // Debug.Log($"Starting Block {currentBlockNumber} with distance {GetCurrentBlockDistance()}");
        // logManager.LogBlockStart(currentBlockNumber);

        // Here, you might want to set any block-specific parameters
        // For example, setting the current block distance:
        // float currentBlockDistance = GetCurrentBlockDistance();
        // float currentBlockDistance = 5f;
        // Debug.Log($"Current block distance set to: {currentBlockDistance}");
    }

    /// <summary>
    /// Ends the current block of trials.
    /// </summary>
    private void EndCurrentBlock()
    {
        logManager.LogBlockEnd(currentBlockNumber);
        //     LogManager.instance.LogEvent("BlockEnd", new Dictionary<string, object>
        // {
        //     {"BlockNumber", currentBlockNumber + 1}
        // });
        Debug.Log($"Block {currentBlockNumber} completed.");

        // currentBlockNumber++;

        if (currentBlockNumber < TOTAL_BLOCKS)
        {
            Debug.Log($"Loading RestBreakScene after Block {currentBlockNumber}");
            LoadScene(restBreakScene);
        }
        else
        {
            EndExperiment();
        }
    }

    /// <summary>
    /// Ends the experiment and transitions to the EndExperiment scene.
    /// </summary>
    private void EndExperiment()
    {
        Debug.Log("All trials completed. Ending experiment.");
        LoadScene(endExperimentScene);
        BackgroundMusicManager.Instance.StopMusic();

        // Calculate and log total experiment time
        float totalTime = Time.time - PlayerPrefs.GetFloat("ExperimentStartTime", 0f);
        Debug.Log($"Total experiment time: {totalTime} seconds");

        // LogManager.instance.LogEvent("ExperimentEnd");
        // LogManager.instance.LogEvent("ExperimentEnd", new Dictionary<string, object>());
        logManager.LogExperimentEnd();

        // Log any remaining trials
        LogManager.Instance.DumpUnloggedTrials();
        LogManager.Instance.LogExperimentEnd();
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
    private void LogDecision(bool worked)
    {
        Debug.Log($"Trial {currentTrialIndex}: Decision - {(worked ? "Worked" : "Skipped")}, Effort Level: {GetCurrentTrialEV()}");
    }

    /// <summary>
    /// Logs the outcome of the current trial.
    /// </summary>
    private void LogTrialOutcome(bool rewardCollected)
    {
        Debug.Log($"Block {currentBlockNumber}, Trial {currentTrialIndex}: Outcome - {(rewardCollected ? "Reward Collected" : "Time Out")}, Effort Level: {GetCurrentTrialEV()}");
    }

    // Add this method to log presses per effort level
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
    public void LogTrialData(bool completed, float reactionTime, int buttonPresses)
    {
        Debug.Log($"Trial {currentTrialIndex}: Completed - {completed}, Reaction Time - {reactionTime}, Button Presses - {buttonPresses}, Effort Level: {GetCurrentTrialEV()}");
    }

    /// <summary>
    /// Logs the position of a spawned reward.
    /// </summary>
    public void LogRewardPosition(Vector2 position)
    {
        rewardPositions.Add(position);
        Debug.Log($"Logged reward position: {position}");
    }

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
    public void ClearLoggedData()
    {
        rewardPositions.Clear();
        rewardCollectionTimings.Clear();
        Debug.Log("Cleared all logged experiment data.");
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
    public int GetCurrentBlockNumber() => currentBlockNumber;
    public int GetCurrentTrialIndex() => currentTrialIndex;
    public int GetCurrentTrialInBlock() => currentTrialIndex % TRIALS_PER_BLOCK + 1;
    public int GetTotalTrialsInBlock() => TRIALS_PER_BLOCK;
    public Sprite GetCurrentTrialSprite() => trials[currentTrialIndex].EffortSprite;
    public Sprite GetStoredTrialSprite(Sprite sprite) => currentTrialSprite;
    public Vector2 GetCurrentTrialPlayerPosition() => trials[currentTrialIndex].PlayerPosition;
    public Vector2 GetCurrentTrialRewardPosition() => trials[currentTrialIndex].RewardPosition;
        public void StoreCurrentTrialSprite(Sprite sprite)
    {
        currentTrialSprite = sprite;
        Debug.Log($"Stored trial sprite: {sprite?.name ?? "NULL"}");
    }



//     public Vector2 GetCurrentTrialRewardPosition(Vector2 playerPosition)
// {
//     // Possible directions: Up, Down, Left, Right
//     Vector2[] directions = new Vector2[] 
//     { 
//         Vector2.up, Vector2.down, Vector2.left, Vector2.right 
//     };

//     // Randomly select a direction
//     Vector2 selectedDirection = directions[Random.Range(0, directions.Length)];

//     // Calculate reward position 5 cells away in the selected direction
//     Vector2 rewardPosition = playerPosition + (selectedDirection * 5);

//     return rewardPosition;
// }

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