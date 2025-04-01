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
    [SerializeField] private TextMeshProUGUI instructionText;
    private bool sceneInitialized = false;

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
            DontDestroyOnLoad(gameObject); // Ensure the manager persists across scenes
            InitializePracticeManager(); // Initialize trials and state
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Duplicate PracticeManager detected. Destroying duplicate.");
            Destroy(gameObject); // Destroy duplicate instances
        }
    }

    public void InitializePracticeManager()
    {
        ValidateSprites();
        PrepareDifficulties();
        GeneratePracticeTrials();
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Retrieve the trial index from PlayerPrefs or set it to 0 if not found
        currentPracticeTrialIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex", 0);

        Debug.Log("PracticeManager initialized with trials: " + practiceTrials.Count);
        Debug.Log("Current practice trial index: " + currentPracticeTrialIndex);
    }

    public void SetCurrentPracticeTrialIndex(int index)
    {
        Debug.Log($"Setting currentPracticeTrialIndex from {currentPracticeTrialIndex} to {index}");
        currentPracticeTrialIndex = index;
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", index);
        PlayerPrefs.Save(); // Ensure the value is saved immediately
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && sceneInitialized)
        {
            StartPracticeMode();
        }
    }

    private void OnDisable()
    {
        // Remove scene loading event listener
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}");

        // Ensure trials exist regardless of scene
        EnsurePracticeTrialsExist();

        // Only initialize if we're in the PracticePhase scene
        if (scene.name == "PracticePhase")
        {
            Debug.Log("PracticePhase scene loaded - Initializing scene");
            StartCoroutine(InitializeSceneAfterLoad());
        }
        else
        {
            sceneInitialized = false;
        }
    }

    public void EnsurePracticeTrialsExist()
    {
        if (practiceTrials == null || practiceTrials.Count == 0)
        {
            Debug.Log("Ensuring practice trials exist - regenerating trials");
            PrepareDifficulties();
            GeneratePracticeTrials();
        }
    }

    private IEnumerator InitializeSceneAfterLoad()
    {
        Debug.Log("Starting scene initialization...");
        yield return new WaitForSeconds(0.2f);

        int retryAttempts = 0;
        const int maxRetryAttempts = 5;

        while (!sceneInitialized && retryAttempts < maxRetryAttempts)
        {
            instructionText = GameObject.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();

            if (instructionText != null)
            {
                instructionText.text = "Press 'Space' to continue";
                sceneInitialized = true;
                break;
            }

            retryAttempts++;
            yield return new WaitForSeconds(0.2f);
        }

        if (!sceneInitialized)
        {
            Debug.LogError("Failed to initialize scene after multiple attempts!");
        }
    }

    public void ResetPracticeForNewAttempt()
    {
        Debug.Log("Resetting practice for new attempt");

        // Reset trial tracking
        SetCurrentPracticeTrialIndex(0); // Reset the trial index to 0
        GeneratePracticeTrials(); // Regenerate trials

        // Reset PlayerPrefs for practice state
        PlayerPrefs.SetInt("IsPracticeTrial", 1);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);
        PlayerPrefs.Save();

        Debug.Log("Practice state reset. Current trial index: " + currentPracticeTrialIndex);
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
            // Effortlevel 1: 4 trials with Apple sprite
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            // new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },
            // new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite },

            // Effortlevel 2: 4 trials with Grapes sprite
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },
            new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite },

            // Effortlevel 2: 4 trials with Watermelon sprite
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite },
        };
    }

    private void GeneratePracticeTrials()
    {
        practiceTrials.Clear();

        // Define trial difficulties with effort levels and sprites
        trialDifficulties = new List<TrialDifficulty>
    {
        new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite }, // Apple
        new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite }, // Apple
        // new TrialDifficulty { effortLevel = 1, rewardValue = 10f, rewardSprite = appleSprite }, // Apple
        new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite }, // Grapes
        new TrialDifficulty { effortLevel = 2, rewardValue = 10f, rewardSprite = grapesSprite }, // Grapes
        new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite }, // Watermelon
        new TrialDifficulty { effortLevel = 3, rewardValue = 10f, rewardSprite = watermelonSprite }, // Watermelon
    };

        // Shuffle and generate trials
        List<TrialDifficulty> shuffledDifficulties = new List<TrialDifficulty>(trialDifficulties);
        for (int i = shuffledDifficulties.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            TrialDifficulty temp = shuffledDifficulties[i];
            shuffledDifficulties[i] = shuffledDifficulties[j];
            shuffledDifficulties[j] = temp;
        }

        for (int i = 0; i < totalPracticeTrials; i++)
        {
            TrialDifficulty difficulty = shuffledDifficulties[i % shuffledDifficulties.Count];
            practiceTrials.Add(new PracticeTrial
            {
                effortLevel = difficulty.effortLevel,
                rewardValue = difficulty.rewardValue,
                rewardSprite = difficulty.rewardSprite,
                wasAttempted = false
            });
        }

        Debug.Log($"Generated {practiceTrials.Count} randomized practice trials");
        foreach (var trial in practiceTrials)
        {
            Debug.Log($"Trial - Effort Level: {trial.effortLevel}, Sprite: {trial.rewardSprite.name}");
        }
    }

    public void StartPracticeMode()
    {
        Debug.Log($"StartPracticeMode called! Practice attempts: {practiceAttempts}");

        // Reset practice state
        SetCurrentPracticeTrialIndex(0); // Reset the trial index to 0
        GeneratePracticeTrials(); // Regenerate trials

        // Ensure PlayerPrefs flags are set
        PlayerPrefs.SetInt("IsPracticeTrial", 1); // ✅ Critical for RewardSpawner and PlayerController to detect practice
        PlayerPrefs.Save();

        // Reset score at the start of practice mode
        PracticeScoreManager.Instance?.ResetScore();
        Debug.Log($"StartPracticeMode - Score Reset. Current Score: {PracticeScoreManager.Instance?.GetCurrentScore()}");

        if (practiceAttempts >= MaxPracticeAttempts)
        {
            Debug.Log("Maximum practice attempts reached. Ending experiment.");
            SceneManager.LoadScene("EndExperiment");
            return;
        }

        // Log the start of the first practice trial
        LogPracticeTrialStart(currentPracticeTrialIndex);

        Debug.Log($"Starting practice attempt {practiceAttempts + 1}");
        SceneManager.LoadScene(decisionPhaseScene);
    }

    public void ResetPracticeState()
    {
        currentPracticeTrialIndex = 0;
        GeneratePracticeTrials(); // Regenerate trials
        PlayerPrefs.SetInt("IsPracticeTrial", 1);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);
        PlayerPrefs.Save();

        Debug.Log("Practice state reset. Current trial index: " + currentPracticeTrialIndex);
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

        // Ensure trials exist
        EnsurePracticeTrialsExist();

        if (currentPracticeTrialIndex < 0 || currentPracticeTrialIndex >= practiceTrials.Count)
        {
            Debug.LogError("Invalid practice trial index");
            EndPracticeMode();
            return;
        }

        PracticeTrial currentTrial = practiceTrials[currentPracticeTrialIndex];
        if (!isWorking)
        {
            currentTrial.wasSkipped = true;
            LogPracticeTrialOutcome(currentPracticeTrialIndex, true, false, 0f);
            AdvanceToNextTrial();
            return;
        }

        currentTrial.wasAttempted = true;
        PlayerPrefs.SetInt("CurrentPracticeEffortLevel", currentTrial.effortLevel);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);
        PlayerPrefs.Save();

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
        Debug.Log($"Advancing to trial {currentPracticeTrialIndex}");

        // Check if trials exist
        if (practiceTrials == null || practiceTrials.Count == 0)
        {
            Debug.LogWarning("Practice trials list is empty. Regenerating trials.");
            GeneratePracticeTrials();
        }

        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);
        PlayerPrefs.Save();

        if (currentPracticeTrialIndex >= totalPracticeTrials)
        {
            EndPracticeMode();
            return;
        }

        SceneManager.LoadScene(decisionPhaseScene);
    }

    public void EndPracticeMode()
    {
        currentPracticeTrialIndex = -1;
        // When transitioning from practice trials to formal trials, ensure the IsPracticeTrial flag is reset to 0:
        PlayerPrefs.SetInt("IsPracticeTrial", 0); // Reset the flag
        PlayerPrefs.DeleteKey("CurrentPracticeTrialIndex");
        PlayerPrefs.DeleteKey("CurrentPracticeEffortLevel");

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
        Debug.Log($"GetCurrentPracticeTrial called. Current index: {currentPracticeTrialIndex}, Total trials: {practiceTrials.Count}");

        // Check if trials exist
        if (practiceTrials == null || practiceTrials.Count == 0)
        {
            // Debug.LogWarning("Practice trials list is empty. Regenerating trials.");
            GeneratePracticeTrials();
        }

        if (currentPracticeTrialIndex >= 0 && currentPracticeTrialIndex < practiceTrials.Count)
        {
            return practiceTrials[currentPracticeTrialIndex];
        }
        else
        {
            // Debug.LogError($"Invalid practice trial index: {currentPracticeTrialIndex}. Total trials: {practiceTrials.Count}");
            // Return a default trial to prevent null reference exceptions
            return new PracticeTrial
            {
                effortLevel = 1,
                rewardValue = 10f,
                rewardSprite = appleSprite,
                wasAttempted = false
            };
        }
    }

    // public int GetCurrentTrialPressesRequired()
    // {
    //     PracticeTrial currentTrial = GetCurrentPracticeTrial();
    //     if (currentTrial != null)
    //     {
    //         // Map effort level to presses required
    //         switch (currentTrial.effortLevel)
    //         {
    //             case 1: return 1; // Apple - 1 press per step
    //             case 2: return 3; // Grapes - 3 presses per step
    //             case 3: return 5; // Watermelon - 5 presses per step
    //             default:
    //                 Debug.LogWarning($"Unexpected effort level: {currentTrial.effortLevel}. Defaulting to 1.");
    //                 return 1;
    //         }
    //     }

    //     Debug.LogError("No current practice trial found. Defaulting to 1 press per step.");
    //     return 1;
    // }

    // Now also using the calibrated PressesPerEffortLevel 
