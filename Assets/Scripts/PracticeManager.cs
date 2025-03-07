using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance { get; private set; }

    [Header("Practice Configuration")]
    [SerializeField] private string decisionPhaseScene = "PracticeDecisionPhase";
    [SerializeField] private string gridWorldScene = "PracticeGridWorld";
    [SerializeField] private string getReadyCheckScene = "GetReadyCheck";

    [Header("UI Elements")]
    [SerializeField] private Button startPracticeButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private TextMeshProUGUI instructionText;
    private bool buttonsInitialized = false;

    [Header("Sprite Configuration")]
    [SerializeField] private Sprite appleSprite; // Sprite for effort level 1
    [SerializeField] private Sprite grapesSprite; // Sprite for effort level 3
    [SerializeField] private Sprite watermelonSprite; // Sprite for effort level 5
    // [SerializeField] private string spritesResourcePath = "Resources/PracticeSprites";

    [Header("Trial Difficulty Configurations")]
    [SerializeField] private int totalPracticeTrials = 12;

    // Trial states
    private enum PracticeTrialState
    {
        DecisionPhase,
        GridWorld,
        Completed
    }

    [Serializable]
    public class TrialDifficulty
    {
        public int effortLevel;
        public float rewardValue;
        public Sprite rewardSprite;
    }

    [Serializable]
    public class PracticeTrial
    {
        public int effortLevel;
        public float rewardValue;
        public Sprite rewardSprite;
        public bool wasSkipped;
        public bool wasAttempted;
    }

    private List<TrialDifficulty> trialDifficulties;

    private List<PracticeTrial> practiceTrials = new List<PracticeTrial>();

    private int currentPracticeTrialIndex = -1;
    // private PracticeTrialState currentTrialState = PracticeTrialState.DecisionPhase;

    // Track practice attempts
    private int practiceAttempts = 0;
    private const int MaxPracticeAttempts = 2;

    public event Action OnPracticeCompleted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Check if we're returning from a failed check
            bool needsRetry = PlayerPrefs.GetInt("NeedsPracticeRetry", 0) == 1;
            if (needsRetry)
            {
                Debug.Log("Detected failed check, preparing for retry");
                practiceAttempts = PlayerPrefs.GetInt("PracticeAttempts", 0);
                // Reset the retry flag immediately
                PlayerPrefs.SetInt("NeedsPracticeRetry", 0);
                PlayerPrefs.Save();
            }
            else
            {
                // Fresh start
                practiceAttempts = 0;
                PlayerPrefs.SetInt("PracticeAttempts", 0);
                PlayerPrefs.Save();
            }

            ValidateSprites();
            PrepareDifficulties();
            GeneratePracticeTrials();

            // Subscribe to scene loading event
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && buttonsInitialized)
        {
            StartPracticeMode();
        }
    }

    private void OnDisable()
    {
        // Remove scene loading event listener
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Clean up button listeners and clear references
        CleanupButtonListeners();
    }

    private void CleanupButtonListeners()
    {
        if (startPracticeButton != null)
        {
            startPracticeButton.onClick.RemoveAllListeners();
            startPracticeButton = null;
        }
        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton = null;
        }

        // Reset initialization flags
        buttonsInitialized = false;

        // Remove any existing navigation controller
        ButtonNavigationController navigationController = GetComponent<ButtonNavigationController>();
        if (navigationController != null)
        {
            navigationController.ClearElements();
            Destroy(navigationController);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");

        // Only initialize buttons if we're in the PracticePhase scene
        if (scene.name == "PracticePhase")
        {
            Debug.Log("PracticePhase scene loaded - Initializing buttons");
            CleanupButtonListeners(); // Clean up any existing listeners first
            StartCoroutine(InitializeButtonsAfterSceneLoad());
        }
        else
        {
            // If we're not in PracticePhase, ensure all listeners are cleaned up
            CleanupButtonListeners();
        }
    }

    private IEnumerator InitializeButtonsAfterSceneLoad()
    {
        Debug.Log("Starting button initialization...");
        yield return new WaitForSeconds(0.2f);

        int retryAttempts = 0;
        const int maxRetryAttempts = 5;

        while (!buttonsInitialized && retryAttempts < maxRetryAttempts)
        {
            startPracticeButton = GameObject.Find("StartPracticeButton")?.GetComponent<Button>();
            skipButton = GameObject.Find("SkipButton")?.GetComponent<Button>();
            instructionText = GameObject.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();

            if (startPracticeButton != null && skipButton != null && instructionText != null)
            {
                startPracticeButton.gameObject.SetActive(true);
                skipButton.gameObject.SetActive(true);
                instructionText.text = "Press 'Space' to continue";

                // Clear any existing listeners
                startPracticeButton.onClick.RemoveAllListeners();
                skipButton.onClick.RemoveAllListeners();

                // Add new listeners
                startPracticeButton.onClick.AddListener(() => StartPracticeMode());
                skipButton.onClick.AddListener(() => GoToGetReadyCheck());

                SetupButtonNavigation();
                buttonsInitialized = true;
                break;
            }

            retryAttempts++;
            yield return new WaitForSeconds(0.2f);
        }

        if (!buttonsInitialized)
        {
            Debug.LogError("Failed to initialize buttons after multiple attempts!");
        }
    }

    private void SetupButtonNavigation()
    {
        // Remove any existing navigation controller
        ButtonNavigationController existingController = GetComponent<ButtonNavigationController>();
        if (existingController != null)
        {
            Destroy(existingController);
        }

        // Add new navigation controller
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        if (startPracticeButton != null) navigationController.AddElement(startPracticeButton);
        if (skipButton != null) navigationController.AddElement(skipButton);
    }

    public void ResetPracticeForNewAttempt()
    {
        Debug.Log("Resetting practice for new attempt");

        // Reset trial tracking
        currentPracticeTrialIndex = -1;
        practiceTrials.Clear();
        GeneratePracticeTrials();

        // Reset PlayerPrefs for practice state
        PlayerPrefs.SetInt("IsPracticeTrial", 0);
        PlayerPrefs.DeleteKey("CurrentPracticeTrialIndex");
        PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");
        PlayerPrefs.Save();

        // Reset button state
        buttonsInitialized = false;
        startPracticeButton = null;
        skipButton = null;

        // Load PracticePhase scene
        SceneManager.LoadScene("PracticePhase");
    }

    public void ReinitializeButtons()
    {
        InitializeButtonListeners();

        // Remove existing ButtonNavigationController and add a new one
        ButtonNavigationController existingController = GetComponent<ButtonNavigationController>();
        if (existingController != null)
        {
            Destroy(existingController);
        }

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(startPracticeButton);
        navigationController.AddElement(skipButton);
    }

    private void ValidateSprites()
    {
        // Check if sprites are assigned
        if (appleSprite == null || grapesSprite == null || watermelonSprite == null)
        {
            Debug.LogError("One or more reward sprites are not assigned in the inspector!");
        }
    }

    private void PrepareDifficulties()
    {
        // Create trial difficulties with specific sprites for each effort level
        trialDifficulties = new List<TrialDifficulty>
        {
            // Effortlevel 1: 6 trials with Apple sprite
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },

            // Effortlevel 3: 4 trials with Grapes sprite
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = grapesSprite },

            // Effortlevel 5: 2 trials with Watermelon sprite
            new TrialDifficulty { effortLevel = 5, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 5, rewardValue = 10f, rewardSprite = watermelonSprite },
        };
    }

    private void GeneratePracticeTrials()
    {
        practiceTrials.Clear();

        if (trialDifficulties == null || trialDifficulties.Count == 0)
        {
            Debug.LogError("No trial difficulties configured. Cannot generate practice trials.");
            return;
        }

        // Create a copy of trial difficulties to randomize
        List<TrialDifficulty> shuffledDifficulties = new List<TrialDifficulty>(trialDifficulties);

        // Use Fisher-Yates shuffle algorithm to randomize the difficulties
        for (int i = shuffledDifficulties.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            TrialDifficulty temp = shuffledDifficulties[i];
            shuffledDifficulties[i] = shuffledDifficulties[j];
            shuffledDifficulties[j] = temp;
        }

        // Generate trials using shuffled difficulties
        for (int i = 0; i < totalPracticeTrials; i++)
        {
            TrialDifficulty difficulty = shuffledDifficulties[i % shuffledDifficulties.Count];

            PracticeTrial newTrial = new PracticeTrial
            {
                effortLevel = difficulty.effortLevel,
                rewardValue = difficulty.rewardValue,
                rewardSprite = difficulty.rewardSprite,
                wasSkipped = false,
                wasAttempted = false
            };

            practiceTrials.Add(newTrial);
        }

        Debug.Log($"Generated {practiceTrials.Count} randomized practice trials");

        // Debug log to verify sprite and effort level assignments
        foreach (var trial in practiceTrials)
        {
            Debug.Log($"Trial - Effort Level: {trial.effortLevel}, Sprite: {trial.rewardSprite.name}");
        }
    }

    private void Start()
    {
        // Find buttons if not already assigned
        if (startPracticeButton == null)
            startPracticeButton = GameObject.Find("StartPracticeButton").GetComponent<Button>();

        if (skipButton == null)
            skipButton = GameObject.Find("SkipButton").GetComponent<Button>();

        // Reinitialize buttons
        ReinitializeButtons();
    }

    private void InitializeButtonListeners()
    {
        Debug.Log("Initializing button listeners");

        if (startPracticeButton != null)
        {
            // Remove all previous listeners before adding new ones
            startPracticeButton.onClick.RemoveAllListeners();
            startPracticeButton.onClick.AddListener(() =>
            {
                Debug.Log("Start Practice button clicked");
                StartPracticeMode();
            });
        }
        else
        {
            Debug.LogError("StartPracticeButton is null during listener initialization!");
        }

        if (skipButton != null)
        {
            // Remove all previous listeners before adding new ones
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() =>
            {
                Debug.Log("Skip button clicked");
                GoToGetReadyCheck();
            });
        }
        else
        {
            Debug.LogError("SkipButton is null during listener initialization!");
        }
    }

    public void StartPracticeMode()
    {
        Debug.Log($"StartPracticeMode called! Practice attempts: {practiceAttempts}");

        // Reset score at the start of practice mode
        PracticeScoreManager.Instance?.ResetScore();

        Debug.Log($"StartPracticeMode - Score Reset. Current Score: {PracticeScoreManager.Instance?.GetCurrentScore()}");

        if (practiceAttempts >= MaxPracticeAttempts)
        {
            Debug.Log("Maximum practice attempts reached. Ending experiment.");
            SceneManager.LoadScene("EndExperiment");
            return;
        }

        // Initialize practice state
        currentPracticeTrialIndex = 0;
        PlayerPrefs.SetInt("IsPracticeTrial", 1);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);
        PlayerPrefs.Save();

        // Log the start of the first practice trial
        LogPracticeTrialStart(currentPracticeTrialIndex + 1); // adjusted for 0-based index

        Debug.Log($"Starting practice attempt {practiceAttempts + 1}");
        SceneManager.LoadScene(decisionPhaseScene);
    }

    public void HandleChecksFailed()
    {
        Debug.Log("Checks failed, handling retry...");
        practiceAttempts++;
        PlayerPrefs.SetInt("PracticeAttempts", practiceAttempts);
        PlayerPrefs.Save();

        if (practiceAttempts < MaxPracticeAttempts)
        {
            Debug.Log($"Starting practice attempt {practiceAttempts + 1}");
            ResetPracticeForNewAttempt();
        }
        else
        {
            Debug.Log("Maximum practice attempts reached");
            SceneManager.LoadScene("EndExperiment");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CleanupButtonListeners();

        // Clean up all practice-related PlayerPrefs
        PlayerPrefs.DeleteKey("PracticeAttempts");
        PlayerPrefs.DeleteKey("NeedsPracticeRetry");
        PlayerPrefs.DeleteKey("IsPracticeTrial");
        PlayerPrefs.DeleteKey("CurrentPracticeTrialIndex");
        PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");
    }

    public void HandleDecisionPhaseOutcome(bool isWorking)
    {
        Debug.Log($"HandleDecisionPhaseOutcome called. isWorking: {isWorking}. Current Score: {PracticeScoreManager.Instance?.GetCurrentScore()}");


        if (currentPracticeTrialIndex < 0 || currentPracticeTrialIndex >= practiceTrials.Count)
        {
            Debug.LogError("Invalid practice trial index");
            EndPracticeMode();
            return;
        }

        PracticeTrial currentTrial = practiceTrials[currentPracticeTrialIndex];

        if (!isWorking)
        {
            // Trial was skipped
            // PracticeScoreManager.Instance?.AddScore(1);
            currentTrial.wasSkipped = true;
            LogPracticeTrialOutcome(currentPracticeTrialIndex, true, false, 0f); // Log skipped trial
            AdvanceToNextTrial();
            return;
        }

        // If working, go to GridWorld
        currentTrial.wasAttempted = true;
        // currentTrialState = PracticeTrialState.GridWorld;
        PlayerPrefs.SetInt("CurrentPracticeEffortLevel", currentTrial.effortLevel);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);
        PlayerPrefs.Save(); // Ensure PlayerPrefs are written immediately

        Debug.Log($"Transitioning to GridWorld. Effort Level: {currentTrial.effortLevel}, Trial Index: {currentPracticeTrialIndex}");
        SceneManager.LoadScene(gridWorldScene);
    }

    public void HandleGridWorldOutcome(bool timeExpired)
    {
        // GridWorld trial is considered complete whether time expires or reward is collected
        Debug.Log($"HandleGridWorldOutcome called. Time Expired: {timeExpired}");
        // Reset practice trial state explicitly
        PlayerPrefs.SetInt("IsPracticeTrial", 1);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);

        AdvanceToNextTrial();
    }

    private void AdvanceToNextTrial()
    {
        currentPracticeTrialIndex++;
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);

        // Check if practice is complete
        if (currentPracticeTrialIndex >= totalPracticeTrials)
        {
            EndPracticeMode();
            return;
        }

        // Reset trial state and move to next Decision Phase
        // currentTrialState = PracticeTrialState.DecisionPhase;
        SceneManager.LoadScene(decisionPhaseScene);
    }

    public void GoToGetReadyCheck()
    {
        EndPracticeMode();
    }

    public void EndPracticeMode()
    {
        currentPracticeTrialIndex = -1;

        PlayerPrefs.SetInt("IsPracticeTrial", 0);
        PlayerPrefs.DeleteKey("CurrentPracticeTrialIndex");
        PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");

        // Clean up listeners before transitioning
        CleanupButtonListeners();

        OnPracticeCompleted?.Invoke();
        SceneManager.LoadScene(getReadyCheckScene);
        RestoreExperimentControllers();
    }


    // Getters for current trial information
    public Sprite GetCurrentPracticeTrialSprite()
    {
        return (currentPracticeTrialIndex >= 0 && currentPracticeTrialIndex < practiceTrials.Count)
            ? practiceTrials[currentPracticeTrialIndex].rewardSprite
            : null;
    }

    public PracticeTrial GetCurrentPracticeTrial()
    {
        return (currentPracticeTrialIndex >= 0 && currentPracticeTrialIndex < practiceTrials.Count)
            ? practiceTrials[currentPracticeTrialIndex]
            : null;
    }

    // Ensure this method consistently identifies practice trials
    public bool IsPracticeTrial()
    {
        return currentPracticeTrialIndex >= 0 &&
               currentPracticeTrialIndex < totalPracticeTrials &&
               PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
    }

    public int GetCurrentPracticeTrialIndex() => currentPracticeTrialIndex;
    // public int GetCurrentTrialEffortLevel() => GetCurrentPracticeTrial()?.effortLevel ?? 0;
    public int GetTotalPracticeTrials() => totalPracticeTrials;

    public int GetCurrentTrialEffortLevel()
    {
        PracticeTrial currentTrial = GetCurrentPracticeTrial();
        if (currentTrial != null)
        {
            // Map effort level to presses per step
            switch (currentTrial.effortLevel)
            {
                case 1: return 1; // Effort level 1 = 1 press per step
                case 3: return 3; // Effort level 3 = 3 presses per step
                case 5: return 5; // Effort level 5 = 5 presses per step
                default:
                    Debug.LogWarning($"Unexpected effort level: {currentTrial.effortLevel}. Defaulting to 1.");
                    return 1;
            }
        }

        Debug.LogError("No current practice trial found. Defaulting to 1 press per step.");
        return 1;
    }

    public int GetCurrentTrialEV()
    {
        int effortLevel = GetCurrentTrialEffortLevel();
        int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel - 1}", 0); // Subtract 1 to match the PlayerPrefs keys

        Debug.Log($"Current practice trial (index: {currentPracticeTrialIndex}) Effort Level: {effortLevel}, Presses Required: {pressesRequired}");

        return pressesRequired;
    }

    // Experiment controller management
    // private void DisableExperimentControllers()
    // {
    //     var experimentManager = FindAnyObjectByType<ExperimentManager>();
    //     var gameController = FindAnyObjectByType<GameController>();

    //     if (experimentManager != null)
    //         experimentManager.enabled = false;

    //     if (gameController != null)
    //         gameController.enabled = false;
    // }

    private void RestoreExperimentControllers()
    {
        var experimentManager = FindAnyObjectByType<ExperimentManager>();
        var gameController = FindAnyObjectByType<GameController>();

        if (experimentManager != null)
            experimentManager.enabled = true;

        if (gameController != null)
            gameController.enabled = true;
    }

    private void OnEnable()
    {
        // Ensure buttons are initialized when scene becomes active
        if (Instance != null)
        {
            Instance.ReinitializeButtons();
        }

        Debug.Log("Practice Phase Scene Enabled - Checking Buttons");
        if (startPracticeButton != null)
            Debug.Log("Start Practice Button is assigned");
        else
            Debug.LogError("Start Practice Button is NOT assigned!");
    }

