using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PracticeDecisionManager : MonoBehaviour
{
    [SerializeField] private Image effortSpriteImage;
    [SerializeField] private TextMeshProUGUI effortLevelText;
    [SerializeField] private TextMeshProUGUI pressesRequiredText;
    [SerializeField] private Button workButton;
    [SerializeField] private Button skipButton;
    private AudioSource audioSource;
    [SerializeField] private AudioClip workButtonSound;
    [SerializeField] private AudioClip skipButtonSound;
    private PracticeManager practiceManager;

    [Header("Time Settings")]
    // [SerializeField] private float decisionTimeLimit = 2.5f;
    [SerializeField] private TextMeshProUGUI timerText;
    // private float currentTimer;
    // private bool isTimerRunning;

    // New flag to prevent double processing
    private bool hasProcessedCurrentTrial = false;

    // Skip delay constant
    private const float SKIP_DELAY = 3f;
    private bool isSkipDelayActive = false;
    private float skipDelayTimer;

    private void Awake()
    {
        ValidateComponents();
        SetupAudioSource();
        FindPracticeManager();
        // SetupKeyboardNavigation();

        // Add more robust null checks
        if (practiceManager == null)
        {
            // Try to find PracticeManager again, this time using GetComponent if on the same GameObject
            practiceManager = GetComponent<PracticeManager>();

            // If still null, log a more detailed error
            if (practiceManager == null)
            {
                Debug.LogError("PracticeManager could not be found! Ensure a PracticeManager exists in the scene.");
            }
        }

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(workButton);
        navigationController.AddElement(skipButton);

        // SetupButtonListeners(); 
    }

    private void FindPracticeManager()
    {
        practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager could not be found in the scene!");
        }
    }

    private void Update()
    {
        if (isSkipDelayActive)
        {
            UpdateSkipDelay();
            return;
        }

        // if (isTimerRunning)
        // {
        //     UpdateTimer();
        // }

        // Handle keyboard navigation during practice trial
        // if (workButton != null && skipButton != null &&
        //     workButton.interactable && skipButton.interactable &&
        //     isTimerRunning)
        // {
        // HandleKeyboardNavigation(); 
        // }
    }

    private void SetupAudioSource()
    {
        audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }


    private void ValidateComponents()
    {
        // Ensure effort sprite image is configured correctly
        if (effortSpriteImage == null)
        {
            effortSpriteImage = transform.Find("EV Image")?.GetComponent<Image>();
        }

        if (effortSpriteImage != null)
        {
            RectTransform rectTransform = effortSpriteImage.rectTransform;
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(100f, 100f);

            effortSpriteImage.raycastTarget = false;
            effortSpriteImage.preserveAspect = true;
        }

        // Attach decision method directly to buttons
        if (workButton != null)
        {
            workButton.onClick.AddListener(() => OnDecisionMade(true));
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() => OnDecisionMade(false));
        }
    }

    private void OnEnable()
    {
        // Ensure UI is properly initialized when object becomes active
        SetupDecisionPhase();
        // Reset selection when enabled
        // isWorkButtonSelected = true;
        // UpdateButtonSelection();
    }

    public void SetupDecisionPhase()
    {
        Debug.Log($"SetupDecisionPhase CALLED");
        Debug.Log($"Current Practice Trial Index: {practiceManager.GetCurrentPracticeTrialIndex()}");

        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager is not assigned!");
            // Consider finding the PracticeManager if it's null
            practiceManager = FindAnyObjectByType<PracticeManager>();

            if (practiceManager == null)
            {
                Debug.LogError("Cannot find PracticeManager. Cannot start practice phase.");
                return;
            }
        }

        // Ensure practice mode is started if not already
        if (practiceManager.GetCurrentPracticeTrialIndex() < 0)
        {
            practiceManager.StartPracticeMode();
        }

        UpdateEffortSprite();
        EnableButtons();

        // Additional logging
        PracticeManager.PracticeTrial currentTrial = practiceManager.GetCurrentPracticeTrial();
        if (currentTrial != null)
        {
            Debug.Log($"Current Trial Details in SetupDecisionPhase:");
            Debug.Log($"  Effort Level: {currentTrial.effortLevel}");
            Debug.Log($"  Reward Value: {currentTrial.rewardValue}");
        }
        else
        {
            Debug.LogError("No current practice trial found!");
        }

        // StartTimer();

        // // Removed timer start
        //     if (timerText != null)
        //     {
        //         timerText.text = ""; // Clear timer text
        //     }
    }

    // private void StartTimer()
    // {
    //     currentTimer = decisionTimeLimit;
    //     isTimerRunning = true;
    //     if (timerText != null)
    //     {
    //         timerText.text = $"Time: {currentTimer:F0}";
    //     }
    // }

    public void UpdateEffortSprite()
    {
        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager is not assigned!");
            return;
        }

        PracticeManager.PracticeTrial currentTrial = practiceManager.GetCurrentPracticeTrial();
        if (currentTrial == null)
        {
            Debug.LogError($"No current practice trial found at index {practiceManager.GetCurrentPracticeTrialIndex()}!");

            // Attempt to handle the error or reset the practice mode
            practiceManager.StartPracticeMode();
            return;
        }

        Sprite effortSprite = practiceManager.GetCurrentPracticeTrialSprite();
        int effortLevel = practiceManager.GetCurrentTrialEffortLevel();
        int pressesRequired = effortLevel;

        if (effortSprite == null)
        {
            Debug.LogError("Current practice trial sprite is null!");
            return;
        }

        // Update sprite and UI
        effortSpriteImage.sprite = effortSprite;
        effortSpriteImage.enabled = true;
        effortSpriteImage.preserveAspect = true;
        effortSpriteImage.color = Color.white;

        UpdateUITexts(effortLevel, pressesRequired);
    }

    private void UpdateUITexts(int effortLevel, int pressesRequired)
    {
        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager is null in UpdateUITexts!");
            return;
        }

        PracticeManager.PracticeTrial currentTrial = practiceManager.GetCurrentPracticeTrial();
        if (currentTrial != null)
        {
            effortLevel = currentTrial.effortLevel;
            pressesRequired = GetPracticePressesByEffortLevel(effortLevel);
            // pressesRequired = effortLevel; // Directly map effort level to presses required
        }
        else
        {
            Debug.LogError("No current practice trial found!");
            return;
        }

        if (effortLevelText != null)
        {
            // Explicitly show the effort level mapping
            effortLevelText.text = $"Effort Level: {effortLevel} ({GetEffortLevelDescription(effortLevel)})";

            // effortLevelText.text = $"Effort Level: {effortLevel}";
            Debug.Log($"Setting Effort Level Text to: {effortLevelText.text}");
        }

        if (pressesRequiredText != null)
        {
            pressesRequiredText.text = $"Presses Required: {pressesRequired}";
            Debug.Log($"Setting Presses Required Text to: {pressesRequiredText.text}");
        }
    }

    private string GetEffortLevelDescription(int effortLevel)
    {
        switch (effortLevel)
        {
            case 1: return "Apple - Low Effort";
            case 3: return "Grapes - Medium Effort";
            case 5: return "Watermelon - High Effort";
            default:
                Debug.LogWarning($"Unexpected effort level: {effortLevel}");
                return "Unknown Effort";
        }
    }

    private int GetPracticePressesByEffortLevel(int effortLevel)
    {
        switch (effortLevel)
        {
            case 1: return 1; // Apple - 1 press per step
            case 3: return 3; // Grapes - 3 presses per step
            case 5: return 5; // Watermelon - 5 presses per step
            default:
                Debug.LogWarning($"Unexpected effort level: {effortLevel}. Defaulting to 1.");
                return 1;
        }
    }

    // private int GetPracticePressesByEffortLevel(int effortLevel)
    // {
    //     // Directly return the effort level, matching the PracticeManager's logic
    //     switch (effortLevel)
    //     {
    //         case 1: return 1; // 1 press per step
    //         case 3: return 3; // 3 presses per step
    //         case 5: return 5; // 5 presses per step
    //         default: 
    //             Debug.LogWarning($"Unexpected effort level: {effortLevel}. Defaulting to 1.");
    //             return 1;
    //     }
    // }

    private void EnableButtons()
    {
        if (workButton != null)
        {
            workButton.interactable = true;
            // isWorkButtonSelected = true;
            // UpdateButtonSelection();
        }
        if (skipButton != null) skipButton.interactable = true;
    }

    private void DisableButtons()
    {
        if (workButton != null) workButton.interactable = false;
        if (skipButton != null) skipButton.interactable = false;
    }
    private void OnDecisionMade(bool workDecision)
    {
        // Reset the trial processing flag
        hasProcessedCurrentTrial = false;

        // Prevent multiple calls by disabling buttons immediately
        DisableButtons();

        // Play decision sound
        if (audioSource != null)
        {
            AudioClip clipToPlay = workDecision ? workButtonSound : skipButtonSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        // Log the decision
        LogDecision(workDecision);

        // Store the sprite for cross-scene use
        PlayerPrefs.SetString("CurrentRewardSpriteName", effortSpriteImage.sprite.name);
        PlayerPrefs.SetInt("IsPracticeTrial", 1);

        if (workDecision)
        {
            // Load GridWorld for working
            SceneManager.LoadScene("GetReadyEveryTrialPractice");
            PlayerPrefs.SetInt("SkippedTrial", 0);
        }
        else
        {
            // Directly handle trial completion through PracticeManager
            // practiceManager.HandlePracticeTrialCompletion(false);
            ActivateSkipDelay();
        }
    }

    private void ActivateSkipDelay()
    {
        isSkipDelayActive = true;
        skipDelayTimer = SKIP_DELAY;

        if (timerText != null)
        {
            timerText.text = $"Waiting: {skipDelayTimer:F0}";
        }

        Invoke("CompleteSkipDelay", SKIP_DELAY);
    }

    private void UpdateSkipDelay()
    {
        skipDelayTimer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Waiting: {skipDelayTimer:F0}";
        }

        if (skipDelayTimer <= 0)
        {
            CompleteSkipDelay();
        }
    }

    private void CompleteSkipDelay()
    {
        // Prevent multiple trial completions
        if (hasProcessedCurrentTrial)
        {
            Debug.Log("Trial already processed. Skipping duplicate processing.");
            return;
        }

        isSkipDelayActive = false;
        hasProcessedCurrentTrial = true;

        // Log trial index before completion
        Debug.Log($"CompleteSkipDelay - Current Practice Trial Index BEFORE Completion: {practiceManager.GetCurrentPracticeTrialIndex()}");

        practiceManager.HandleGridWorldOutcome(true);

        // Log trial index after completion
        Debug.Log($"CompleteSkipDelay - Current Practice Trial Index AFTER Completion: {practiceManager.GetCurrentPracticeTrialIndex()}");
    }

    // private void UpdateTimer()
    // {
    //     currentTimer -= Time.deltaTime;

    //     if (timerText != null)
    //     {
    //         timerText.text = $"Time: {currentTimer:F0}";
    //     }

    //     if (currentTimer <= 0)
    //     {
    //         TimeExpired();
    //     }
    // }

    // private void TimeExpired()
    // {
    //     isTimerRunning = false;
    //     DisableButtons();

    //     // Log the time expiration
    //     LogDecision(false);
    //     Debug.Log("Decision time expired - Moving to penalty scene");

    //     // Move to penalty scene
    //     SceneManager.LoadScene("TimePenalty");
    // }

    // In PracticeDecisionManager.cs
    private void LogDecision(bool workDecision)
    {
        string trialType = "Practice";
        int currentTrialIndex = practiceManager.GetCurrentPracticeTrialIndex();

        string logEntry = $"{System.DateTime.Now}: {trialType} Trial {currentTrialIndex} - Player decided to {(workDecision ? "Work" : "Skip")}";
        System.IO.File.AppendAllText("practice_decision_log.txt", logEntry + System.Environment.NewLine);
        Debug.Log(logEntry);
    }
}