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
    #region Constants
    private const int TOTAL_TRIALS = 90;
    private const float TRIAL_DURATION = 10f;
    private const float REWARD_VALUE = 10f;
    private const float SKIP_DELAY = 3f;
    #endregion

    #region Serialized Fields
    [SerializeField] private Sprite squareSprite;
    [SerializeField] private Sprite circleSprite;
    [SerializeField] private Sprite triangleSprite;
    [SerializeField] private string welcomePageScene = "TitlePage";
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
    private bool isDecisionPhase = true;
    private bool experimentStarted = false;
    #endregion

    #region Events
    public event System.Action<bool> OnTrialEnded;
    #endregion

    public static ExperimentManager Instance { get; private set; }

    #region Unity Lifecycle Methods
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            // This Unity function prevents the GameObject (and its components) 
            // from being destroyed when a new scene is loaded
            DontDestroyOnLoad(gameObject);
            InitializeTrials();
            InitializeSpriteToEffortMap();
            //SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Load the welcome page if not already there
        // if (SceneManager.GetActiveScene().name != welcomePageScene)
        // {
        //     LoadScene(welcomePageScene);
        // }

        // Start the background music
        BackgroundMusicManager.Instance.PlayMusic();
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
    /// Progresses to the next scene in the experiment flow.
    /// </summary>
    public void ProgressToNextScene(string nextScene)
    {
        if (nextScene == decisionPhaseScene)
        {
            if (!experimentStarted)
            {
                experimentStarted = true;
                currentTrialIndex = 0;
            }
            isDecisionPhase = true;
        }
        else if (nextScene == gridWorldScene)
        {
            isDecisionPhase = false;
        }
        LoadScene(nextScene);
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
            //ProgressToNextScene(decisionPhaseScene);
        }
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    // public void HandleDecision(bool workDecision)
    // {
    //     LogDecision(workDecision);
    //     if (workDecision)
    //     {
    //         ProgressToNextScene(gridWorldScene);
    //     }
    //     else
    //     {
    //         currentTrialIndex++;
    //         if (currentTrialIndex >= TOTAL_TRIALS)
    //         {
    //             EndExperiment();
    //         }
    //         else
    //         {
    //             StartCoroutine(LoadNextDecisionPhaseWithDelay());
    //         }
    //     }
    // }    

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

    private IEnumerator ShowNextTrialAfterDelay()
    {
        yield return new WaitForSeconds(SKIP_DELAY);
        MoveToNextTrial();
    }

    private void MoveToNextTrial()
    {
        currentTrialIndex++;
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
    public void EndTrial(bool completed)
    {
        LogTrialOutcome(completed);
        OnTrialEnded?.Invoke(completed);
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
    }

    private IEnumerator LoadNextDecisionPhaseWithDelay()
    {
        yield return new WaitForSeconds(SKIP_DELAY);
        ProgressToNextScene(decisionPhaseScene);
    }
    #endregion

    #region Logging Methods
    private void LogDecision(bool worked)
    {
        Debug.Log($"Trial {currentTrialIndex}: Decision - {(worked ? "Worked" : "Skipped")}, Effort Level: {GetCurrentTrialEV()}");
    }

    private void LogTrialOutcome(bool rewardCollected)
    {
        Debug.Log($"Trial {currentTrialIndex}: Outcome - {(rewardCollected ? "Reward Collected" : "Time Out")}, Effort Level: {GetCurrentTrialEV()}");
    }

    public void LogTrialData(bool completed, float reactionTime, int buttonPresses)
    {
        Debug.Log($"Trial {currentTrialIndex}: Completed - {completed}, Reaction Time - {reactionTime}, Button Presses - {buttonPresses}, Effort Level: {GetCurrentTrialEV()}");
    }
    #endregion

    #region Getter Methods
    public float GetCurrentTrialEV()
    {
        if (trials == null || currentTrialIndex >= trials.Count)
        {
            Debug.LogError($"Trials not initialized or currentTrialIndex ({currentTrialIndex}) out of range. Trials count: {(trials != null ? trials.Count.ToString() : "null")}");
            return 0f;
        }

        Sprite currentSprite = trials[currentTrialIndex].EffortSprite;
        if (spriteToEffortMap == null || !spriteToEffortMap.ContainsKey(currentSprite))
        {
            Debug.LogError($"spriteToEffortMap not initialized or doesn't contain the current sprite. Current sprite: {currentSprite?.name ?? "null"}");
            return 0f;
        }

        return spriteToEffortMap[currentSprite];
    }

    public Vector2 GetCurrentTrialPlayerPosition() => trials[currentTrialIndex].PlayerPosition;
    public Vector2 GetCurrentTrialRewardPosition() => trials[currentTrialIndex].RewardPosition;
    public float GetCurrentTrialRewardValue() => REWARD_VALUE;
    public Sprite GetCurrentTrialSprite() => trials[currentTrialIndex].EffortSprite;
    public float GetTrialDuration() => TRIAL_DURATION;
    #endregion
}

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
