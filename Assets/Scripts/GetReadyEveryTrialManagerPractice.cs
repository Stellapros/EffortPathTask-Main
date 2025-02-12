using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GetReadyEveryTrialManagerPractice : MonoBehaviour
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
            int currentTrialIndex = practiceManager.GetCurrentPracticeTrialIndex();
            int totalTrials = 12; // Hardcoded total practice trials

            trialCounterText.text = $"Trial {currentTrialIndex + 1} of {totalTrials}";
        }
    }

    private void UpdateBlockCounter()
    {
        if (blockCounterText != null)
        {
            // For practice trials, always show "Practice Block"
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
        // Move to Decision Phase for practice
        SceneManager.LoadScene("PracticeDecisionPhase");
    }

    // Optional: Add a method to manually skip (for testing)
    public void SkipToNextScene()
    {
        MoveToNextScene();
    }

    // Optional debug method 
    private void LogPracticeTrialDetails()
    {
        if (practiceManager != null)
        {
            var currentTrial = practiceManager.GetCurrentPracticeTrial();

            if (currentTrial != null)
            {
                Debug.Log($"Current Practice Trial Index: {practiceManager.GetCurrentPracticeTrialIndex()}");
                Debug.Log($"Effort Level: {currentTrial.effortLevel}");
                Debug.Log($"Reward Value: {currentTrial.rewardValue}");
            }
        }
    }
}