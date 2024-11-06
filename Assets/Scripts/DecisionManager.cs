using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;


public class DecisionManager : MonoBehaviour
{
    [SerializeField] private Image effortSpriteImage;
    [SerializeField] private TextMeshProUGUI effortLevelText;
    [SerializeField] private TextMeshProUGUI pressesRequiredText; // New field for presses required
    [SerializeField] private Button workButton;
    [SerializeField] private Button skipButton;
    private AudioSource audioSource;
    [SerializeField] private AudioClip workButtonSound;
    [SerializeField] private AudioClip skipButtonSound;
    private TourManager tourManager;
    private ExperimentManager experimentManager;
    private PracticeManager practiceManager;

private void Awake()
    {
        ValidateComponents();
        SetupAudioSource();
        
        // Initialize managers
        experimentManager = ExperimentManager.Instance;
        tourManager = TourManager.Instance;
        practiceManager = PracticeManager.Instance;

        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found!");
        }

        SetupButtonListeners();
    }

    private void Start()
    {
        InitializeGameMode();
    }

    private void InitializeGameMode()
    {
        if (tourManager != null && tourManager.IsTourActive())
        {
            SetupForTour();
        }
        else if (practiceManager != null && practiceManager.IsPracticeTrial())
        {
            SetupForPractice();
        }
        else
        {
            SetupForFormalExperiment();
        }
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
        if (workButton == null)
            workButton = GameObject.Find("WorkButton")?.GetComponent<Button>();
        
        if (skipButton == null)
            skipButton = GameObject.Find("SkipButton")?.GetComponent<Button>();
        
        if (effortSpriteImage == null)
            effortSpriteImage = GameObject.Find("EffortLevelImage")?.GetComponent<Image>();
        
        // Log warnings for missing components
        if (workButton == null)
            Debug.LogError("WorkButton not found or not assigned!");
        if (skipButton == null)
            Debug.LogError("SkipButton not found or not assigned!");
        if (effortSpriteImage == null)
            Debug.LogError("EffortLevelImage not found or not assigned!");
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

        // Only setup tour-specific highlights if tour is active
        if (tourManager != null && tourManager.IsTourActive() && tourManager.GetCurrentStepIndex() == 4)
        {
            StartCoroutine(HighlightButtons());
        }
    }

    private void SetupForTour()
    {
        if (tourManager == null) return;

        int currentStep = tourManager.GetCurrentStepIndex();
        
        switch (currentStep)
        {
            case 4: // Work button step
                if (workButton != null) workButton.interactable = true;
                if (skipButton != null) skipButton.interactable = false;
                if (workButton != null) HighlightButton(workButton);
                break;
            
            case 7: // Skip button step
                if (workButton != null) workButton.interactable = false;
                if (skipButton != null) skipButton.interactable = true;
                if (skipButton != null) HighlightButton(skipButton);
                break;
            
            default:
                EnableButtons();
                break;
        }
    }

    private void SetupForPractice()
    {
        // Show practice-specific UI elements
        if (pressesRequiredText != null)
        {
            var practiceManager = PracticeManager.Instance;
            if (practiceManager != null)
            {
                pressesRequiredText.text = $"Practice Trial {practiceManager.GetCurrentPracticeTrialIndex() + 1} of {practiceManager.GetTotalPracticeTrials()}";
            }
        }
        EnableButtons();
    }

    private void SetupForFormalExperiment()
    {
        // Setup for the real experiment
    }

    private void HighlightWorkButton()
    {
        // Add code to visually highlight the 'Work' button
    }

    private IEnumerator HighlightButtons()
    {
        yield return new WaitForSeconds(1f);
        HighlightButton(workButton);
        yield return new WaitForSeconds(2f);
        HighlightButton(skipButton);
    }

    private void HighlightButton(Button button)
    {
        Image buttonImage = button.GetComponent<Image>();
        Color originalColor = buttonImage.color;
        buttonImage.color = Color.yellow;
        StartCoroutine(ResetButtonColor(buttonImage, originalColor));
        StartCoroutine(PulseButton(buttonImage));
    }

    private IEnumerator PulseButton(Image buttonImage)
    {
        Color originalColor = buttonImage.color;
        while (true)
        {
            // Pulse effect
            for (float t = 0; t < 1; t += Time.deltaTime)
            {
                buttonImage.color = Color.Lerp(originalColor, Color.yellow, (Mathf.Sin(t * 4) + 1) / 2);
                yield return null;
            }
        }
    }

    private IEnumerator ResetButtonColor(Image buttonImage, Color originalColor)
    {
        yield return new WaitForSeconds(1f);
        buttonImage.color = originalColor;
    }

    public void SetupDecisionPhase()
    {
        Debug.Log("DecisionManager: Setting up decision phase");
        UpdateEffortSprite();
        EnableButtons();
    }

    private void UpdateEffortSprite()
    {
        if (experimentManager != null)
        {
            Sprite effortSprite = experimentManager.GetCurrentTrialSprite();
            int effortLevel = experimentManager.GetCurrentTrialEffortLevel();
            int pressesRequired = experimentManager.GetCurrentTrialEV();

            Debug.Log($"UpdateEffortSprite: Effort Level = {effortLevel}, Presses Required = {pressesRequired}");

            if (effortSpriteImage != null && effortSprite != null)
            {
                effortSpriteImage.sprite = effortSprite;
            }

            if (effortLevelText != null)
            {
                effortLevelText.text = $"Effort Level: {effortLevel}";
            }

            if (pressesRequiredText != null)
            {
                pressesRequiredText.text = $"Presses Required: {pressesRequired}";
            }
        }
        else
        {
            Debug.LogError("ExperimentManager is null in UpdateEffortSprite!");
        }
    }

    private void EnableButtons()
    {
        if (workButton != null) workButton.interactable = true;
        if (skipButton != null) skipButton.interactable = true;
    }

    private void DisableButtons()
    {
        if (workButton != null) workButton.interactable = false;
        if (skipButton != null) skipButton.interactable = false;
    }

    private void OnDecisionMade(bool workDecision)
    {
        if (tourManager != null && tourManager.IsTourActive())
        {
            DisableButtons();
            if (workDecision && tourManager.GetCurrentStepIndex() == 4)
            {
                // Handle work button during tour
                StartCoroutine(DelayedTourProgress());
            }
            else if (!workDecision && tourManager.GetCurrentStepIndex() == 7)
            {
                // Handle skip button during tour
                StartCoroutine(HandleSkipTourStep());
            }
        }
        else if (experimentManager != null)
        {
            experimentManager.HandleDecision(workDecision);
            DisableButtons();
            LogDecision(workDecision);
        }
    }

private IEnumerator DelayedTourProgress()
{
    yield return new WaitForSeconds(0.5f);
    tourManager.ProcessNextStep();
}

private IEnumerator HandleSkipTourStep()
{
    yield return new WaitForSeconds(3f); // Wait time for skip action
    tourManager.ProcessNextStep();
}
    private void LogDecision(bool workDecision)
    {
        string logEntry = $"{System.DateTime.Now}: Player decided to {(workDecision ? "Work" : "Skip")}";
        System.IO.File.AppendAllText("decision_log.txt", logEntry + System.Environment.NewLine);
        Debug.Log(logEntry);
    }
}