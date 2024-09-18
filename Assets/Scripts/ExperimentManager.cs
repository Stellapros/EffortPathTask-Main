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
    private const int TOTAL_TRIALS = 18; // Total number of trials in the experiment
    private const int TRIALS_PER_BLOCK = 6; // Number of trials in each block
    private const int TOTAL_BLOCKS = 3; // Total number of blocks
    private const float TRIAL_DURATION = 10f; // Duration of each trial in seconds
    private const float REWARD_VALUE = 10f; // Value of the reward for each trial
    private const float SKIP_DELAY = 3f; // Delay before showing the next trial after skipping
    #endregion

    #region Serialized Fields
    // [SerializeField] private Sprite squareSprite;
    // [SerializeField] private Sprite circleSprite;
    // [SerializeField] private Sprite triangleSprite;
    [SerializeField] private Sprite level1Sprite;
    [SerializeField] private Sprite level2Sprite;
    [SerializeField] private Sprite level3Sprite;
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
    // private Dictionary<Sprite, float> spriteToEffortMap;
    private Dictionary<Sprite, int> spriteToEffortMap;
    private int currentTrialIndex = 0;
    private int currentBlockIndex = 0;
    private bool experimentStarted = false;
    private float decisionStartTime;
    private float trialStartTime;
    private List<Vector2> rewardPositions = new List<Vector2>();
    private List<(float collisionTime, float movementDuration)> rewardCollectionTimings = new List<(float, float)>();
    private ScoreManager scoreManager;
    private LogManager logManager;
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
            InitializeTrials();
            InitializeSpriteToEffortMap();

            // Initialize LogManager
            logManager = FindObjectOfType<LogManager>();
            if (logManager == null)
            {
                Debug.LogError("LogManager not found in the scene! Creating a new instance.");
                logManager = new GameObject("LogManager").AddComponent<LogManager>();
            }

            // Initialize ScoreManager
            scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager == null)
            {
                Debug.LogError("ScoreManager not found in the scene!");
            }
        }
        else
        {
            Destroy(gameObject);
        }

    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Start the background music
        BackgroundMusicManager.Instance.PlayMusic();

        // ensure that the total time is accurately calculated and displayed when the experiment ends
        PlayerPrefs.SetFloat("ExperimentStartTime", Time.time);

        if (logManager == null)
        {
            Debug.LogError("LogManager is still null in Start method!");
        }
        else
        {
            logManager.LogExperimentStart();
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion

    #region Initialization Methods
    /// <summary>
    /// Initializes all trials for the experiment.
    /// </summary>
    // Generates trials for each block separately, 
    // //ensuring the correct distribution of effort levels within each block.
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

            // Log information about the generated block
            Debug.Log($"Generated Block {i + 1} (Original Index: {blockIndex}): {blockTrials.Count} trials");
        }

        Debug.Log($"Total trials generated: {trials.Count}");
    }

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
            Trial trial = new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex, blockOrder);
            blockTrials.Add(trial);
        }

        return blockTrials.OrderBy(x => Random.value).ToList(); // Shuffle trials within the block
    }

    /// <summary>
    /// Together with total trials = 12
    /// </summary>
    /// <param name="blockIndex"></param>
    /// <returns></returns>
    private List<int> GetEffortLevelsForBlock(int blockIndex)
    {
        List<int> effortLevels = new List<int>();
        switch (blockIndex)
        {
            case 0: // Block 1: 3:2:1 ratio
                effortLevels.AddRange(Enumerable.Repeat(3, 3));
                effortLevels.AddRange(Enumerable.Repeat(2, 2));
                effortLevels.AddRange(Enumerable.Repeat(1, 1));
                break;
            case 1: // Block 2: 1:1:1 ratio
                effortLevels.AddRange(Enumerable.Repeat(3, 2));
                effortLevels.AddRange(Enumerable.Repeat(2, 2));
                effortLevels.AddRange(Enumerable.Repeat(1, 2));
                break;
            case 2: // Block 3: 1:2:3 ratio
                effortLevels.AddRange(Enumerable.Repeat(3, 1));
                effortLevels.AddRange(Enumerable.Repeat(2, 2));
                effortLevels.AddRange(Enumerable.Repeat(1, 3));
                break;
        }
        return effortLevels.OrderBy(x => Random.value).ToList(); // Shuffle effort levels within the block
    }



    /// <summary>
    /// Together with total trials = 90
    /// </summary>
    /// <param name="blockIndex"></param>
    /// <returns></returns>
    // private List<int> GetEffortLevelsForBlock(int blockIndex)
    // {
    //     List<int> effortLevels = new List<int>();
    //     switch (blockIndex)
    //     {
    //         case 0: // Block 1: 3:2:1 ratio
    //             effortLevels.AddRange(Enumerable.Repeat(3, 15));
    //             effortLevels.AddRange(Enumerable.Repeat(2, 10));
    //             effortLevels.AddRange(Enumerable.Repeat(1, 5));
    //             break;
    //         case 1: // Block 2: 1:1:1 ratio
    //             effortLevels.AddRange(Enumerable.Repeat(3, 10));
    //             effortLevels.AddRange(Enumerable.Repeat(2, 10));
    //             effortLevels.AddRange(Enumerable.Repeat(1, 10));
    //             break;
    //         case 2: // Block 3: 1:2:3 ratio
    //             effortLevels.AddRange(Enumerable.Repeat(3, 15));
    //             effortLevels.AddRange(Enumerable.Repeat(2, 10));
    //             effortLevels.AddRange(Enumerable.Repeat(1, 5));
    //             break;
    //     }
    //     return effortLevels.OrderBy(x => Random.value).ToList(); // Shuffle effort levels within the block
    // }




    // private void InitializeTrials()
    // {
    //     trials = new List<Trial>();
    //     for (int block = 0; block < TOTAL_BLOCKS; block++)
    //     {
    //         List<Trial> blockTrials = GenerateBlockTrials(block);
    //         trials.AddRange(blockTrials);
    //     }
    // }

    // private List<Trial> GenerateBlockTrials(int blockIndex)
    // {
    //     List<Trial> blockTrials = new List<Trial>();
    //     int[] effortLevels = GetEffortLevelsForBlock(blockIndex);

    //     for (int i = 0; i < TRIALS_PER_BLOCK; i++)
    //     {
    //         Vector2 playerSpawnPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
    //         Vector2 rewardPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
    //         int effortLevel = effortLevels[i % effortLevels.Length];
    //         Sprite effortSprite = GetSpriteForEffortLevel(effortLevel);
    //         blockTrials.Add(new Trial(effortSprite, playerSpawnPosition, rewardPosition, blockIndex));
    //     }

    //     return blockTrials.OrderBy(x => Random.value).ToList(); // Shuffle trials within the block
    // }

    // private int[] GetEffortLevelsForBlock(int blockIndex)
    // {
    //     switch (blockIndex)
    //     {
    //         case 0: return new int[] { 3, 3, 3, 2, 2, 1 }; // Block 1: 3:2:1 ratio
    //         case 1: return new int[] { 3, 3, 2, 2, 1, 1 }; // Block 2: 1:1:1 ratio
    //         case 2: return new int[] { 1, 2, 2, 3, 3, 3 }; // Block 3: 1:2:3 ratio
    //         default: return new int[] { 1, 2, 3 };
    //     }
    // }

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
            { level1Sprite, 1 },
            { level2Sprite, 2 },
            { level3Sprite, 3 }
        };
    }
    // private void InitializeSpriteToEffortMap()
    // {
    //     spriteToEffortMap = new Dictionary<Sprite, float>
    //     {
    //         { squareSprite, 1f },
    //         { circleSprite, 2f },
    //         { triangleSprite, 3f }
    //     };
    // }


    /// <summary>
    /// Returns a random effort sprite.
    /// </summary>
    // private Sprite GetRandomEffortSprite()
    // {
    //     int randomIndex = Random.Range(0, 3);
    //     switch (randomIndex)
    //     {
    //         case 0: return squareSprite;
    //         case 1: return circleSprite;
    //         case 2: return triangleSprite;
    //         default: return squareSprite;
    //     }
    // }
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
        }
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
                        button.onClick.AddListener(() => LoadScene(transition.toScene));

                        // Special case for starting the experiment
                        if (transition.toScene == decisionPhaseScene && !experimentStarted)
                        {
                            button.onClick.AddListener(StartExperiment);
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
    public void StartExperiment()
    {
        if (!experimentStarted)
        {
            Debug.Log("Starting experiment");
            experimentStarted = true;
            currentTrialIndex = 0;
            currentBlockIndex = 0;
            ScoreManager.Instance.ResetScore(); // Reset score at the start of the experiment
            logManager.LogExperimentStart();

            StartNewBlock();
            MoveToNextTrial();
        }
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    public void HandleDecision(bool workDecision)
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
        float reactionTime = Time.time - decisionStartTime;
        logManager.LogTrialInfo(currentTrialIndex + 1, currentTrial.BlockIndex, currentTrial.BlockOrder, GetCurrentTrialEV(), workDecision, reactionTime);

        if (workDecision)
        {
            Debug.Log("ExperimentManager: Player decided to work. Loading GridWorld scene.");
            LoadScene(gridWorldScene);
        }
        else
        {
            Debug.Log("ExperimentManager: Player decided to skip. Waiting for 3 seconds before showing next trial.");
            StartCoroutine(ShowNextTrialAfterDelay());
        }
        // else
        // {
        //     Debug.Log("Skip decision made. Moving to next trial.");
        //     MoveToNextTrial();
        // }
    }

    // public void HandleDecision(bool workDecision)
    // {
    //     Trial currentTrial = trials[currentTrialIndex];
    //     float reactionTime = Time.time - decisionStartTime;
    //     logManager.LogTrialInfo(currentTrialIndex + 1, currentTrial.BlockIndex, currentTrial.BlockOrder, GetCurrentTrialEV(), workDecision, reactionTime);

    //     if (workDecision)
    //     {
    //         StartGridWorldPhase();
    //     }
    //     else
    //     {
    //         // If skipped, move to the next trial
    //         // currentTrialIndex++;
    //         MoveToNextTrial();
    //     }
    // }
    // public void HandleDecision(bool workDecision)
    // {
    //     Debug.Log($"ExperimentManager: Decision handled: {(workDecision ? "Work" : "Skip")}");
    //     LogDecision(workDecision);

    //     if (workDecision)
    //     {
    //         Debug.Log("ExperimentManager: Player decided to work. Loading GridWorld scene.");
    //         LoadScene(gridWorldScene);
    //     }
    //     else
    //     {
    //         Debug.Log("ExperimentManager: Player decided to skip. Waiting for 3 seconds before showing next trial.");
    //         StartCoroutine(ShowNextTrialAfterDelay());
    //     }

    //     Trial currentTrial = trials[currentTrialIndex];

    //     float reactionTime = Time.time - decisionStartTime; // Assume decisionStartTime is set when the decision phase starts
    //     logManager.LogTrialInfo(currentTrialIndex + 1, currentTrial.BlockIndex, currentTrial.BlockOrder, GetCurrentTrialEV(), workDecision, reactionTime);

    // }

    private void StartGridWorldPhase()
    {
        trialStartTime = Time.time;
        // ... (code to set up and start the grid world phase)
        Debug.Log($"Grid world phase started for trial {currentTrialIndex + 1}");
    }

    /// <summary>
    /// Coroutine to show the next trial after a delay when skipping.
    /// </summary>
    private IEnumerator ShowNextTrialAfterDelay()
    {
        yield return new WaitForSeconds(SKIP_DELAY);
        MoveToNextTrial();
    }

    public void MoveToNextTrial()
    {
        currentTrialIndex++;
        Debug.Log($"Moving to trial {currentTrialIndex}");

        if (currentTrialIndex % TRIALS_PER_BLOCK == 0)
        {
            if (currentBlockIndex < TOTAL_BLOCKS - 1)
            {
                currentBlockIndex++;
                Debug.Log($"Block {currentBlockIndex} completed. Taking a break.");
                LoadScene(restBreakScene);
            }
            else
            {
                Debug.Log("Experiment ended");
                EndExperiment();
            }
        }
        else if (currentTrialIndex >= TOTAL_TRIALS)
        {
            Debug.Log("Experiment ended");
            EndExperiment();
        }
        else
        {
            LoadScene(decisionPhaseScene);
        }
    }
    // public void MoveToNextTrial()
    // {
    //     currentTrialIndex++;
    //     Debug.Log($"Moving to trial {currentTrialIndex}");
    //     if (currentTrialIndex >= TOTAL_TRIALS)
    //     {
    //         Debug.Log("Experiment ended");
    //         EndExperiment();
    //     }
    //     else
    //     {
    //         Debug.Log($"Moving to trial {currentTrialIndex}");
    //         LoadScene(decisionPhaseScene);
    //     }
    // }

    public void ContinueAfterBreak()
    {
        Debug.Log($"ExperimentManager: Continuing to Block {currentBlockIndex + 1}");
        Debug.Log($"ExperimentManager: Loading scene: {decisionPhaseScene}");
        LoadScene(decisionPhaseScene);
    }


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
        Debug.Log($"Decision phase started for trial {currentTrialIndex + 1}");
    }

    /// <summary>
    /// Ends the current trial and logs the result.
    /// </summary>
    // public void EndTrial(bool completed)
    // {
    //     if (scoreManager == null)
    //     {
    //         Debug.LogError("ScoreManager is null in EndTrial method!");
    //         scoreManager = FindObjectOfType<ScoreManager>();
    //         if (scoreManager == null)
    //         {
    //             Debug.LogError("Failed to find ScoreManager in the scene!");
    //             return;
    //         }
    //     }

    //     if (currentTrialIndex >= TOTAL_TRIALS)
    //     {
    //         Debug.Log("All trials completed. Ending experiment.");
    //         EndExperiment();
    //         return;
    //     }

    //     LogTrialOutcome(completed);
    //     OnTrialEnded?.Invoke(completed);
    //     // if (completed)
    //     // {
    //     //     float rewardValue = GetCurrentTrialRewardValue();
    //     //     scoreManager.AddScore(Mathf.RoundToInt(rewardValue));
    //     // }
    //     MoveToNextTrial();
    // }

    public void EndTrial(bool rewardCollected)
    {
        if (logManager == null)
        {
            Debug.LogError("LogManager is null in EndTrial method!");
            return;
        }

        Debug.Log($"Trial ended. Reward collected: {rewardCollected}");
        float completionTime = Time.time - trialStartTime;
        logManager.LogTrialOutcome(currentTrialIndex + 1, rewardCollected, completionTime);

        OnTrialEnded?.Invoke(rewardCollected);

        currentTrialIndex++;
        if (currentTrialIndex % TRIALS_PER_BLOCK == 0)
        {
            EndCurrentBlock();
            if (currentBlockIndex < TOTAL_BLOCKS - 1)
            {
                currentBlockIndex++;
                StartNewBlock();
                LoadScene(restBreakScene);
            }
            else
            {
                EndExperiment();
            }
        }
        else if (currentTrialIndex >= TOTAL_TRIALS)
        {
            EndExperiment();
        }
        else
        {
            MoveToNextTrial();
        }
    }

    private void StartNewBlock()
    {
        logManager?.LogBlockStart(currentBlockIndex, trials[currentTrialIndex].BlockOrder);
    }

    private void EndCurrentBlock()
    {
        logManager?.LogBlockEnd(currentBlockIndex, trials[currentTrialIndex - 1].BlockOrder);
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

        // Save experiment data
        // SaveExperimentData();
        logManager.LogExperimentEnd();
    }
    #endregion

    #region Logging Methods
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
        Debug.Log($"Block {currentBlockIndex}, Trial {currentTrialIndex}: Outcome - {(rewardCollected ? "Reward Collected" : "Time Out")}, Effort Level: {GetCurrentTrialEV()}");
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

    /// <summary>
    /// Saves the experiment data. Implementation needed.
    /// </summary>
    public void SaveExperimentData()
    {
        // TODO: Implement saving logic here
        Debug.Log("Saving experiment data...");
    }
    #endregion

    #region Getter Methods
    /// <summary>
    /// Gets the effort value for the current trial.
    /// </summary>
    public int GetCurrentTrialEV()
    {
        if (trials == null || currentTrialIndex >= trials.Count)
        {
            Debug.LogWarning($"Trials not initialized or currentTrialIndex ({currentTrialIndex}) out of range. Trials count: {(trials != null ? trials.Count.ToString() : "null")}");
            return 0;
        }

        Sprite currentSprite = trials[currentTrialIndex].EffortSprite;
        if (spriteToEffortMap == null || !spriteToEffortMap.ContainsKey(currentSprite))
        {
            Debug.LogWarning($"spriteToEffortMap not initialized or doesn't contain the current sprite. Current sprite: {currentSprite?.name ?? "null"}");
            return 0;
        }

        return spriteToEffortMap[currentSprite];
    }

    public int GetCurrentBlockIndex() => currentBlockIndex;
    public int GetCurrentTrialInBlock() => currentTrialIndex % TRIALS_PER_BLOCK + 1;
    public int GetTotalTrialsInBlock() => TRIALS_PER_BLOCK;
    public Vector2 GetCurrentTrialPlayerPosition() => trials[currentTrialIndex].PlayerPosition;
    public Vector2 GetCurrentTrialRewardPosition() => trials[currentTrialIndex].RewardPosition;
    public float GetCurrentTrialRewardValue() => REWARD_VALUE;
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
}