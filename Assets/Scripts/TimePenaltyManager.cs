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
    private string cachedPenaltyMessage = string.Empty;
    private float lastUpdateTime = 0f;
    private const float TEXT_UPDATE_INTERVAL = 0.1f;
    private bool isInitialized = false;

    private void Awake()
    {
        if (penaltyText == null)
        {
            Debug.LogError("PenaltyText component not assigned!");
            enabled = false;
            return;
        }

        // Ensure we only initialize once
        if (!isInitialized)
        {
            currentPenaltyTime = penaltyDuration;
            lastUpdateTime = Time.time;
            isInitialized = true;
        }
    }

    private void Start()
    {
        // Initial text update
        UpdatePenaltyText();
    }

    private void Update()
    {
        if (penaltyComplete || isTransitioning) return;

        if (currentPenaltyTime > 0)
        {
            currentPenaltyTime -= Time.deltaTime;

            if (Time.time - lastUpdateTime >= TEXT_UPDATE_INTERVAL)
            {
                UpdatePenaltyText();
                lastUpdateTime = Time.time;
            }

            if (currentPenaltyTime <= 0)
            {
                CompleteAndTransition();
            }
        }
    }

    private void CompleteAndTransition()
    {
        if (penaltyComplete || isTransitioning) return;

        // Check if we already processed this penalty
        if (ExperimentManager.Instance != null && ExperimentManager.Instance.DecisionMade)
        {
            // Skip penalty if decision was already made
            SceneManager.LoadScene("DecisionPhase");
            return;
        }

        currentPenaltyTime = 0;
        UpdatePenaltyText();
        penaltyComplete = true;
        isTransitioning = true;

        if (ExperimentManager.Instance == null)
        {
            Debug.LogError("ExperimentManager.Instance is null!");
            return;
        }

        StartCoroutine(TransitionAfterDelay());
    }

    private void UpdatePenaltyText()
    {
        if (penaltyText == null) return;

        string penaltyReason = penaltyDuration > 3f ? "No decision made!" : "Trial skipped!";
        string newMessage = $"{penaltyReason}\nWait {Mathf.Max(0, currentPenaltyTime):F1} seconds before the next chance...";

        if (newMessage != cachedPenaltyMessage)
        {
            cachedPenaltyMessage = newMessage;
            penaltyText.text = newMessage;
        }
    }

    private System.Collections.IEnumerator TransitionAfterDelay()
    {
        // Add a small delay to ensure UI is stable
        yield return new WaitForSeconds(0.5f);

        if (ExperimentManager.Instance != null && ExperimentManager.Instance.HasTimeForNewTrial())
        {
            Debug.Log("Loading decision phase for next trial");
            SceneManager.LoadScene("DecisionPhase");
        }
        else
        {
            Debug.Log("No time remaining for new trial after penalty");
            SceneManager.LoadScene("RestBreak");
        }
    }
}