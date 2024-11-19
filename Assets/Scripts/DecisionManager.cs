using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class DecisionManager : MonoBehaviour
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

        // Initialize managers
        experimentManager = ExperimentManager.Instance;
        practiceManager = PracticeManager.Instance;

        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found!");
        }

        // Verify UI setup
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
            timerText.text = $"Waiting: {skipDelayTimer:F1}s";
        }

        if (skipDelayTimer <= 0)
        {
            CompleteSkipDelay();
        }
    }

    private void CompleteSkipDelay()
    {
        isSkipDelayActive = false;
        SetupDecisionPhase(); // Reset for next decision
    }

    private void UpdateTimer()
    {
        currentTimer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Time: {currentTimer:F1}s";
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

    // private void SetupForPractice()
    // {
    //     // Show practice-specific UI elements
    //     if (pressesRequiredText != null)
    //     {
    //         var practiceManager = PracticeManager.Instance;
    //         if (practiceManager != null)
    //         {
    //             pressesRequiredText.text = $"Practice Trial {practiceManager.GetCurrentPracticeTrialIndex() + 1} of {practiceManager.GetTotalPracticeTrials()}";
    //         }
    //     }
    //     EnableButtons();
    // }

    // private void SetupForFormalExperiment()
    // {
    //     // Setup for the real experiment
    // }

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
            timerText.text = $"Time: {currentTimer:F1}s";
        }
    }

    private void UpdateEffortSprite()
    {
        Debug.Log("Starting UpdateEffortSprite method");

        // Check if image component exists
        if (effortSpriteImage == null)
        {
            Debug.LogError("Effort Image reference is null!");
            return;
        }

        // Check if ExperimentManager exists
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager is null!");
            return;
        }

        // Get and verify sprite
        Sprite effortSprite = experimentManager.GetCurrentTrialSprite();

        if (effortSprite == null)
        {
            Debug.LogError("Current trial sprite is null!");
            return;
        }

        // Get trial information
        int effortLevel = experimentManager.GetCurrentTrialEffortLevel();
        int pressesRequired = experimentManager.GetCurrentTrialEV();

        Debug.Log($"Sprite info - Width: {effortSprite.rect.width}, Height: {effortSprite.rect.height}");
        Debug.Log($"Image component - Enabled: {effortSpriteImage.enabled}, Color: {effortSpriteImage.color}");

        // Assign sprite and verify
        effortSpriteImage.sprite = effortSprite;

        // Configure image settings
        effortSpriteImage.enabled = true;
        effortSpriteImage.preserveAspect = true;
        effortSpriteImage.color = Color.white; // Ensure full opacity

        // Update UI texts
        if (effortLevelText != null)
        {
            effortLevelText.text = $"Effort Level: {effortLevel}";
        }

        if (pressesRequiredText != null)
        {
            pressesRequiredText.text = $"Presses Required: {pressesRequired}";
        }

        // Verify RectTransform properties
        RectTransform rectTransform = effortSpriteImage.rectTransform;
        Debug.Log($"RectTransform - Position: {rectTransform.position}, Size: {rectTransform.rect.size}");
        Debug.Log($"RectTransform - Scale: {rectTransform.localScale}");
        Debug.Log("Finished UpdateEffortSprite method");
    }

    // private void EnableButtons()
    // {
    //     if (workButton != null) workButton.interactable = true;
    //     if (skipButton != null) skipButton.interactable = true;
    // }
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
        if (!isTimerRunning) return; // Prevent multiple decisions

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

        if (experimentManager != null)
        {
            int effortLevel = experimentManager.GetCurrentTrialEffortLevel();
            int pressesRequired = experimentManager.GetCurrentTrialEV();
            float trialDuration = experimentManager.GetTrialDuration();
            Sprite currentSprite = effortSpriteImage.sprite;

            if (currentSprite == null)
            {
                Debug.LogError("Current sprite in effortSpriteImage is null!");
                return;
            }

            // // Start GridWorld initialization as a coroutine
            // StartCoroutine(GridWorldManager.Instance.InitializeGridWorld(
            //     trialDuration
            //     // effortLevel,
            //     // pressesRequired,
            //     // currentSprite,
            // ));

            experimentManager.HandleDecision(workDecision);
            DisableButtons();
            LogDecision(workDecision);

            // Different logic for work and skip
            if (workDecision)
            {
                // Move to GetReadyEveryTrial scene for work
                SceneManager.LoadScene("GetReadyEveryTrial");
            }
            else
            {
                // Activate skip delay in current scene
                ActivateSkipDelay();
            }
        }
    }

    private void ActivateSkipDelay()
    {
        isSkipDelayActive = true;
        skipDelayTimer = SKIP_DELAY;

        if (timerText != null)
        {
            timerText.text = $"Waiting: {skipDelayTimer:F1}s";
        }
    }

    private void LogDecision(bool workDecision)
    {
        string logEntry = $"{System.DateTime.Now}: Player decided to {(workDecision ? "Work" : "Skip")}";
        System.IO.File.AppendAllText("decision_log.txt", logEntry + System.Environment.NewLine);
        Debug.Log(logEntry);
    }
}