using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class DecisionManager : MonoBehaviour
{
    [SerializeField] private Image effortSpriteImage;
    [SerializeField] private TextMeshProUGUI effortLevelText;
    [SerializeField] private Button workButton;
    [SerializeField] private Button skipButton;

    // New audio-related fields
    private AudioSource audioSource;
    [SerializeField] private AudioClip workButtonSound;
    [SerializeField] private AudioClip skipButtonSound;



    private ExperimentManager experimentManager;

    private void Awake()
    {
        experimentManager = ExperimentManager.Instance;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found!");
        }
        SetupButtonListeners();

        // Ensure we have an AudioSource
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void SetupButtonListeners()
    {
        if (workButton != null)
        {
            workButton.onClick.RemoveAllListeners();
            workButton.onClick.AddListener(() => OnDecisionMade(true));
        }
        else
        {
            Debug.LogError("Work button is not assigned!");
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => OnDecisionMade(false));
        }
        else
        {
            Debug.LogError("Skip button is not assigned!");
        }
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
            float effortValue = experimentManager.GetCurrentTrialEV();

            if (effortSpriteImage != null && effortSprite != null)
            {
                effortSpriteImage.sprite = effortSprite;
            }

            if (effortLevelText != null)
            {
                effortLevelText.text = $"Effort Level: {effortValue}";
            }
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
        Debug.Log($"Decision made: {(workDecision ? "Work" : "Skip")}");

        // Play the appropriate sound
        if (audioSource != null)
        {
            AudioClip clipToPlay = workDecision ? workButtonSound : skipButtonSound;
            if (clipToPlay != null)
            {
                audioSource.PlayOneShot(clipToPlay);
            }
            else
            {
                Debug.LogWarning($"{(workDecision ? "Work" : "Skip")} button sound is not assigned!");
            }
        }
        else
        {
            Debug.LogError("AudioSource is not assigned!");
        }

        experimentManager.HandleDecision(workDecision);
        DisableButtons();
        LogDecision(workDecision);
    }

    private void LogDecision(bool workDecision)
    {
        string logEntry = $"{System.DateTime.Now}: Player decided to {(workDecision ? "Work" : "Skip")}";
        System.IO.File.AppendAllText("decision_log.txt", logEntry + System.Environment.NewLine);
        Debug.Log(logEntry);
    }
}