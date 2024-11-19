using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GetReadyEveryTrialManager : MonoBehaviour
{
    [SerializeField] private float displayDuration = 1f;
    [SerializeField] private TextMeshProUGUI blockCounterText;
    [SerializeField] private TextMeshProUGUI trialCounterText;
    [SerializeField] private TextMeshProUGUI readyText;

    private float currentTimer;
    private ExperimentManager experimentManager;

    private void Start()
    {
        // Initialize timer
        currentTimer = displayDuration;

        // Get ExperimentManager instance
        experimentManager = ExperimentManager.Instance;

        // Update trial and block counters
        UpdateTrialCounter();
        UpdateBlockCounter();

        // Set up ready text
        SetupReadyText();
    }

    private void Update()
    {
        // Count down the timer
        currentTimer -= Time.deltaTime;

        // When timer reaches zero, move to next scene
        if (currentTimer <= 0)
        {
            MoveToNextScene();
        }
    }

    private void UpdateTrialCounter()
    {
        if (trialCounterText != null && experimentManager != null)
        {
            int currentTrialInBlock = experimentManager.GetCurrentTrialInBlock();
            int totalTrialsInBlock = experimentManager.GetTotalTrialsInBlock();

            trialCounterText.text = $"Trial {currentTrialInBlock} of {totalTrialsInBlock}";
        }
    }

    private void UpdateBlockCounter()
    {
        if (blockCounterText != null && experimentManager != null)
        {
            int currentBlock = experimentManager.GetCurrentBlockNumber();
            blockCounterText.text = $"Block {currentBlock + 1}";
        }
    }

    private void SetupReadyText()
    {
        if (readyText != null)
        {
            readyText.text = "Get Ready!";
        }
    }

    private void MoveToNextScene()
    {
        // Move to GridWorld scene
        SceneManager.LoadScene("GridWorld");
    }

    // Optional: Add a method to manually skip (for testing)
    public void SkipToNextScene()
    {
        MoveToNextScene();
    }

    // Optional additional debug information
    private void LogTrialDetails()
    {
        if (experimentManager != null)
        {
            Debug.Log($"Current Block: {experimentManager.GetCurrentBlockNumber()}");
            Debug.Log($"Current Trial in Block: {experimentManager.GetCurrentTrialInBlock()}");
            
            Vector2 playerPos = experimentManager.GetCurrentTrialPlayerPosition();
            Vector2 rewardPos = experimentManager.GetCurrentTrialRewardPosition();
            int rewardValue = experimentManager.GetCurrentTrialRewardValue();

            Debug.Log($"Player Position: {playerPos}");
            Debug.Log($"Reward Position: {rewardPos}");
            Debug.Log($"Reward Value: {rewardValue}");
        }
    }
}