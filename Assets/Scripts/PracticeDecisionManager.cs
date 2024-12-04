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
    private ExperimentManager experimentManager;
    private PracticeManager practiceManager;
    private RewardSpawner rewardSpawner;

    // Practice trial specific variables
    private const int TOTAL_PRACTICE_TRIALS = 3;
    private bool isPracticeTrial = false;

    // Added for keyboard navigation
    private bool isWorkButtonSelected = true;
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color selectedColor = new Color(0.87f, 0.86f, 0.67f);

    [Header("Time Settings")]
    [SerializeField] private float decisionTimeLimit = 2.5f;
    [SerializeField] private TextMeshProUGUI timerText;
    private float currentTimer;
    private bool isTimerRunning;

    // Skip delay constant
    private const float SKIP_DELAY = 3f;
    private bool isSkipDelayActive = false;
    private float skipDelayTimer;

    private void Awake()
    {
        ValidateComponents();
        SetupAudioSource();

        // Explicitly find PracticeManager if not set via Instance
        if (practiceManager == null)
        {
            practiceManager = FindAnyObjectByType<PracticeManager>();

            if (practiceManager == null)
            {
                Debug.LogError("PracticeManager could not be found in the scene! This will cause issues with trial management.");

                // Create a temporary PracticeManager if absolutely necessary
                GameObject practiceManagerObject = new GameObject("PracticeManager");
                practiceManager = practiceManagerObject.AddComponent<PracticeManager>();
            }
        }

        // Similarly, find ExperimentManager if not set
        if (experimentManager == null)
        {
            experimentManager = FindAnyObjectByType<ExperimentManager>();

            if (experimentManager == null)
            {
                Debug.LogError("ExperimentManager could not be found in the scene! This will cause issues with experiment management.");

                // Create a temporary ExperimentManager if absolutely necessary
                GameObject experimentManagerObject = new GameObject("ExperimentManager");
                experimentManager = experimentManagerObject.AddComponent<ExperimentManager>();
            }
        }

        // Additional validation checks
        if (effortSpriteImage == null)
        {
            Debug.LogError("Effort Image component not assigned in DecisionManager!");
        }

        SetupButtonListeners();
        SetupKeyboardNavigation();
    }


    // New method for keyboard navigation setup
    private void SetupKeyboardNavigation()
    {
        // Add visual feedback components if they don't exist
        SetupButtonVisualFeedback(workButton);
        SetupButtonVisualFeedback(skipButton);

        // Set initial selection
        UpdateButtonSelection();
    }

    // New method to setup visual feedback
    private void SetupButtonVisualFeedback(Button button)
    {
        if (button != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }
        }
    }

    private void Update()
    {
        if (isSkipDelayActive)
        {
            UpdateSkipDelay();
            return;
        }

        if (isTimerRunning)
        {
            UpdateTimer();
        }

        // Only handle input if buttons are interactable and timer is running
        if (workButton != null && skipButton != null &&
            workButton.interactable && skipButton.interactable &&
            isTimerRunning)
        {
            // Handle left/right navigation
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                isWorkButtonSelected = !isWorkButtonSelected;
                UpdateButtonSelection();
            }

            // Handle decision confirmation
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
            {
                OnDecisionMade(isWorkButtonSelected);
            }
        }
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
        isSkipDelayActive = false;

        // Reset for the next decision
        SetupDecisionPhase();
    }

    private void UpdateTimer()
    {
        currentTimer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Time: {currentTimer:F0}";
        }

        if (currentTimer <= 0)
        {
            TimeExpired();
        }
    }

    private void TimeExpired()
    {
        isTimerRunning = false;
        DisableButtons();

        // Log the time expiration
        LogDecision(false);
        Debug.Log("Decision time expired - Moving to penalty scene");

        // Move to penalty scene
        SceneManager.LoadScene("TimePenalty");
    }

    // New method to update button visual state
    private void UpdateButtonSelection()
    {
        if (workButton != null && skipButton != null)
        {
            // Update work button
            Image workImage = workButton.GetComponent<Image>();
            if (workImage != null)
            {
                workImage.color = isWorkButtonSelected ? selectedColor : normalColor;
            }

            // Update skip button
            Image skipImage = skipButton.GetComponent<Image>();
            if (skipImage != null)
            {
                skipImage.color = !isWorkButtonSelected ? selectedColor : normalColor;
            }

            // Set EventSystem selection
            EventSystem.current.SetSelectedGameObject(
                isWorkButtonSelected ? workButton.gameObject : skipButton.gameObject
            );
        }
    }

    private void OnEnable()
    {
        // Ensure UI is properly initialized when object becomes active
        SetupDecisionPhase();

        // Reset selection when enabled
        isWorkButtonSelected = true;
        UpdateButtonSelection();
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void ValidateComponents()
    {
        // Find components if not assigned in inspector
        if (effortSpriteImage == null)
        {
            effortSpriteImage = transform.Find("EV Image")?.GetComponent<Image>();
            if (effortSpriteImage == null)
            {
                Debug.LogError("EV Image component not found in children!");
                return;
            }
        }

        // Ensure RectTransform is properly configured
        RectTransform rectTransform = effortSpriteImage.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(100f, 100f); // Match your UI requirements

        // Ensure Image component is properly configured
        effortSpriteImage.raycastTarget = false;
        effortSpriteImage.preserveAspect = true;
    }

    private void SetupButtonListeners()
    {
        if (workButton != null)
        {
            workButton.onClick.RemoveAllListeners();
            workButton.onClick.AddListener(() => OnDecisionMade(true));
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => OnDecisionMade(false));
        }
    }

    public void SetupDecisionPhase()
    {
        Debug.Log("DecisionManager: Setting up decision phase");

        if (effortSpriteImage == null || experimentManager == null)
        {
            Debug.LogError($"Critical components missing - Image: {effortSpriteImage != null}, ExperimentManager: {experimentManager != null}");
            return;
        }

        UpdateEffortSprite();
        EnableButtons();
        StartTimer();
    }

    private void StartTimer()
    {
        currentTimer = decisionTimeLimit;
        isTimerRunning = true;
        if (timerText != null)
        {
            timerText.text = $"Time: {currentTimer:F0}";
        }
    }

    public void UpdateEffortSprite()
    {
        Debug.Log("Starting UpdateEffortSprite method");

        // Check if image component exists
        if (effortSpriteImage == null)
        {
            Debug.LogError("Effort Image reference is null!");
            return;
        }

        // Add null checks at the beginning
        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager is null in UpdateEffortSprite!");
            return;
        }

        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager is null in UpdateEffortSprite!");
            return;
        }

        // Ensure sprite sources are available
        if (practiceManager.IsPracticeTrial())
        {
            if (practiceManager.GetCurrentPracticeTrialSprite() == null)
            {
                Debug.LogError("Practice trial sprite is null!");
                return;
            }
        }
        else
        {
            if (experimentManager.GetCurrentTrialSprite() == null)
            {
                Debug.LogError("Experiment trial sprite is null!");
                return;
            }
        }
        // Check if image component exists
        if (effortSpriteImage == null)
        {
            Debug.LogError("Effort Image reference is null!");
            return;
        }

        Sprite effortSprite = null;
        int effortLevel = 0;
        int pressesRequired = 0;

        // Determine sprite source based on trial type
        if (practiceManager.IsPracticeTrial())
        {
            // Get sprite from PracticeManager for practice trials
            effortSprite = practiceManager.GetCurrentPracticeTrialSprite();
            effortLevel = practiceManager.GetCurrentTrialEffortLevel();
            pressesRequired = practiceManager.GetCurrentTrialEffortLevel() * 10; // Example calculation
        }
        else
        {
            // Use ExperimentManager for formal trials
            effortSprite = experimentManager.GetCurrentTrialSprite();
            effortLevel = experimentManager.GetCurrentTrialEffortLevel();
            pressesRequired = experimentManager.GetCurrentTrialEV();
        }

        if (effortSprite == null)
        {
            Debug.LogError("Current trial sprite is null!");
            return;
        }

        // Store the sprite for cross-scene access
        if (!practiceManager.IsPracticeTrial())
        {
            experimentManager.StoreCurrentTrialSprite(effortSprite);

        }

        // Assign sprite and verify
        effortSpriteImage.sprite = effortSprite;

        // Configure image settings
        effortSpriteImage.enabled = true;
        effortSpriteImage.preserveAspect = true;
        effortSpriteImage.color = Color.white;

        // Update UI texts
        if (effortLevelText != null)
        {
            effortLevelText.text = $"Effort Level: {effortLevel}";
        }

        if (pressesRequiredText != null)
        {
            pressesRequiredText.text = $"Presses Required: {pressesRequired}";

            // Add practice trial information for practice trials
            if (practiceManager.IsPracticeTrial())
            {
                pressesRequiredText.text += $" (Practice Trial {practiceManager.GetCurrentPracticeTrialIndex() + 1})";
            }
        }
    }

    private void EnableButtons()
    {
        if (workButton != null)
        {
            workButton.interactable = true;
            // Reset selection state when enabling buttons
            isWorkButtonSelected = true;
            UpdateButtonSelection();
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
        if (!isTimerRunning) return;

        isTimerRunning = false;
        Debug.Log($"Decision made: {(workDecision ? "Work" : "Skip")}");

        // Play sound
        if (audioSource != null)
        {
            AudioClip clipToPlay = workDecision ? workButtonSound : skipButtonSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        Sprite currentSprite = effortSpriteImage.sprite;
        if (currentSprite == null)
        {
            Debug.LogError("No sprite selected for the trial!");
            return;
        }

        // Sprite name storage for cross-scene transfer
        PlayerPrefs.SetString("CurrentRewardSpriteName", currentSprite.name);

        // Explicitly set practice trial flag
        bool isPracticeTrial = practiceManager.IsPracticeTrial();
        PlayerPrefs.SetInt("IsPracticeTrial", isPracticeTrial ? 1 : 0);

        if (isPracticeTrial)
        {
            HandlePracticeTrial(workDecision, currentSprite);
        }
        else
        {
            HandleFormalExperimentTrial(workDecision);
        }
    }

    private void HandlePracticeTrial(bool workDecision, Sprite currentSprite)
    {
        Debug.Log($"HandlePracticeTrial called - Work Decision: {workDecision}");

        if (workDecision)
        {
            // Store the current sprite for cross-scene transfer
            PlayerPrefs.SetString("CurrentRewardSpriteName", currentSprite.name);

            // Ensure practice trial flag is set
            PlayerPrefs.SetInt("IsPracticeTrial", 1);

            SceneManager.LoadScene("GetReadyEveryTrial");
        }
        else
        {
            // Activate skip delay for practice trials
            ActivateSkipDelay();

            // Advance to next practice trial if skipped
            practiceManager.AdvancePracticeTrial();
        }
    }

    private void HandleFormalExperimentTrial(bool workDecision)
    {
        if (experimentManager != null)
        {
            experimentManager.HandleDecision(workDecision);
            DisableButtons();
            LogDecision(workDecision);
        }
    }

    // New method to handle sprite transfer between scenes
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GetReadyEveryTrial")
        {
            // Find the RewardSpawner and set its sprite
            RewardSpawner rewardSpawner = FindAnyObjectByType<RewardSpawner>();
            if (rewardSpawner != null)
            {
                rewardSpawner.SetRewardSprite(effortSpriteImage.sprite);
            }

            // Remove the event listener to prevent multiple calls
            SceneManager.sceneLoaded -= OnSceneLoaded;
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

        // Ensure the scene stays the same during skip delay
        Invoke("CompleteSkipDelay", SKIP_DELAY);
    }

    // Modify LogDecision to include more detailed practice trial logging
    private void LogDecision(bool workDecision)
    {
        string trialType = practiceManager.IsPracticeTrial() ? "Practice" : "Formal";
        int currentTrialIndex = practiceManager.IsPracticeTrial()
            ? practiceManager.GetCurrentPracticeTrialIndex()
            : -1;

        string logEntry = $"{System.DateTime.Now}: {trialType} Trial {currentTrialIndex} - Player decided to {(workDecision ? "Work" : "Skip")}";
        System.IO.File.AppendAllText("decision_log.txt", logEntry + System.Environment.NewLine);
        Debug.Log(logEntry);
    }
}