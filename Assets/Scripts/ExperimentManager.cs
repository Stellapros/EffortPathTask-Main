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
            DontDestroyOnLoad(gameObject);
            InitializeTrials();
            InitializeSpriteToEffortMap();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load the welcome page if not already there
        if (SceneManager.GetActiveScene().name != welcomePageScene)
        {
            LoadScene(welcomePageScene);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endregion

    #region Initialization Methods
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

    private void InitializeSpriteToEffortMap()
    {
        spriteToEffortMap = new Dictionary<Sprite, float>
        {
            { squareSprite, 1f },
            { circleSprite, 2f },
            { triangleSprite, 3f }
        };
    }

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
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Called when a new scene is loaded.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupButtons(scene.name);
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
                        button.onClick.RemoveAllListeners(); // Clear existing listeners
                        button.onClick.AddListener(() => LoadScene(transition.toScene));
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
            experimentStarted = true;
            currentTrialIndex = 0;
            //LoadScene(decisionPhaseScene);
        }
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    public void HandleDecision(bool workDecision)
    {
        LogDecision(workDecision);
        if (workDecision)
        {
            LoadScene(gridWorldScene);
        }
        else
        {
            currentTrialIndex++;
            if (currentTrialIndex >= TOTAL_TRIALS)
            {
                EndExperiment();
            }
            else
            {
                StartCoroutine(LoadNextDecisionPhaseWithDelay());
            }
        }
    }

    /// <summary>
    /// Ends the current trial and logs the result.
    /// </summary>
    public void EndTrial(bool completed)
    {
        LogTrialOutcome(completed);
        OnTrialEnded?.Invoke(completed);
        currentTrialIndex++;
        if (currentTrialIndex >= TOTAL_TRIALS)
        {
            EndExperiment();
        }
        else
        {
            LoadScene(decisionPhaseScene);
        }
    }

    /// <summary>
    /// Ends the experiment and transitions to the EndExperiment scene.
    /// </summary>
    private void EndExperiment()
    {
        Debug.Log("All trials completed. Ending experiment.");
        LoadScene(endExperimentScene);
    }

    private IEnumerator LoadNextDecisionPhaseWithDelay()
    {
        yield return new WaitForSeconds(SKIP_DELAY);
        LoadScene(decisionPhaseScene);
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