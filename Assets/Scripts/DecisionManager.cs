using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DecisionManager : MonoBehaviour
{
    [SerializeField] private Image effortSpriteImage;
    [SerializeField] private TextMeshProUGUI effortLevelText;
    [SerializeField] private TextMeshProUGUI pressesRequiredText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button workButton;
    [SerializeField] private Button skipButton;
    private AudioSource audioSource;
    [SerializeField] private AudioClip workButtonSound;
    [SerializeField] private AudioClip skipButtonSound;
    private ExperimentManager experimentManager;

    // Added for keyboard navigation
    private bool? isWorkButtonSelected = true;
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color selectedColor = new Color(0.87f, 0.86f, 0.67f);

    [Header("Time Settings")]
    [SerializeField] private float decisionTimeLimit = 2.5f;
    [SerializeField] private TextMeshProUGUI timerText;
    private float currentTimer;
    private bool isTimerRunning;
    private float decisionStartTime;

    // Skip delay constant
    private const int SKIP_SCORE = 0;
    private const int NO_DECISION_SCORE = 0;
    private const float SKIP_DELAY = 3f;
    private const float NO_DECISION_PENALTY = 5f;
    private bool isSkipDelayActive = false;
    private float skipDelayTimer;

    private void Awake()
    {
        ValidateComponents();
        SetupAudioSource();

        // Find ExperimentManager if not set
        if (experimentManager == null)
        {
            experimentManager = FindAnyObjectByType<ExperimentManager>();

            if (experimentManager == null)
            {
                Debug.LogError("ExperimentManager could not be found in the scene!");
                return;
            }
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

        // Handle input only if buttons are interactable and timer is running
        if (workButton != null && skipButton != null &&
            workButton.interactable && skipButton.interactable &&
            isTimerRunning)
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
    //         OnDecisionMade(true); // Confirm Work decision immediately
    //     }
    //     // Handle right arrow key press for Skip
    //     else if (Input.GetKeyDown(KeyCode.RightArrow))
    //     {
    //         isWorkButtonSelected = false;
    //         UpdateButtonSelection();
    //         OnDecisionMade(false); // Confirm Skip decision immediately
    //     }
    // }
    private void HandleInput()
    {
        // Handle 'A' key press for Work
        if (Input.GetKeyDown(KeyCode.A))
        {
            isWorkButtonSelected = true;
            UpdateButtonSelection();
            OnDecisionMade(true); // Confirm Work decision immediately
        }
        // Handle 'D' key press for Skip
        else if (Input.GetKeyDown(KeyCode.D))
        {
            isWorkButtonSelected = false;
            UpdateButtonSelection();
            OnDecisionMade(false); // Confirm Skip decision immediately
        }
    }


    private void UpdateSkipDelay()
    {
        if (!isSkipDelayActive) return;

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

        // Calculate consistent decision time (full duration in this case)
        float decisionTime = decisionTimeLimit;

        // Get current block number
        int currentBlock = experimentManager.GetCurrentBlockNumber();
        int currentTrial = experimentManager.GetCurrentTrialIndex();
        int effortLevel = experimentManager.GetCurrentTrialEffortLevel();
        int requiredPresses = experimentManager.GetCurrentTrialEV();

        // Log as a "no decision" trial with the updated signature
        // LogDecision(false, true, currentBlock);

        // Log the decision outcome for timeout
        LogManager.Instance.LogDecisionOutcome(
            currentTrial,
            currentBlock,
            "NoDecision",
            false, // rewardCollected will be updated in the movement phase
decisionTime, // Use the consistent decision time
            0, // movementTime will be updated in the movement phase
            0, // buttonPresses will be updated in the movement phase
            effortLevel,
            requiredPresses
        );

        // Store current trial data before moving to penalty scene
        if (effortSpriteImage != null && effortSpriteImage.sprite != null)
        {
            PlayerPrefs.SetString("CurrentRewardSpriteName", effortSpriteImage.sprite.name);
        }

        experimentManager.MoveToNextTrial(); // Advance to next trial

        // Add 0 points for no decision
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(NO_DECISION_SCORE, !experimentManager.IsCurrentTrialPractice());
            Debug.Log("Time expired: No points awarded");
        }

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
                // Neutral color when no selection
                workImage.color = isWorkButtonSelected == true ? selectedColor : normalColor;
            }

            // Update skip button
            Image skipImage = skipButton.GetComponent<Image>();
            if (skipImage != null)
            {
                // Neutral color when no selection
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

    private void OnEnable()
    {
        // Ensure UI is properly initialized when object becomes active
        SetupDecisionPhase();

        // Reset selection when enabled
        isWorkButtonSelected = null;
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
        Debug.Log($"DecisionManager.SetupDecisionPhase - Current Trial Index: {experimentManager.GetCurrentTrialIndex()}");

        if (effortSpriteImage == null || experimentManager == null)
        {
            Debug.LogError($"Critical components missing - Image: {effortSpriteImage != null}, ExperimentManager: {experimentManager != null}");
            return;
        }

        // Reset UI state
        EnableButtons();
        UpdateEffortSprite();
        StartTimer();

        // Set instruction text
        if (instructionText != null)
        {
            instructionText.text = "A for Work / D for Skip";
        }

        // Reset selection state to ensure no default selection
        isWorkButtonSelected = null;
        UpdateButtonSelection();
    }

    private void StartTimer()
    {
        currentTimer = decisionTimeLimit;
        decisionStartTime = Time.time; // Record when the timer starts
        isTimerRunning = true;
        if (timerText != null)
        {
            timerText.text = $"Time: {currentTimer:F0}";
        }
    }

    public void UpdateEffortSprite()
    {
        if (effortSpriteImage == null || experimentManager == null)
        {
            Debug.LogError("Missing components for updating effort sprite!");
            return;
        }

        Sprite effortSprite = experimentManager.GetCurrentTrialSprite();
        int effortLevel = experimentManager.GetCurrentTrialEffortLevel();
        int pressesRequired = experimentManager.GetCurrentTrialEV();

        if (effortSprite == null)
        {
            Debug.LogError("Current trial sprite is null!");
            return;
        }

        experimentManager.StoreCurrentTrialSprite(effortSprite);

        effortSpriteImage.sprite = effortSprite;
        effortSpriteImage.enabled = true;
        effortSpriteImage.preserveAspect = true;
        effortSpriteImage.color = Color.white;

        UpdateUITexts(effortLevel, pressesRequired);
    }

    private void UpdateUITexts(int effortLevel, int pressesRequired)
    {
        if (effortLevelText != null)
        {
            effortLevelText.text = $"Effort Level: {effortLevel}";
        }

        if (pressesRequiredText != null)
        {
            pressesRequiredText.text = $"Presses Required: {pressesRequired}";
        }
    }

    private void EnableButtons()
    {
        if (workButton != null)
        {
            workButton.interactable = true;
            // Reset selection state when enabling buttons
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

    // private void OnDecisionMade(bool workDecision)
    // {
    //     if (!isTimerRunning) return;

    //     isTimerRunning = false;
    //     Debug.Log($"Decision made: {(workDecision ? "Work" : "Skip")}");

    //     // Play sound
    //     if (audioSource != null)
    //     {
    //         AudioClip clipToPlay = workDecision ? workButtonSound : skipButtonSound;
    //         if (clipToPlay != null)
    //         {
    //             audioSource.PlayOneShot(clipToPlay);
    //         }
    //     }

    //     Sprite currentSprite = effortSpriteImage.sprite;
    //     if (currentSprite == null)
    //     {
    //         Debug.LogError("No sprite selected for the trial!");
    //         return;
    //     }

    //     // Sprite name storage for cross-scene transfer
    //     PlayerPrefs.SetString("CurrentRewardSpriteName", currentSprite.name);

    //     // Handle decision through ExperimentManager
    //     if (experimentManager != null)
    //     {
    //         experimentManager.HandleDecision(workDecision);
    //         DisableButtons();
    //         LogDecision(workDecision);
    //     }
    // }

    // In OnDecisionMade method, remove the LogDecisionOutcome call
    private void OnDecisionMade(bool workDecision)
    {
        if (!isTimerRunning) return;

        Debug.Log($"OnDecisionMade called - workDecision: {workDecision}");
        isTimerRunning = false;
        DisableButtons();

        // Calculate decision time consistently
        float decisionTime = Time.time - decisionStartTime;

        // Play sound
        if (audioSource != null)
        {
            AudioClip clipToPlay = workDecision ? workButtonSound : skipButtonSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
        }

        // Store sprite name before scene transition
        if (effortSpriteImage != null && effortSpriteImage.sprite != null)
        {
            PlayerPrefs.SetString("CurrentRewardSpriteName", effortSpriteImage.sprite.name);
            PlayerPrefs.Save();
        }

        // Get current block number from ExperimentManager
        int currentBlock = experimentManager.GetCurrentBlockNumber();
        int currentTrial = experimentManager.GetCurrentTrialIndex();
        int effortLevel = experimentManager.GetCurrentTrialEffortLevel();
        int requiredPresses = experimentManager.GetCurrentTrialEV();

        // Store decision data in PlayerPrefs for later logging when outcome is known
        PlayerPrefs.SetString("DecisionType", workDecision ? "Work" : "Skip");
        PlayerPrefs.SetFloat("DecisionTime", decisionTimeLimit - currentTimer);
        PlayerPrefs.SetInt("CurrentBlock", currentBlock);
        PlayerPrefs.SetInt("CurrentTrial", currentTrial);
        PlayerPrefs.SetInt("EffortLevel", effortLevel);
        PlayerPrefs.SetInt("RequiredPresses", requiredPresses);
        PlayerPrefs.Save();

        // Handle the decision through ExperimentManager first - pass the decision time
        if (experimentManager != null)
        {
            Debug.Log("Calling OnDecisionMade.HandleDecision");
            experimentManager.HandleDecision(workDecision, decisionTime); // Pass the decision time 
        }

        if (workDecision)
        {
            Debug.Log("Work decision - transitioning to GridWorld");
            StartCoroutine(DelayedSceneTransition("GridWorld", 0.1f));
        }
        else
        {
            Debug.Log("Skip decision - handling skip logic");
            // Add skip score immediately
            if (ScoreManager.Instance != null)
            {
                Debug.Log($"Adding skip score: {SKIP_SCORE} points");
                ScoreManager.Instance.AddScore(SKIP_SCORE, !experimentManager.IsCurrentTrialPractice());
            }
            // Log Skip outcome immediately since we know the result
            LogManager.Instance.LogDecisionOutcome(
                currentTrial,
                currentBlock,
                "Skip",
                false, // rewardCollected 
                       // decisionTimeLimit - currentTimer,
                decisionTime, // Use the consistent decision time
                0, // movementTime
                0, // buttonPresses
                effortLevel,
                requiredPresses
            );
            // Then activate skip delay
            ActivateSkipDelay();
        }
    }

    private System.Collections.IEnumerator DelayedSceneTransition(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
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
        Debug.Log("ActivateSkipDelay called");
        isSkipDelayActive = true;
        skipDelayTimer = SKIP_DELAY;

        if (timerText != null)
        {
            timerText.text = $"Waiting: {skipDelayTimer:F1}";
        }
    }

    private void CompleteSkipDelay()
    {
        Debug.Log("CompleteSkipDelay called");
        isSkipDelayActive = false;

        if (experimentManager != null)
        {
            experimentManager.MoveToNextTrial();
        }

        // Reset for the next decision
        SetupDecisionPhase();
    }

    // Modify LogDecision to include more detailed practice trial logging
    // private void LogDecision(bool workDecision, bool isTimeExpired, int blockNumber)
    // {
    //     if (experimentManager == null) return;

    //     string decisionType = isTimeExpired ? "NoDecision" : (workDecision ? "Work" : "Skip");
    //     float decisionTime = decisionTimeLimit - currentTimer;
    //     int currentTrial = experimentManager.GetCurrentTrialIndex();

    //     // Log to LogManager
    //     // LogManager.Instance?.LogDecisionPhaseStart(currentTrial);

    //     var parameters = new Dictionary<string, string>
    // {
    //     {"TrialNumber", currentTrial.ToString()},
    //     {"BlockNumber", blockNumber.ToString()},  // Now included in all decision logs
    //     {"DecisionType", decisionType},
    //     {"DecisionTime", decisionTime.ToString("F3")},
    //     {"EffortLevel", experimentManager.GetCurrentTrialEffortLevel().ToString()},
    //     {"RequiredPresses", experimentManager.GetCurrentTrialEV().ToString()}
    // };

    //     if (isTimeExpired)
    //     {
    //         LogManager.Instance?.LogEvent("Decision", parameters);
    //         LogManager.Instance?.LogPenaltyApplied(currentTrial, "NoDecision", NO_DECISION_PENALTY);
    //     }
    //     else if (!workDecision)
    //     {
    //         LogManager.Instance?.LogSkipDecision(currentTrial, decisionTime);
    //         LogManager.Instance?.LogPenaltyApplied(currentTrial, "Skip", SKIP_DELAY);
    //     }
    //     else
    //     {
    //         LogManager.Instance?.LogWorkDecision(currentTrial, decisionTime);
    //     }
    // }
}