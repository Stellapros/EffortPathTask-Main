using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance { get; private set; }

    [Header("Practice Configuration")]
    [SerializeField] private float practiceTrialDuration = 10f;
    [SerializeField] private string getReadyPracticeScene = "GetReadyPractice";
    [SerializeField] private string decisionPhaseScene = "DecisionPhase";
    [SerializeField] private string getReadyCheckScene = "GetReadyCheck";

    [Header("UI Elements")]
    [SerializeField] private Button startPracticeButton;
    [SerializeField] private Button skipButton;
    private RewardSpawner rewardSpawner;

    [Header("Practice Configuration")]
    private const int TOTAL_PRACTICE_TRIALS = 6; // Updated to 6 trials

    [SerializeField]
    private List<PracticeTrial> practiceTrials = new List<PracticeTrial>
{
    // Add 6 different practice trials with varying configurations
    new PracticeTrial { effortLevel = 2, rewardValue = 10f, rewardSpriteIndex = 0 },
    new PracticeTrial { effortLevel = 4, rewardValue = 10f, rewardSpriteIndex = 1 },
    new PracticeTrial { effortLevel = 6, rewardValue = 10f, rewardSpriteIndex = 2 },
    new PracticeTrial { effortLevel = 2, rewardValue = 10f, rewardSpriteIndex = 0 },
    new PracticeTrial { effortLevel = 4, rewardValue = 10f, rewardSpriteIndex = 1 },
    new PracticeTrial { effortLevel = 6, rewardValue = 10f, rewardSpriteIndex = 2 }
};

    private int currentPracticeTrialIndex = -1;
    private bool isPracticeMode = false;
    private DecisionManager decisionManager;
    public event Action OnPracticeCompleted;

    private void Awake()
    {
        SetupSingleton();

        // Add in Start() method
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(startPracticeButton);
        navigationController.AddElement(skipButton);
    }

    private void SetupSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        InitializeComponents();
        // SetupEventListeners();
    }

    private void InitializeComponents()
    {
        decisionManager = FindAnyObjectByType<DecisionManager>();
        rewardSpawner = FindAnyObjectByType<RewardSpawner>();

        if (startPracticeButton != null)
            startPracticeButton.onClick.AddListener(StartPracticeMode);

        if (skipButton != null)
            skipButton.onClick.AddListener(GoToGetReadyCheck);
    }

    public bool IsPracticeTrial() => currentPracticeTrialIndex >= 0 && currentPracticeTrialIndex < TOTAL_PRACTICE_TRIALS;


    public void StartPracticeMode()
    {
        isPracticeMode = true;
        currentPracticeTrialIndex = 0;

        // Set a global flag for practice mode
        PlayerPrefs.SetInt("IsPracticeTrial", 1);

        SceneManager.LoadScene(decisionPhaseScene);
    }

    public Sprite GetCurrentPracticeTrialSprite()
    {
        var currentTrial = GetCurrentPracticeTrial();

        Debug.Log($"Current Practice Trial Index: {currentPracticeTrialIndex}");

        if (currentTrial == null)
        {
            Debug.LogError("No current practice trial found!");
            return null;
        }

        // Log details about the current trial
        Debug.Log($"Current Trial Details: " +
                  $"Effort Level: {currentTrial.effortLevel}, " +
                  $"Reward Sprite Index: {currentTrial.rewardSpriteIndex}, " +
                  $"Direct Reward Sprite: {currentTrial.rewardSprite != null}");

        // Prioritize direct sprite assignment
        if (currentTrial.rewardSprite != null)
        {
            Debug.Log("Returning sprite directly from trial configuration");
            return currentTrial.rewardSprite;
        }

        // Fallback to sprite from prefabs
        if (rewardSpawner != null)
        {
            var practiceRewardPrefabs = rewardSpawner.GetPracticeRewardPrefabs();

            Debug.Log($"Practice Reward Prefabs Count: {practiceRewardPrefabs?.Count ?? 0}");

            if (practiceRewardPrefabs != null && currentTrial.rewardSpriteIndex < practiceRewardPrefabs.Count)
            {
                var selectedPrefab = practiceRewardPrefabs[currentTrial.rewardSpriteIndex];
                var spriteRenderer = selectedPrefab.GetComponent<SpriteRenderer>();

                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    Debug.Log($"Returning sprite from prefab: {spriteRenderer.sprite.name}");
                    return spriteRenderer.sprite;
                }
            }
        }

        Debug.LogError($"Could not find sprite for practice trial {currentTrial.rewardSpriteIndex}");
        return null;
    }

    public void HandlePracticeTrialCompletion(bool success)
    {
        Debug.Log($"HandlePracticeTrialCompletion called - Success: {success}");

        // Reset the practice trial flag
        PlayerPrefs.SetInt("IsPracticeTrial", 0);

        currentPracticeTrialIndex++;

        if (currentPracticeTrialIndex >= TOTAL_PRACTICE_TRIALS)
        {
            Debug.Log("Practice trials completed. Moving to GetReadyCheck scene.");
            EndPracticeMode();
        }
        else
        {
            Debug.Log("Moving to next practice trial in Decision Phase.");
            SceneManager.LoadScene("PracticeDecisionPhase");
        }
    }

    public void CompleteGridWorldPracticeTrial(bool success)
    {
        // Reset the practice trial flag after the trial is complete
        PlayerPrefs.SetInt("IsPracticeTrial", 0);

        // Call the existing method to handle trial progression
        HandlePracticeTrialCompletion(success);
    }

    public void AdvancePracticeTrial()
    {
        currentPracticeTrialIndex++;
        if (currentPracticeTrialIndex >= TOTAL_PRACTICE_TRIALS)
        {
            // Move to GetReadyCheck scene after 6 practice trials
            SceneManager.LoadScene("GetReadyCheck");
        }
    }
    public void GoToGetReadyCheck()
    {
        SceneManager.LoadScene(getReadyCheckScene);
    }

    // Modify existing EndPracticeMode to ensure clean transition
    public void EndPracticeMode()
    {
        isPracticeMode = false;
        currentPracticeTrialIndex = -1;
        OnPracticeCompleted?.Invoke();
        SceneManager.LoadScene(getReadyCheckScene);
    }

    public int GetCurrentPracticeTrialIndex() => currentPracticeTrialIndex;
    public int GetTotalPracticeTrials() => TOTAL_PRACTICE_TRIALS;
    public int GetCurrentTrialEffortLevel() => GetCurrentPracticeTrial()?.effortLevel ?? 0;
    public float GetCurrentTrialRewardValue() => GetCurrentPracticeTrial()?.rewardValue ?? 0f;

    // Add this method to help identify practice trials across scenes
    public bool IsCurrentScenePracticeTrial()
    {
        // This can be used by other scripts to check if the current scene is part of a practice trial
        return PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
    }

    // Add a method to reset the practice trial flag after each trial
    public void ResetPracticeTrialFlag()
    {
        PlayerPrefs.SetInt("IsPracticeTrial", 0);
    }

    public PracticeTrial GetCurrentPracticeTrial()
    {
        return (currentPracticeTrialIndex >= 0 && currentPracticeTrialIndex < practiceTrials.Count)
            ? practiceTrials[currentPracticeTrialIndex]
            : null;
    }


    [Serializable]
    public class PracticeTrial
    {
        [Header("Trial Configuration")]
        public int effortLevel;
        public float rewardValue;

        [Header("Reward Sprite Configuration")]
        public Sprite rewardSprite;
        public int rewardSpriteIndex;

        [Header("Optional Practice-Specific Elements")]
        public bool useCustomPrefab;
        public GameObject customRewardPrefab;
    }
}