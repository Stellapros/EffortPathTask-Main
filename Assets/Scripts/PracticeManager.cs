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

    public event Action OnPracticeCompleted;

    private void Awake()
    {
        // Ensure sprites are assigned
        ValidateSprites();

        // Prepare trial difficulties with specific sprites
        PrepareDifficulties();

        // Rest of the singleton and initialization logic remains the same
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Always initialize button listeners
            InitializeButtonListeners();
            GeneratePracticeTrials();

            ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
            navigationController.AddElement(startPracticeButton);
            navigationController.AddElement(skipButton);
        }
        else
        {
            // If another instance exists, reinitialize buttons to ensure functionality
            InitializeButtonListeners();
            Destroy(gameObject);
        }
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
            // Effortlevel 1: 5 trials with Apple sprite
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

            // Effortlevel 5: 3 trials with Watermelon sprite
            new TrialDifficulty { effortLevel = 5, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 5, rewardValue = 10f, rewardSprite = watermelonSprite },
            new TrialDifficulty { effortLevel = 5, rewardValue = 10f, rewardSprite = watermelonSprite }
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
        // Clear existing listeners first
        if (startPracticeButton != null)
        {
            startPracticeButton.onClick.RemoveAllListeners();
            startPracticeButton.onClick.AddListener(StartPracticeMode);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(GoToGetReadyCheck);
        }
    }

    public void StartPracticeMode()
    {
        Debug.Log("StartPracticeMode called!");

        currentPracticeTrialIndex = 0;
        // currentTrialState = PracticeTrialState.DecisionPhase;

        // Set practice mode flags
        PlayerPrefs.SetInt("IsPracticeTrial", 1);
        PlayerPrefs.SetInt("CurrentPracticeTrialIndex", currentPracticeTrialIndex);

        Debug.Log($"Loading scene: {decisionPhaseScene}");
        SceneManager.LoadScene(decisionPhaseScene);
    }

    public void HandleDecisionPhaseOutcome(bool isWorking)
    {
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
            currentTrial.wasSkipped = true;
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
        // currentTrialState = PracticeTrialState.Completed;

        PlayerPrefs.SetInt("IsPracticeTrial", 0);
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
}
