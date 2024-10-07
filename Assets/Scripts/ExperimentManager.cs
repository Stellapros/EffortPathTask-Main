using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


/// <summary>
/// Manages the overall flow of the experiment, including trial generation, scene transitions, and data logging.
/// </summary>
public class ExperimentManager : MonoBehaviour
{
    #region Singleton
    public static ExperimentManager Instance { get; private set; }
    #endregion

    #region Constants
    private const int TOTAL_TRIALS = 3; // Total number of trials in the experiment
    private const int PRACTICE_TRIALS = 2;
    private const int TRIALS_PER_BLOCK = 1; // Number of trials in each block
    private const int TOTAL_BLOCKS = 3; // Total number of blocks
    private const float TRIAL_DURATION = 10f; // Duration of each trial in seconds
    private const int REWARD_VALUE = 10; // Value of the reward for each trial
    private const float SKIP_DELAY = 3f; // Delay before showing the next trial after skipping
    #endregion

    #region Serialized Fields
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level2Sprite;
    [SerializeField] private Sprite level3Sprite;
    [SerializeField] private List<BlockConfig> blockConfigs;
    [SerializeField] private float[] blockDistances = new float[3] { 3f, 5f, 7f };
    [SerializeField] private string decisionPhaseScene = "DecisionPhase";
    [SerializeField] private string gridWorldScene = "GridWorld";
    [SerializeField] private string restBreakScene = "RestBreak";
    [SerializeField] private string endExperimentScene = "EndExperiment";

    [System.Serializable]
    public class SceneTransition
    {
        public string fromScene;
        public string toScene;
        public string buttonName;
    }

    [SerializeField]
    private List<SceneTransition> sceneTransitions = new List<SceneTransition>();
    #endregion

    #region Private Fields
    private List<Trial> trials;
    private Dictionary<Sprite, int> spriteToEffortMap;
    private int currentTrialIndex = 0;
    private int practiceTrialIndex = 0;
    private int currentBlockNumber = 0;
    private bool experimentStarted = false;
    private bool isPractice = true;
    private float decisionStartTime;
    private float trialStartTime;
    private List<Vector2> rewardPositions = new List<Vector2>();
    private List<(float collisionTime, float movementDuration)> rewardCollectionTimings = new List<(float, float)>();
    public ScoreManager scoreManager;
    public LogManager logManager;
    #endregion

    #region Events
    public event System.Action<bool> OnTrialEnded;
    #endregion

    #region Unity Lifecycle Methods
    private void Awake()
    {
        // Implement the singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeComponents();

        }
        else
        {
            Destroy(gameObject);
        }

        logManager = FindObjectOfType<LogManager>();
        if (logManager == null)
        {
            Debug.LogError("LogManager not found in the scene!");
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Start the background music
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.PlayMusic();
        }
        else
        {
            Debug.LogWarning("BackgroundMusicManager not found.");
        }

        // Ensure that the total time is accurately calculated and displayed when the experiment ends
        PlayerPrefs.SetFloat("ExperimentStartTime", Time.time);

        if (logManager == null)
        {
            Debug.LogError("LogManager is still null in Start method!");
        }
        else
        {
            // LogManager.instance.LogEvent("ExperimentStart");
            logManager.LogExperimentStart(true);
            // LogManager.instance.LogEvent("ExperimentStart", new Dictionary<string, object>());
        }

        VerifyPlayerPrefs();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion

    #region Initialization Methods
    private void InitializeComponents()
    {
        InitializeTrials();
        InitializeSpriteToEffortMap();
        LogEffortLevels();
        LogPressesPerEffortLevel();

        // Initialize LogManager
        logManager = FindObjectOfType<LogManager>();
        if (logManager == null)
        {
            Debug.LogError("LogManager not found in the scene! Creating a new instance.");
            logManager = new GameObject("LogManager").AddComponent<LogManager>();
        }
        // Log experiment configuration
        // logManager.LogExperimentConfiguration(spriteToEffortMap);

        // Initialize ScoreManager
        scoreManager = FindObjectOfType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManager not found in the scene!");
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

            Debug.Log($"Generated Block {i + 1} (Original Index: {blockIndex}): {blockTrials.Count} trials, Distance: {blockDistances[blockIndex]}");
        }

