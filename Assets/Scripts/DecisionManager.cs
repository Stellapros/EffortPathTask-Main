using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the decision phase of each trial, where the participant chooses to work or skip.
/// </summary>
public class DecisionManager : MonoBehaviour
{
    [SerializeField] private Image effortImage;
    [SerializeField] private TextMeshProUGUI effortLevelText;
    [SerializeField] private Button workButton;
    [SerializeField] private Button skipButton;

    private ExperimentManager experimentManager;

    private void Start()
    {
        // Find the ExperimentManager in the scene
        experimentManager = ExperimentManager.Instance;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found in the scene!");
            return;
        }
        SetupButtons();
    }

    private void SetupButtons()
    {
        if (workButton != null)
        {
            workButton.onClick.AddListener(() => OnDecisionMade(true));
        }
        else
        {
            Debug.LogError("Work button is not assigned in the DecisionManager!");
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() => OnDecisionMade(false));
        }
        else
        {
            Debug.LogError("Skip button is not assigned in the DecisionManager!");
        }
    }

    /// <summary>
    /// Sets up the decision phase UI with the current trial's effort sprite and level.
    /// </summary>
    public void SetupDecisionPhase()
    {
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager is null in SetupDecisionPhase");
            return;
        }

        Sprite currentTrialSprite = experimentManager.GetCurrentTrialSprite();
        float currentTrialEV = experimentManager.GetCurrentTrialEV();

        if (effortImage != null)
        {
            effortImage.sprite = currentTrialSprite;
        }
        else
        {
            Debug.LogError("Effort Image is not assigned in DecisionManager!");
        }

        if (effortLevelText != null)
        {
            effortLevelText.text = $"Effort Level: {currentTrialEV}";
        }
        else
        {
            Debug.LogError("Effort Level Text is not assigned in DecisionManager!");
        }

        // Enable buttons
        if (workButton != null) workButton.interactable = true;
        if (skipButton != null) skipButton.interactable = true;
    }

    /// <summary>
    /// Handles the user's decision to work or skip.
    /// </summary>
    /// <param name="workDecision">True if the user decided to work, false if they decided to skip.</param>
    private void OnDecisionMade(bool workDecision)
    {
        Debug.Log($"Decision made: {(workDecision ? "Work" : "Skip")}");

        // Disable buttons to prevent multiple clicks
        if (workButton != null) workButton.interactable = false;
        if (skipButton != null) skipButton.interactable = false;

        if (experimentManager != null)
        {
            experimentManager.HandleDecision(workDecision);
        }
        else
        {
            Debug.LogError("ExperimentManager is null in OnDecisionMade!");
        }
    }

    private void OnDestroy()
    {
        if (workButton != null) workButton.onClick.RemoveAllListeners();
        if (skipButton != null) skipButton.onClick.RemoveAllListeners();
    }
}