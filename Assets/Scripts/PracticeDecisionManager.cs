using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Collections;

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

    private float decisionPhaseStartTime;

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

    // Add this to track skip state explicitly
    private bool isSkipInProgress = false;

    private void Awake()
    {
        ValidateComponents();
        SetupAudioSource();
        FindPracticeManager();
        SetupKeyboardNavigation();
        HideCursor();
    }


    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
        decisionPhaseStartTime = Time.time;
        SetupDecisionPhase();
        isWorkButtonSelected = null;
        UpdateButtonSelection();

        // Reset skip flags on enable
        isSkipInProgress = false;
        hasProcessedCurrentTrial = false;
    }

    public void SetupDecisionPhase()
    {
        Debug.Log($"SetupDecisionPhase CALLED");
        Debug.Log($"Current Practice Trial Index: {practiceManager.GetCurrentPracticeTrialIndex()}"); // Add 1 for display

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
                // Use PracticeManager to get the correct presses required
                pressesRequired = practiceManager.GetCurrentTrialPressesRequired();
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
            case 1: return "Apple - 1 Press";
            case 2: return "Grapes - 3 Presses";
            case 3: return "Watermelon - 5 Presses";
            default:
                Debug.LogWarning($"Unexpected effort level: {effortLevel}");
                return $"Unknown - {effortLevel} Presses";
        }
    }

    private int GetPracticePressesByEffortLevel(int effortLevel)
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

    private void OnDecisionMade(bool workDecision)
    {
    // Generate a transaction ID for this decision
    string transactionId = System.Guid.NewGuid().ToString();

    // Store the transaction ID
    PlayerPrefs.SetString("CurrentDecisionTransactionId", transactionId);

    // IMPORTANT: Store the decision type to prevent confusion
    PlayerPrefs.SetInt("IsWorkDecision", workDecision ? 1 : 0);
    
    // CRITICAL FIX: Store the current trial index at decision time
    int currentTrialIndex = PracticeManager.Instance.GetCurrentPracticeTrialIndex();
    PlayerPrefs.SetInt("CurrentDecisionTrialIndex", currentTrialIndex);

    // IMPORTANT: Add a timestamp to help with debugging and race condition detection
    PlayerPrefs.SetString("LastDecisionTimestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));

    // IMPORTANT: Reset the WorkDecisionProcessed flag at the start of a new decision
    PlayerPrefs.SetInt("WorkDecisionProcessed", 0);
    PlayerPrefs.Save();

        float decisionRT = Time.time - decisionPhaseStartTime;

        // Store decision type and time for practice trials
        PlayerPrefs.SetString("DecisionType", workDecision ? "Work" : "Skip");
        PlayerPrefs.SetFloat("PracticeDecisionTime", decisionRT);
        PlayerPrefs.Save();

        int trialIndex = PracticeManager.Instance.GetCurrentPracticeTrialIndex();
        const int PRACTICE_BLOCK_NUMBER = -1;

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

        // Get practice type from the PracticeManager
        string practiceType = GetCurrentPracticeType();

        if (workDecision)
        {
            // Clear any skip flags to make sure skip flow doesn't get triggered
            isSkipInProgress = false;
            isSkipDelayActive = false;

            Debug.Log($"[DECISION-{transactionId}] Work decision made in practice trial {trialIndex}, starting effort task");

            // For Work decisions, store transaction ID so it can be used by GridWorldManager
            PlayerPrefs.SetString("WorkDecisionTransactionId", transactionId);

            // CRITICAL: Force PlayerPrefs save for WebGL
            PlayerPrefs.Save();

            // IMPORTANT: Add a short delay before transitioning to ensure PlayerPrefs are saved
            // Increased from 0.5f to 0.8f for better WebGL compatibility
            StartCoroutine(DelayedSceneTransition("GetReadyEveryTrialPractice", 0.8f));
        }
        else
        {
            // Mark that skip is in progress - WebGL fix
            isSkipInProgress = true;

            // For Skip decisions, log the outcome immediately
            int effortLevel = PracticeManager.Instance.GetCurrentTrialEffortLevel();
            int requiredPresses = PracticeManager.Instance.GetCurrentTrialPressesRequired();

            LogManager.Instance.LogDecisionOutcome(
                trialIndex,
                PRACTICE_BLOCK_NUMBER,
                "Skip",
                false, // rewardCollected
                decisionRT,
                0f, // movementDuration
                0, // buttonPresses
                effortLevel,
                requiredPresses,
                true, // skipAdjustment
                "-", // pressData
                -1f, // timePerPress
                SKIP_SCORE, // points
                0, // loggedTotalScore (default to 0)
                0, // loggedPracticeScore (default to 0)
                practiceType // Pass the practice type
            );

            Debug.Log($"[DECISION-{transactionId}] Skip decision - adding 0 point");
            PracticeScoreManager.Instance?.AddScore(SKIP_SCORE);
            ActivateSkipDelay();
        }
    }

    private string GetCurrentPracticeType()
    {
        // Get the current practice block type from PlayerPrefs
        string blockTypeString = PlayerPrefs.GetString("CurrentPracticeBlockType", "EqualRatio");

        // Return the practice type
        return blockTypeString;
    }

    private IEnumerator DelayedSceneTransition(string sceneName, float delay)
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

        Debug.Log("CompleteSkipDelay - Moving to next practice trial");

        // Get the transaction ID stored earlier
        string transactionId = PlayerPrefs.GetString("CurrentDecisionTransactionId", "");

        // Pass the transaction ID to ensure idempotent operation
        if (practiceManager != null && isSkipInProgress)
        {
            Debug.Log($"Calling HandleGridWorldOutcome for skip with transactionId: {transactionId}");

            // CRITICAL FIX: Use a coroutine with short delay to ensure consistent state
            StartCoroutine(DelayedHandleGridWorldOutcome(true, transactionId, 0.2f));
            isSkipInProgress = false;
        }
        else
        {
            Debug.LogError("PracticeManager is null or skip is not in progress in CompleteSkipDelay");
        }
    }

    private IEnumerator DelayedHandleGridWorldOutcome(bool isSkip, string transactionId, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (practiceManager != null)
        {
            practiceManager.HandleGridWorldOutcome(isSkip, transactionId);
        }
        else
        {
            Debug.LogError("PracticeManager is null in delayed GridWorldOutcome call");
        }
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
}