        Debug.Log($"Total trials generated: {trials.Count}");
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

        for (int i = 0; i < TRIALS_PER_BLOCK; i++)
        {
            Vector2 playerSpawnPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            Vector2 rewardPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            int effortLevel = effortLevels[i];
            Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);

            // Create a trial with additional block information
            // Trial Creation: When creating each trial, 
            // both the original block index and its order in the experiment are recorded:
            Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockOrder);
            blockTrials.Add(trial);
        }

        return blockTrials.OrderBy(x => Random.value).ToList(); // Shuffle trials within the block
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

        // Log to a file for persistent record
        // string logEntry = $"{System.DateTime.Now}: Effort levels initialized - Easy: {spriteToEffortMap[level1Sprite]}, Medium: {spriteToEffortMap[level2Sprite]}, Hard: {spriteToEffortMap[level3Sprite]}";
        // System.IO.File.AppendAllText("experiment_log.txt", logEntry + System.Environment.NewLine);
        LogPressesPerEffortLevel();
    }

    #endregion

    #region Scene Management Methods
    /// <summary>
    /// Loads a new scene by name.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Debug.Log($"Loading scene: {sceneName}");
        SceneManager.LoadScene(sceneName);
        CleanupPlayer();
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
        SetupButtons(scene.name);

        if (scene.name == decisionPhaseScene && experimentStarted)
        {
            SetupDecisionPhase();
        }
        else if (scene.name == gridWorldScene)
        {
            // Ensure ScoreManager is available in GridWorld scene
            if (scoreManager == null)
            {
                scoreManager = FindObjectOfType<ScoreManager>();
                if (scoreManager == null)
                {
                    Debug.LogError("ScoreManager not found in GridWorld scene!");
                }
            }
            SetupGridWorldPhase();
        }
    }
    private void SetupGridWorldPhase()
    {
        Debug.Log("Setting up Grid World Phase");
        logManager.LogGridWorldPhaseStart(currentTrialIndex);
    }

    /// <summary>
    /// Sets up button listeners for the current scene.
    /// </summary>
    private void SetupButtons(string sceneName)
    {
        foreach (var transition in sceneTransitions)
        {
            if (transition.fromScene == sceneName)
            {
                GameObject buttonObj = GameObject.Find(transition.buttonName);
                if (buttonObj != null)
                {
                    UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();

                        // Special case for starting the formal experiment from GetReadyFormal scene
                        if (sceneName == "GetReadyFormal")
                        {
                            button.onClick.AddListener(StartFormalExperiment);
                        }
                        else
                        {
                            button.onClick.AddListener(() => LoadScene(transition.toScene));
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Button component not found on {transition.buttonName} in {sceneName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Button {transition.buttonName} not found in {sceneName}");
                }
            }
        }
    }
    #endregion

    #region Experiment Control Methods
    /// <summary>
    /// Starts the experiment by transitioning to the DecisionPhase scene.
    /// </summary>
    // public void StartExperiment()
    // {
    //     if (!experimentStarted)
    //     {
    //         Debug.Log("Starting experiment");
    //         experimentStarted = true;
    //         currentTrialIndex = 0;
    //         currentBlockNumber = 0;
    //         ScoreManager.Instance.ResetScore();

    //         // Log the start of the experiment and the first trial
    //         logManager.LogExperimentStart();
    //         LogFirstTrial();

    //         StartNewBlock();
    //         SetupNewTrial();
    //         Debug.Log($"Initial trial setup complete. Current trial index: {currentTrialIndex}");
    //         LoadScene(decisionPhaseScene);
    //     }
    // }
    public void StartExperiment()
    {
        if (!experimentStarted)
        {
            Debug.Log("Starting experiment with practice trials");
            experimentStarted = true;
            isPractice = true;
            practiceTrialIndex = 0;
            currentTrialIndex = 0;
            currentBlockNumber = 0;
            ScoreManager.Instance.ResetScore();

            logManager.LogExperimentStart(true); // Pass true for practice trials
            SetupPracticeTrial();
            LoadScene(decisionPhaseScene);
        }
    }
    private void SetupPracticeTrial()
    {
        Debug.Log($"Setting up practice trial {practiceTrialIndex + 1}");
        int effortLevel = Random.Range(1, 4);
        int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 10);
        logManager.LogTrialStart(practiceTrialIndex, 0, 3f, effortLevel, pressesRequired, true);
    }

    private void LogFirstTrial()
    {
        float currentBlockDistance = GetCurrentBlockDistance();
        int effortLevel = GetCurrentTrialEffortLevel();
        int pressesRequired = GetCurrentTrialEV();
        logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, currentBlockDistance, effortLevel, pressesRequired, isPractice);

    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    public void HandleDecision(bool workDecision)
    {
        if (isPractice)
        {
            HandlePracticeDecision(workDecision);
        }
        else
        {
            HandleFormalDecision(workDecision);
        }
    }

    private void HandlePracticeDecision(bool workDecision)
    {
        int effortLevel = Random.Range(1, 4);
        int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 10);
        logManager.LogDecisionMade(practiceTrialIndex, workDecision ? "Work" : "Skip");

        if (workDecision)
        {
            StartCoroutine(LoadSceneAfterDelay(gridWorldScene, 0.5f));
        }
        else
        {
            StartCoroutine(ShowNextTrialAfterDelay());
        }
    }
    public void HandleFormalDecision(bool workDecision)
    {
        if (logManager == null)
        {
            Debug.LogError("LogManager is null in HandleDecision method!");
            logManager = FindObjectOfType<LogManager>();
            if (logManager == null)
            {
                Debug.LogError("Failed to find LogManager in the scene!");
                return;
            }
        }
        Trial currentTrial = trials[currentTrialIndex];
        float decisionReactionTime = Time.time - decisionStartTime;
        int pressesRequired = GetCurrentTrialEV(); // Get the number of presses required
        logManager.LogDecisionMade(currentTrialIndex, workDecision ? "Work" : "Skip");

        if (workDecision)
        {
            Debug.Log($"Player decided to work on trial {currentTrialIndex + 1}. Loading GridWorld scene after delay.");
            StartCoroutine(LoadSceneAfterDelay(gridWorldScene, 0.5f));
        }
        else
        {
            Debug.Log($"Player decided to skip trial {currentTrialIndex + 1}. Moving to next trial after delay.");
            StartCoroutine(ShowNextTrialAfterDelay());
        }
    }


    private IEnumerator LoadSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        LoadScene(sceneName);
    }

    /// <summary>
    /// Coroutine to show the next trial after a delay when skipping.
    /// </summary>
    private IEnumerator ShowNextTrialAfterDelay()
    {
        yield return new WaitForSeconds(SKIP_DELAY);
        if (isPractice)
        {
            MoveToPracticeOrFormalExperiment();
        }
        else if (currentTrialIndex < TOTAL_TRIALS - 1)
        {
            MoveToNextTrial();
        }
        else
        {
            EndExperiment();
        }
    }

    private void MoveToPracticeOrFormalExperiment()
    {
        practiceTrialIndex++;
        if (practiceTrialIndex < PRACTICE_TRIALS)
        {
            SetupPracticeTrial();
            LoadScene(decisionPhaseScene);
        }
        else
        {
            isPractice = false;
            LoadScene("GetReadyFormal"); // Load the GetReady scene before starting the formal experiment
        }
    }

    /// <summary>
    /// Moves to the next trial or ends the experiment if all trials are completed.
    /// </summary>
    // public void MoveToNextTrial()
    // {
    //     if (currentTrialIndex < TOTAL_TRIALS)
    //     {
    //         Debug.Log($"Moving to trial {currentTrialIndex + 1}");
    //         LoadScene(decisionPhaseScene);
    //     }
    //     else
    //     {
    //         Debug.Log("All trials completed. Ending experiment.");
    //         EndExperiment();
    //     }
    // }
    //     public void MoveToNextTrial()
    // {
    //     currentTrialIndex++;
    //     if (currentTrialIndex % TRIALS_PER_BLOCK == 0)
    //     {
    //         EndCurrentBlock();
    //         currentBlockNumber++;
    //         if (currentBlockNumber < TOTAL_BLOCKS)
    //         {
    //             StartNewBlock();
    //             LoadScene(restBreakScene);
    //         }
    //         else
    //         {
    //             EndExperiment();
    //             return;
    //         }
    //     }
    //     SetupNewTrial();
    //     LoadScene(decisionPhaseScene);
    // }

    // public void MoveToNextTrial()
    // {
    //     currentTrialIndex++;
    //     if (currentTrialIndex >= TOTAL_TRIALS)
    //     {
    //         EndExperiment();
    //     }
    //     else
    //     {
    //         EndCurrentBlock();
    //         currentBlockNumber++;
    //         if (currentBlockNumber < TOTAL_BLOCKS)
    //         {
    //             StartNewBlock();
    //             LoadScene(restBreakScene);
    //         }
    //         else
    //         {
    //             EndExperiment();
    //         }
    //     }
    // }
    public void StartFormalExperiment()
    {
        isPractice = false;
        currentTrialIndex = 0;
        currentBlockNumber = 0;
        ScoreManager.Instance.ResetScore();

        logManager.LogExperimentStart(false); // Pass false for formal experiment
        LogFirstTrial();

        StartNewBlock();
        SetupNewTrial();
        Debug.Log("Starting formal experiment");
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

    // private void SetupTrialForCurrentBlock()
    // {
    //     float blockDistance = GetCurrentBlockDistance();
    //     logManager.LogTrialStart(currentTrialIndex, currentBlockNumber + 1, blockDistance);
    // }

    /// <summary>
    /// Sets up the DecisionPhase scene.
    /// </summary>
    public void SetupDecisionPhase()
    {
        Debug.Log("Setting up Decision Phase");
        DecisionManager decisionManager = FindObjectOfType<DecisionManager>();
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
            float currentBlockDistance = GetCurrentBlockDistance();
            int effortLevel = GetCurrentTrialEffortLevel();
            int pressesRequired = GetCurrentTrialEV();
            logManager.LogTrialStart(currentTrialIndex, currentBlockNumber, currentBlockDistance, effortLevel, pressesRequired, isPractice);
            // logManager.DumpTrialData(); // Add this line

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
        if (isPractice)
        {
            EndPracticeTrial(rewardCollected);
        }
        else
        {
            EndFormalTrial(rewardCollected);
        }
    }

    private void EndPracticeTrial(bool rewardCollected)
    {
        logManager.LogTrialEnd(practiceTrialIndex, rewardCollected ? "Collected" : "NotCollected");
        logManager.LogTrialEnd(practiceTrialIndex, rewardCollected ? "Collected" : "NotCollected");
        if (rewardCollected)
        {
            ScoreManager.Instance.AddScore(REWARD_VALUE, false); // Practice trial
        }

        practiceTrialIndex++;

        if (practiceTrialIndex < PRACTICE_TRIALS)
        {
            SetupPracticeTrial();
            LoadScene(decisionPhaseScene);
        }
        else
        {
            isPractice = false;
            LoadScene("GetReadyFormal");
        }
    }
    public void EndFormalTrial(bool rewardCollected)
    {
        if (logManager == null)
        {
            Debug.LogError("LogManager is null in EndTrial method!");
            return;
        }

        Debug.Log($"Ending {(isPractice ? "practice " : "")}trial {(isPractice ? practiceTrialIndex : currentTrialIndex)}. Reward collected: {rewardCollected}");
        float trialDuration = Time.time - trialStartTime;
        float actionReactionTime = GameController.Instance.GetActionReactionTime();

        if (isPractice)
        {
            // logManager.LogPracticeTrialEnd(practiceTrialIndex, rewardCollected ? "Collected" : "NotCollected");
            MoveToPracticeOrFormalExperiment(); // Add this line
        }
        else
        {
            logManager.LogTrialEnd(currentTrialIndex, rewardCollected ? "Collected" : "NotCollected");
            MoveToNextTrial();
        }

        if (rewardCollected)
        {
            ScoreManager.Instance.AddScore(REWARD_VALUE, true); // Formal trial
        }

        // logManager.DumpTrialData();

        OnTrialEnded?.Invoke(rewardCollected);
    }

    /// <summary>
    /// Starts a new block of trials.
    /// </summary>
    private void StartNewBlock()
    {
        Debug.Log($"Starting Block {currentBlockNumber} with distance {GetCurrentBlockDistance()}");
        logManager.LogBlockStart(currentBlockNumber);

        // Here, you might want to set any block-specific parameters
        // For example, setting the current block distance:
        float currentBlockDistance = GetCurrentBlockDistance();
        Debug.Log($"Current block distance set to: {currentBlockDistance}");
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


    // public int GetCurrentTrialEV()
    // {
    //     if (trials == null || currentTrialIndex >= trials.Count)
    //     {
    //         Debug.LogWarning($"Trials not initialized or currentTrialIndex ({currentTrialIndex}) out of range.");
    //         return 0;
    //     }

    //     Sprite currentSprite = trials[currentTrialIndex].EffortSprite;
    //     if (spriteToEffortMap == null || !spriteToEffortMap.ContainsKey(currentSprite))
    //     {
    //         Debug.LogWarning($"spriteToEffortMap not initialized or doesn't contain the current sprite.");
    //         return 0;
    //     }

    //     int effortLevel = spriteToEffortMap[currentSprite];
    //     int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel + 1}", 0);

    //     Debug.Log($"Current trial (index: {currentTrialIndex}) EV: {pressesRequired}, Sprite: {currentSprite.name}, Effort Level: {effortLevel + 1}");

    //     return pressesRequired;
    // }
    public float GetCurrentBlockDistance()
    {
        if (currentBlockNumber < 0 || currentBlockNumber >= blockDistances.Length)
        {
            Debug.LogError($"Invalid block number: {currentBlockNumber}. Using default distance of 5f.");
            return 5f; // Default fallback distance
        }
        return blockDistances[currentBlockNumber];
    }



    // Getter methods for various experiment parameters
    // public float GetCurrentBlockDistance() => blockDistances[currentBlockNumber];
    public int GetCurrentBlockNumber() => currentBlockNumber;
    public int GetCurrentTrialIndex() => currentTrialIndex;
    public int GetCurrentTrialInBlock() => currentTrialIndex % TRIALS_PER_BLOCK + 1;
    public int GetTotalTrialsInBlock() => TRIALS_PER_BLOCK;
    public Vector2 GetCurrentTrialPlayerPosition() => trials[currentTrialIndex].PlayerPosition;
    public Vector2 GetCurrentTrialRewardPosition() => trials[currentTrialIndex].RewardPosition;
    public int GetCurrentTrialRewardValue() => REWARD_VALUE;
    public Sprite GetCurrentTrialSprite() => trials[currentTrialIndex].EffortSprite;
    public float GetTrialDuration() => TRIAL_DURATION;
    public List<Vector2> GetRewardPositions() => rewardPositions;
    public List<(float collisionTime, float movementDuration)> GetRewardCollectionTimings() => rewardCollectionTimings;
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
            this.PlayerPosition = playerPosition;
            this.RewardPosition = rewardPosition;
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
}