public void LogPracticeTrialStart(int trialIndex)
{
    int effortLevel = GetCurrentTrialEffortLevel();
    int requiredPresses = GetCurrentTrialEV();

    LogManager.Instance.LogEvent("TrialStart", new Dictionary<string, string>
    {
        {"TrialNumber", trialIndex.ToString()}, // Adjust to 1-based index
        {"BlockNumber", "0"}, // Assuming practice trials are in block 0
        {"EffortLevel", effortLevel.ToString()},
        {"RequiredPresses", requiredPresses.ToString()},
        {"AdditionalInfo", "Practice"}
    });
}

public void LogPracticeTrialOutcome(int trialIndex, bool wasSkipped, bool rewardCollected, float completionTime)
{
    string outcome = wasSkipped ? "Skipped" : (rewardCollected ? "Success" : "Failure");

    LogManager.Instance.LogEvent("TrialEnd", new Dictionary<string, string>
    {
        {"TrialNumber", (trialIndex + 1).ToString()},
        {"BlockNumber", "0"}, // Assuming practice trials are in block 0
        {"DecisionType", wasSkipped ? "Skip" : "Work"},
        {"OutcomeType", outcome},
        {"RewardCollected", rewardCollected.ToString()},
        {"MovementDuration", completionTime.ToString("F3")},
        {"ButtonPresses", "0"}, // Assuming no button presses are logged here
        {"AdditionalInfo", "Practice"}
    });
}

    private const string PRACTICE_BLOCK_ID = "Practice";
    private int baseTrialIndex = 1000; // Offset for practice trials to distinguish from formal trials

    public int GetCurrentTrialIndex()
    {
        return baseTrialIndex + currentPracticeTrialIndex;
    }

    // public void LogTrialStart(int trialIndex)
    // {
    //     int effortLevel = GetCurrentTrialEffortLevel();
        
    //     LogManager.Instance.LogEvent("TrialStart", new Dictionary<string, string>
    //     {
    //         {"TrialNumber", trialIndex.ToString()},
    //         {"BlockType", PRACTICE_BLOCK_ID},
    //         {"EffortLevel", effortLevel.ToString()},
    //         {"RequiredPresses", GetCurrentTrialEV().ToString()}
    //     });
    // }

    // public void LogTrialCompletion(int trialIndex, bool skipped, bool rewardCollected, float duration, int buttonPresses)
    // {
    //     LogManager.Instance.LogEvent("TrialEnd", new Dictionary<string, string>
    //     {
    //         {"TrialNumber", (trialIndex + 1).ToString()},
    //         {"BlockType", PRACTICE_BLOCK_ID},
    //         {"Skipped", skipped.ToString()},
    //         {"RewardCollected", rewardCollected.ToString()},
    //         {"Duration", duration.ToString("F3")},
    //         {"TotalPresses", buttonPresses.ToString()}
    //     });
    // }

    // public void LogButtonPress(int trialIndex, int pressCount, string direction)
    // {
    //     LogManager.Instance.LogEvent("ButtonPress", new Dictionary<string, string>
    //     {
    //         {"TrialNumber", (trialIndex + 1).ToString()},
    //         {"BlockType", PRACTICE_BLOCK_ID},
    //         {"PressNumber", pressCount.ToString()},
    //         {"Direction", direction}
    //     });
    // }
}