public int GetCurrentTrialPressesRequired()
{
    PracticeTrial currentTrial = GetCurrentPracticeTrial();
    if (currentTrial != null)
    {
        // Get the effort level directly from the current trial
        int effortLevel = currentTrial.effortLevel;
        
        // Use PlayerPrefs to retrieve the calibrated presses per effort level
        // Subtract 1 from effortLevel to match the PlayerPrefs keys (which are 0-indexed)
        int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 0);

        Debug.Log($"Practice Trial - Effort Level: {effortLevel}, Calibrated Presses Required: {pressesRequired}");

        // Fallback to default values if no calibrated value is found
        if (pressesRequired == 0)
        {
            switch (effortLevel)
            {
                case 1: return 1; // Apple - 1 press per step
                case 2: return 3; // Grapes - 3 presses per step
                case 3: return 5; // Watermelon - 5 presses per step
                default:
                    Debug.LogWarning($"Unexpected effort level: {effortLevel}. Defaulting to 1.");
                    return 1;
            }
        }

        return pressesRequired;
    }

    Debug.LogError("No current practice trial found. Defaulting to 1 press per step.");
    return 1;
}


    // Ensure this method consistently identifies practice trials
    public bool IsPracticeTrial()
    {
        return currentPracticeTrialIndex >= 0 &&
               currentPracticeTrialIndex < totalPracticeTrials &&
               PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
    }

    public int GetCurrentPracticeTrialIndex()
    {
        if (PlayerPrefs.HasKey("CurrentPracticeTrialIndex"))
        {
            currentPracticeTrialIndex = PlayerPrefs.GetInt("CurrentPracticeTrialIndex");
            Debug.Log($"Retrieved currentPracticeTrialIndex from PlayerPrefs: {currentPracticeTrialIndex + 1}"); // Add 1 for display
        }
        else
        {
            Debug.LogWarning("CurrentPracticeTrialIndex not found in PlayerPrefs. Defaulting to 0.");
            currentPracticeTrialIndex = 0;
        }
        return currentPracticeTrialIndex;
    }

    public int GetTotalPracticeTrials() => totalPracticeTrials;

    public int GetCurrentTrialEffortLevel()
    {
        PracticeTrial currentTrial = GetCurrentPracticeTrial();
        if (currentTrial != null)
        {
            // Map effort level to presses per step
            switch (currentTrial.effortLevel)
            {
                case 1: return 1; // Apple - 1 press per step
                case 2: return 2; // Grapes - 3 presses per step
                case 3: return 3; // Watermelon - 5 presses per step
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
        int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel}", 0);

        Debug.Log($"Current practice trial (index: {currentPracticeTrialIndex}) " +
                  $"Effort Level: {effortLevel}, Presses Required: {pressesRequired}");

        if (pressesRequired <= 0)
        {
            // Fallback to defaults if no calibration found
            Debug.LogWarning("Using default presses for practice trial");
            switch (effortLevel)
            {
                case 1: return 1;
                case 2: return 3;
                case 3: return 5;
                default: return 1;
            }
        }

        return pressesRequired;
    }

    // public int GetCurrentTrialEV()
    // {
    //     int effortLevel = GetCurrentTrialEffortLevel();
    //     int pressesRequired = PlayerPrefs.GetInt($"PressesPerEffortLevel_{effortLevel - 1}", 0); // Subtract 1 to match the PlayerPrefs keys

    //     Debug.Log($"Current practice trial (index: {currentPracticeTrialIndex}) Effort Level: {effortLevel}, Presses Required: {pressesRequired}");

    //     return pressesRequired;
    // }

    // public int GetCurrentTrialEV()
    // {
    //     // Get the effort level directly from the current trial
    //     PracticeTrial currentTrial = GetCurrentPracticeTrial();
    //     if (currentTrial != null)
    //     {
    //         // Direct mapping based on effort level
    //         switch (currentTrial.effortLevel)
    //         {
    //             case 1: return 1; // Apple: 1 press per step
    //             case 2: return 3; // Grapes: 3 presses per step
    //             case 3: return 5; // Watermelon: 5 presses per step
    //             default:
    //                 Debug.LogWarning($"Unexpected effort level: {currentTrial.effortLevel}. Defaulting to 1.");
    //                 return 1;
    //         }
    //     }

    //     Debug.LogError("No current practice trial found. Defaulting to 1 press per step.");
    //     return 1;
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
        Debug.Log("Practice Phase Scene Enabled");
    }

    public void LogPracticeTrialStart(int trialIndex)
    {
        // int effortLevel = GetCurrentTrialEffortLevel();
        // int requiredPresses = GetCurrentTrialEV();
        //  int requiredPresses = PracticeManager.Instance.GetCurrentTrialPressesRequired();

        LogManager.Instance.LogEvent("TrialStart", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()}, // Adjust to 1-based index
            {"BlockNumber", "0"}, // Assuming practice trials are in block 0
            // {"EffortLevel", effortLevel.ToString()},
            // {"RequiredPresses", requiredPresses.ToString()},
            {"AdditionalInfo", "Practice"}
        });
    }


    public void LogPracticeTrialOutcome(int trialIndex, bool wasSkipped, bool rewardCollected, float completionTime)
    {
        // Since we've removed skip functionality, wasSkipped should always be false
        string outcome = rewardCollected ? "Success" : "Failure";

        LogManager.Instance.LogEvent("TrialEnd", new Dictionary<string, string>
        {
            {"TrialNumber", (trialIndex + 1).ToString()},
            {"BlockNumber", "0"}, // Assuming practice trials are in block 0
            {"DecisionType", "Work"}, // Always "Work" now since skipping is removed
            {"OutcomeType", outcome},
            {"RewardCollected", rewardCollected.ToString()},
            {"MovementDuration", completionTime.ToString("F3")},
            {"ButtonPresses", "0"}, // Assuming no button presses are logged here
            {"AdditionalInfo", "Practice"}
        });
    }

    // public void LogPracticeDecisionOutcome(int trialIndex, string decisionType, bool rewardCollected, 
    //     float decisionTime, float movementTime, int buttonPresses, int effortLevel, int requiredPresses, int blockNumber = 0)
    // {
    //     // Use the provided blockNumber parameter, defaulting to 0 for practice trials
    //     Debug.Log($"Logging practice trial outcome - Trial: {trialIndex}, Block: {blockNumber}, Decision: {decisionType}");

    //     // IMPORTANT: Modify how LogManager is called to ensure block number isn't adjusted
    //     // Option 1: If you can modify LogManager.LogDecisionOutcome, add a parameter to skip adjustment
    //     LogManager.Instance.LogDecisionOutcome(
    //         trialIndex,
    //         blockNumber, // Use the provided block number
    //         decisionType,
    //         rewardCollected,
    //         decisionTime,
    //         movementTime,
    //         buttonPresses,
    //         effortLevel,
    //         requiredPresses,
    //         true // Add a parameter to skip adjustment (if possible)
    //     );

    //     // Option 2: If you can't modify LogManager, directly log the event
    //     /*
    //     LogManager.Instance.LogEvent("DecisionOutcome", new Dictionary<string, string>
    //     {
    //         {"TrialNumber", (trialIndex + 1).ToString()}, // Manually adjust to 1-based index
    //         {"BlockNumber", blockNumber.ToString()}, // Use raw block number (0 for practice)
    //         {"DecisionType", decisionType},
    //         {"ReactionTime", decisionTime.ToString("F3")},
    //         {"RewardCollected", rewardCollected.ToString()},
    //         {"MovementTime", movementTime.ToString("F3")},
    //         {"ButtonPresses", buttonPresses.ToString()},
    //         {"EffortLevel", effortLevel.ToString()},
    //         {"RequiredPresses", requiredPresses.ToString()},
    //         {"OutcomeType", DetermineOutcomeType(decisionType, rewardCollected)}
    //     });
    //     */
    // }

    // private const string PRACTICE_BLOCK_ID = "Practice";
    private int baseTrialIndex = 1000; // Offset for practice trials to distinguish from formal trials

    public int GetCurrentTrialIndex()
    {
        return baseTrialIndex + currentPracticeTrialIndex;
    }
}
