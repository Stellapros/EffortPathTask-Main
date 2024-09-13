using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using TMPro;

public class EffortSpriteUI : MonoBehaviour
{
    [SerializeField] private Image effortSpriteImage;
    [SerializeField] private TextMeshProUGUI effortLevelText;
    [SerializeField] private Button workButton;
    [SerializeField] private Button skipButton;

    private Action<bool> onDecisionMade;
    private ExperimentManager experimentManager;

    private void Awake()
    {
        SetupButtonListeners();
    }

    private void Start()
    {
        experimentManager = ExperimentManager.Instance;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found in the scene!");
            return;
        }
    }

    private void SetupButtonListeners()
    {
        if (workButton != null)
        {
            workButton.onClick.RemoveAllListeners();
            workButton.onClick.AddListener(() => MakeDecision(true));
        }
        else
        {
            Debug.LogError("Work button is not assigned!");
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() => MakeDecision(false));
        }
        else
        {
            Debug.LogError("Skip button is not assigned!");
        }
    }

    public void Show(Action<bool> decisionCallback)
    {
        onDecisionMade = decisionCallback;
        gameObject.SetActive(true);
        UpdateEffortSprite();
        EnableButtons();
    }

    public void UpdateEffortSprite()
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

    private void MakeDecision(bool workDecision)
    {
        Debug.Log($"Decision made in EffortSpriteUI: {(workDecision ? "Work" : "Skip")}");
        onDecisionMade?.Invoke(workDecision);
        DisableButtons();
        LogDecision(workDecision);
    }

    // private void MakeDecision(bool workDecision)
    // {
    //     Debug.Log($"Decision made in EffortSpriteUI: {(workDecision ? "Work" : "Skip")}");
    //     if (workDecision)
    //     {
    //         Debug.Log("EffortSpriteUI: Directly loading GridWorld scene for testing.");
    //         SceneManager.LoadScene("GridWorld"); // Make sure to use the correct scene name
    //     }
    //     onDecisionMade?.Invoke(workDecision);
    //     DisableButtons();
    // }

    private void DisableButtons()
    {
        if (workButton != null) workButton.interactable = false;
        if (skipButton != null) skipButton.interactable = false;
    }

    private void LogDecision(bool workDecision)
    {
        string logEntry = $"{System.DateTime.Now}: Player decided to {(workDecision ? "Work" : "Skip")}";
        System.IO.File.AppendAllText("decision_log.txt", logEntry + System.Environment.NewLine);
        Debug.Log(logEntry);
    }
}