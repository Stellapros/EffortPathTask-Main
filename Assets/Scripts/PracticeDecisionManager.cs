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
    [SerializeField] private TextMeshProUGUI timerText; // Added timer text
    [SerializeField] private TextMeshProUGUI instructionText;

    [SerializeField] private Button workButton;
    [SerializeField] private Button skipButton;
    private AudioSource audioSource;
    [SerializeField] private AudioClip workButtonSound;
    [SerializeField] private AudioClip skipButtonSound;
    private PracticeManager practiceManager;

    // Added for keyboard navigation
    private bool? isWorkButtonSelected = true;
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color selectedColor = new Color(0.87f, 0.86f, 0.67f);

    // Skip delay constant
    private const int SKIP_SCORE = 0;
    private const float SKIP_DELAY = 3f;
    private bool isSkipDelayActive = false;
    private float skipDelayTimer;

    // New flag to prevent double processing
    private bool hasProcessedCurrentTrial = false;

    private void Awake()
    {
        ValidateComponents();
        SetupAudioSource();
        FindPracticeManager();
        SetupKeyboardNavigation();
    }

    private void SetupKeyboardNavigation()
    {
        // Add visual feedback components
        SetupButtonVisualFeedback(workButton);
        SetupButtonVisualFeedback(skipButton);

        // Set initial selection
        UpdateButtonSelection();
    }

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

        // Handle input only if buttons are interactable
        if (workButton != null && skipButton != null &&
            workButton.interactable && skipButton.interactable)
        {
            HandleInput();
        }
    }

    // private void HandleInput()
    // {
    //     // Handle left arrow key press for Work
    //     if (Input.GetKeyDown(KeyCode.LeftArrow))
    //     {
    //         isWorkButtonSelected = true;
    //         UpdateButtonSelection();
    //         OnDecisionMade(true);
    //     }
    //     // Handle right arrow key press for Skip
    //     else if (Input.GetKeyDown(KeyCode.RightArrow))
    //     {
    //         isWorkButtonSelected = false;
    //         UpdateButtonSelection();
    //         OnDecisionMade(false);
    //     }
    // }

    private void HandleInput()
    {
        // Handle 'A' key press for Work
        if (Input.GetKeyDown(KeyCode.A))
        {
            isWorkButtonSelected = true;
            UpdateButtonSelection();
            OnDecisionMade(true);
        }
        // Handle 'D' key press for Skip
        else if (Input.GetKeyDown(KeyCode.D))
        {
            isWorkButtonSelected = false;
            UpdateButtonSelection();
            OnDecisionMade(false);
        }
    }

    private void UpdateButtonSelection()
    {
        if (workButton != null && skipButton != null)
        {
            // Update work button
            Image workImage = workButton.GetComponent<Image>();
            if (workImage != null)
            {
                workImage.color = isWorkButtonSelected == true ? selectedColor : normalColor;
            }

            // Update skip button
            Image skipImage = skipButton.GetComponent<Image>();
            if (skipImage != null)
            {
                skipImage.color = isWorkButtonSelected == false ? selectedColor : normalColor;
            }

            // Clear selection when no button is selected
            if (isWorkButtonSelected == null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            else
            {
                // Set selection when a button is chosen
                EventSystem.current.SetSelectedGameObject(
                    isWorkButtonSelected.Value ? workButton.gameObject : skipButton.gameObject
                );
            }
        }
    }

    private void FindPracticeManager()
    {
        practiceManager = FindAnyObjectByType<PracticeManager>();
        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager could not be found in the scene!");
            practiceManager = GetComponent<PracticeManager>();
        }
    }

    private void SetupAudioSource()
    {
        audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
    }

    private void ValidateComponents()
    {
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
        SetupDecisionPhase();
        isWorkButtonSelected = null;
        UpdateButtonSelection();
    }

    public void SetupDecisionPhase()
    {
        Debug.Log($"SetupDecisionPhase CALLED");
        Debug.Log($"Current Practice Trial Index: {practiceManager.GetCurrentPracticeTrialIndex()}");

        if (practiceManager == null)
        {
            Debug.LogError("PracticeManager is not assigned!");
            practiceManager = FindAnyObjectByType<PracticeManager>();

            if (practiceManager == null)
            {
                Debug.LogError("Cannot find PracticeManager. Cannot start practice phase.");
                return;
            }
        }

        if (practiceManager.GetCurrentPracticeTrialIndex() < 0)
        {
            practiceManager.StartPracticeMode();
        }

        UpdateEffortSprite();
        EnableButtons();

        // Set instruction text
        if (instructionText != null)
        {
            instructionText.text = "A for Work / D for Skip";
        }

        // Log current trial details
        PracticeManager.PracticeTrial currentTrial = practiceManager.GetCurrentPracticeTrial();
        if (currentTrial != null)
        {
            Debug.Log($"Current Trial Details in SetupDecisionPhase:");
            Debug.Log($"- Effort Level: {currentTrial.effortLevel}");
            Debug.Log($"- Reward Value: {currentTrial.rewardValue}");
        }
        else
        {
            Debug.LogError("No current practice trial found!");
        }
    }

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
            practiceManager.StartPracticeMode();
            return;
        }

        Sprite effortSprite = practiceManager.GetCurrentPracticeTrialSprite();
        int effortLevel = practiceManager.GetCurrentTrialEffortLevel();
        int pressesRequired = GetPracticePressesByEffortLevel(effortLevel);

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
            if (effortLevelText != null)
            {
                effortLevelText.text = $"Effort Level: {effortLevel} ({GetEffortLevelDescription(effortLevel)})";
                Debug.Log($"Setting Effort Level Text to: {effortLevelText.text}");
            }

            if (pressesRequiredText != null)
            {
                pressesRequiredText.text = $"Presses Required: {pressesRequired}";
                Debug.Log($"Setting Presses Required Text to: {pressesRequiredText.text}");
            }
        }
        else
        {
            Debug.LogError("No current practice trial found!");
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

        // Add these debug lines
        Debug.Log($"OnDecisionMade called. WorkDecision: {workDecision}");
        Debug.Log($"Current Score Before Adding: {PracticeScoreManager.Instance?.GetCurrentScore()}");

        if (workDecision)
        {
            StartCoroutine(DelayedSceneTransition("GetReadyEveryTrialPractice", 0.1f));
            // // Flag to indicate that the trial was not skipped,
            // PlayerPrefs.SetInt("SkippedTrial", 0);
        }
        else
        {
            // Only add score here if absolutely necessary
            Debug.Log("Skip decision - adding 1 point");
            PracticeScoreManager.Instance?.AddScore(SKIP_SCORE);


            ActivateSkipDelay();
        }
        Debug.Log($"Current Score After Adding: {PracticeScoreManager.Instance?.GetCurrentScore()}");
    }

    private System.Collections.IEnumerator DelayedSceneTransition(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    private void ActivateSkipDelay()
    {
        isSkipDelayActive = true;
        skipDelayTimer = SKIP_DELAY;

        if (timerText != null)
        {
            timerText.text = $"Waiting: {skipDelayTimer:F1}";
        }
        else
        {
            Debug.LogWarning("Timer Text is not assigned!");
        }
    }

    private void UpdateSkipDelay()
    {
        skipDelayTimer -= Time.deltaTime;

        if (timerText != null)
        {
            timerText.text = $"Waiting: {skipDelayTimer:F1}";
        }

        if (skipDelayTimer <= 0)
        {
            CompleteSkipDelay();
        }
    }

    private void CompleteSkipDelay()
    {
        if (hasProcessedCurrentTrial)
        {
            Debug.Log("Trial already processed. Skipping duplicate processing.");
            return;
        }

        isSkipDelayActive = false;
        hasProcessedCurrentTrial = true;

        // Clear timer text
        if (timerText != null)
        {
            timerText.text = "";
        }

        Debug.Log($"CompleteSkipDelay - Current Practice Trial Index BEFORE Completion: {practiceManager.GetCurrentPracticeTrialIndex()}");
        practiceManager.HandleGridWorldOutcome(true);
        Debug.Log($"CompleteSkipDelay - Current Practice Trial Index AFTER Completion: {practiceManager.GetCurrentPracticeTrialIndex()}");
    }

    private void EnableButtons()
    {
        if (workButton != null)
        {
            workButton.interactable = true;
            isWorkButtonSelected = null;
            UpdateButtonSelection();
        }
        if (skipButton != null) skipButton.interactable = true;
    }

    private void DisableButtons()
    {
        if (workButton != null) workButton.interactable = false;
        if (skipButton != null) skipButton.interactable = false;
    }

    private void LogDecision(bool workDecision)
    {
        string trialType = "Practice";
        int currentTrialIndex = practiceManager.GetCurrentPracticeTrialIndex();

        string logEntry = $"{System.DateTime.Now}: {trialType} Trial {currentTrialIndex + 1} - Player decided to {(workDecision ? "Work" : "Skip")}";
        System.IO.File.AppendAllText("practice_decision_log.txt", logEntry + System.Environment.NewLine);
        Debug.Log(logEntry);
    }
}