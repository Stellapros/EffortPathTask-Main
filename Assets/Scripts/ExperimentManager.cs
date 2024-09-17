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
    private const int TOTAL_TRIALS = 4; // Total number of trials in the experiment
    private const float TRIAL_DURATION = 10f; // Duration of each trial in seconds
    private const float REWARD_VALUE = 10f; // Value of the reward for each trial
    private const float SKIP_DELAY = 3f; // Delay before showing the next trial after skipping
    #endregion

    #region Serialized Fields
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite triangleSprite;
    [SerializeField] private string decisionPhaseScene = "DecisionPhase";
    [SerializeField] private string gridWorldScene = "GridWorld";
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
    private Dictionary<Sprite, float> spriteToEffortMap;
    private int currentTrialIndex = 0;
    private bool experimentStarted = false;
    private List<Vector2> rewardPositions = new List<Vector2>();
    private List<(float collisionTime, float movementDuration)> rewardCollectionTimings = new List<(float, float)>();
    private ScoreManager scoreManager;
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
    private void InitializeTrials()
    {
        trials = new List<Trial>();
        for (int i = 0; i < TOTAL_TRIALS; i++)
        {
            Vector2 playerSpawnPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            Vector2 rewardPosition = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            Sprite randomEffortSprite = GetRandomEffortSprite();
            trials.Add(new Trial(randomEffortSprite, playerSpawnPosition, rewardPosition));
        }
        trials = trials.OrderBy(x => Random.value).ToList(); // Shuffle trials
    }

    /// <summary>
    /// Initializes the mapping between sprites and effort levels.
    /// </summary>
    private void InitializeSpriteToEffortMap()
    {
        spriteToEffortMap = new Dictionary<Sprite, float>
        {
            { squareSprite, 1f },
            { circleSprite, 2f },
            { triangleSprite, 3f }
        };
    }

    /// <summary>
    /// Returns a random effort sprite.
    /// </summary>
    private Sprite GetRandomEffortSprite()
    {
        int randomIndex = Random.Range(0, 3);
        switch (randomIndex)
        {
            case 0: return squareSprite;
            case 1: return circleSprite;
            case 2: return triangleSprite;
            default: return squareSprite;
        }
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
            ScoreManager.Instance.ResetScore(); // Reset score at the start of the experiment
        }
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    public void HandleDecision(bool workDecision)
    {
        Debug.Log($"ExperimentManager: Decision handled: {(workDecision ? "Work" : "Skip")}");
        LogDecision(workDecision);

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
    }

    /// <summary>
    /// Coroutine to show the next trial after a delay when skipping.
    /// </summary>
    private IEnumerator ShowNextTrialAfterDelay()
    {
        yield return new WaitForSeconds(SKIP_DELAY);
        MoveToNextTrial();
    }

    /// <summary>
    /// Moves to the next trial or ends the experiment if all trials are completed.
    /// </summary>
    public void MoveToNextTrial()
    {
        currentTrialIndex++;
        Debug.Log($"Moving to trial {currentTrialIndex}");
        if (currentTrialIndex >= TOTAL_TRIALS)
        {
            Debug.Log("Experiment ended");
            EndExperiment();
        }
        else
        {
            Debug.Log($"Moving to trial {currentTrialIndex}");
            LoadScene(decisionPhaseScene);
        }
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
        Debug.Log($"Trial ended. Reward collected: {rewardCollected}");
        LogTrialOutcome(rewardCollected);
        OnTrialEnded?.Invoke(rewardCollected);
        MoveToNextTrial();
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
        SaveExperimentData();
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
        Debug.Log($"Trial {currentTrialIndex}: Outcome - {(rewardCollected ? "Reward Collected" : "Time Out")}, Effort Level: {GetCurrentTrialEV()}");
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
    public float GetCurrentTrialEV()
    {
        if (trials == null || currentTrialIndex >= trials.Count)
        {
            Debug.LogWarning($"Trials not initialized or currentTrialIndex ({currentTrialIndex}) out of range. Trials count: {(trials != null ? trials.Count.ToString() : "null")}");
            return 0f;
        }

        Sprite currentSprite = trials[currentTrialIndex].EffortSprite;
        if (spriteToEffortMap == null || !spriteToEffortMap.ContainsKey(currentSprite))
        {
            Debug.LogWarning($"spriteToEffortMap not initialized or doesn't contain the current sprite. Current sprite: {currentSprite?.name ?? "null"}");
            return 0f;
        }

        return spriteToEffortMap[currentSprite];
    }

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

        public Trial(Sprite effortSprite, Vector2 playerPosition, Vector2 rewardPosition)
        {
            this.EffortSprite = effortSprite;
            this.PlayerPosition = playerPosition;
            this.RewardPosition = rewardPosition;
        }
    }
}