using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TimePenaltyManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI penaltyText;
    private float penaltyDuration = 5f;
    private float currentPenaltyTime;
    private bool penaltyComplete = false;
    private bool isTransitioning = false;

    private void Start()
    {
        currentPenaltyTime = penaltyDuration;
        UpdatePenaltyText();
    }

    private void Update()
    {
        if (!penaltyComplete && currentPenaltyTime > 0)
        {
            currentPenaltyTime -= Time.deltaTime;
            UpdatePenaltyText();

            if (currentPenaltyTime <= 0 && !isTransitioning)
            {
                PenaltyComplete();
            }
        }
    }

    private void UpdatePenaltyText()
    {
        if (penaltyText != null)
        {
            string penaltyReason = penaltyDuration > 3f ? "No decision made!" : "Trial skipped!";
            penaltyText.text = $"{penaltyReason}\nWait {currentPenaltyTime:F1} seconds before the next trial...";
        }
    }

private void PenaltyComplete()
{
    if (!penaltyComplete && !isTransitioning)
    {
        penaltyComplete = true;
        isTransitioning = true;

        if (ExperimentManager.Instance != null)
        {
            // Check if we're at the last trial
            int currentTrialIndex = ExperimentManager.Instance.GetCurrentTrialIndex();
            int totalTrials = ExperimentManager.Instance.GetTotalTrials();
            
            if (currentTrialIndex >= totalTrials - 1)
            {
                Debug.Log("Final trial complete - ending experiment");
                ExperimentManager.Instance.EndExperiment();
                return;
            }

            // Check if we're at a block boundary
            int trialsPerBlock = ExperimentManager.Instance.GetTotalTrialsInBlock();
            bool isBlockBoundary = (currentTrialIndex + 1) % trialsPerBlock == 0;
            
            if (isBlockBoundary)
            {
                Debug.Log("Block complete after penalty - transitioning to rest break");
                SceneManager.LoadScene("RestBreak");
            }
            else
            {
                Debug.Log($"Loading decision phase for next trial (index: {currentTrialIndex + 1})");
                SceneManager.LoadScene("DecisionPhase");
            }
        }
        else
        {
            Debug.LogError("ExperimentManager.Instance is null in PenaltyComplete!");
        }
    }
}

}