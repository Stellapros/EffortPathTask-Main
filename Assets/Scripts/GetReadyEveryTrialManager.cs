using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System;

public class GetReadyEveryTrialManager : MonoBehaviour
{
    [SerializeField] private float displayDuration = 1f;
    [SerializeField] private TextMeshProUGUI blockCounterText;
    [SerializeField] private TextMeshProUGUI trialCounterText;
    [SerializeField] private TextMeshProUGUI readyText;

    private float currentTimer;
    private PracticeManager practiceManager;

    private void Start()
    {
        // Initialize timer
        currentTimer = displayDuration;

        // // Get ExperimentManager instance
        // experimentManager = ExperimentManager.Instance;

        // Get PracticeManager instance
        practiceManager = PracticeManager.Instance;

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
        if (trialCounterText != null && practiceManager != null)
        {
            int currentTrialIndex = practiceManager.GetCurrentPracticeTrialIndex() + 1;
            int totalPracticeTrials = practiceManager.GetTotalPracticeTrials(); // This matches the totalPracticeTrials in PracticeManager

            trialCounterText.text = $"Practice Trial {currentTrialIndex} of {totalPracticeTrials}";
        }
    }


    private void UpdateBlockCounter()
    {
        if (blockCounterText != null)
        {
            // For practice, we'll just show "Practice Block"
            blockCounterText.text = "Practice Block";
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
        SceneManager.LoadScene("PracticeGridWorld");
    }
}