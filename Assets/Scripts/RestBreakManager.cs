using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class RestBreakManager : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI blockInfoText;
    [SerializeField] private TextMeshProUGUI scoreText;
    // [SerializeField] private string nextSceneName = "DecisionPhase"; // Fallback scene name
    [SerializeField] public AudioClip buttonClickSound;
    private AudioSource audioSource;
    
    private void Start()
    {
        Debug.Log("RestBreakManager: Start method called");

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueExperiment);
            Debug.Log("RestBreakManager: Continue button listener added");
        }
        else
        {
            Debug.LogError("RestBreakManager: Continue button not assigned!");
        }

        UpdateBlockInfo();
        UpdateScoreDisplay();

        // Add in Start() method
ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
navigationController.AddElement(continueButton);
    }

    private void UpdateBlockInfo()
    {
        if (blockInfoText != null)
        {
            if (ExperimentManager.Instance != null)
            {
                int completedBlock = ExperimentManager.Instance.GetCurrentBlockNumber();
                int displayCompletedBlock = completedBlock; // Adjust for display
                int nextBlock = displayCompletedBlock + 1;
                int totalBlocks = 3; // Assuming 3 blocks as per your experiment design

                blockInfoText.text = $"You have completed Block {displayCompletedBlock} of {totalBlocks}.\n\nTake a short break, then click 'Continue' when you're ready to start Block {nextBlock}.";
                Debug.Log($"RestBreakManager: Block info updated. Completed block: {displayCompletedBlock}, Next block: {nextBlock}");
            }
            else
            {
                blockInfoText.text = "Experiment information unavailable.";
                Debug.LogError("RestBreakManager: ExperimentManager.Instance is null!");
            }
        }
        else
        {
            Debug.LogError("RestBreakManager: Block info text not assigned!");
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            if (ScoreManager.Instance != null)
            {
                int totalScore = ScoreManager.Instance.GetTotalScore();
                scoreText.text = $"Current Total Score: {totalScore}";
                Debug.Log($"RestBreakManager: Score updated. Total score: {totalScore}");
            }
            else
            {
                scoreText.text = "Score unavailable.";
                Debug.LogError("RestBreakManager: ScoreManager.Instance is null!");
            }
        }
        else
        {
            Debug.LogError("RestBreakManager: Score text not assigned!");
        }
    }

    private void ContinueExperiment()
    {
        Debug.Log("RestBreakManager: ContinueExperiment method called");

        if (ExperimentManager.Instance != null)
        {
            Debug.Log("RestBreakManager: ExperimentManager.Instance is not null");
            ExperimentManager.Instance.ContinueAfterBreak();
            Debug.Log("RestBreakManager: ExperimentManager.ContinueAfterBreak called");
        }
        else
        {
            Debug.LogError("RestBreakManager: ExperimentManager.Instance is null!");
        }

        // Add direct scene loading
        Debug.Log("RestBreakManager: Attempting to load DecisionPhase scene directly");
        SceneManager.LoadScene("DecisionPhase");
    }
}