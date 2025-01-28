using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class RestBreakManager : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI blockInfoText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string nextSceneName = "Block_Instructions"; // Generic name since blocks are randomized
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

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(continueButton);
    }

private void UpdateBlockInfo()
{
    if (blockInfoText != null && ExperimentManager.Instance != null)
    {
        int currentBlock = ExperimentManager.Instance.GetCurrentBlockNumber();
        ExperimentManager.BlockType nextBlockType = ExperimentManager.Instance.GetNextBlockType();

        Debug.Log($"RestBreakManager: Current block: {currentBlock}, Next block type: {nextBlockType}");

        // Check if we're at the end of the experiment
        if (currentBlock > ExperimentManager.Instance.GetTotalBlocks())
        {
            SceneManager.LoadScene("EndExperiment");
            return;
        }

        string blockDescription = nextBlockType == ExperimentManager.BlockType.HighLowRatio ?
            "where most trials will require more effort" :
            "where most trials will require less effort";

        // 修改显示逻辑，确保 currentBlock - 1 不会为负数
        int completedBlock = currentBlock - 1;
        if (completedBlock < 0) completedBlock = 0; // 防止负数

        blockInfoText.text = $"You have completed Block {completedBlock} of 2.\n\n" +
            "Take a short break, then click 'Continue' when you're ready to start " +
            $"the next block {blockDescription}. ";

        Debug.Log($"RestBreakManager: Block info updated. Completed block: {completedBlock}, Next block type: {nextBlockType}");
    }
    else
    {
        Debug.LogError("RestBreakManager: Required components not assigned!");
    }
}


    private void UpdateScoreDisplay()
    {
        if (scoreText != null && ScoreManager.Instance != null)
        {
            int totalScore = ScoreManager.Instance.GetTotalScore();
            scoreText.text = $"Current Total Score: {totalScore}";
            Debug.Log($"RestBreakManager: Score updated. Total score: {totalScore}");
        }
        else
        {
            Debug.LogError("RestBreakManager: Score text or ScoreManager not assigned!");
        }
    }

    private void ContinueExperiment()
    {
        Debug.Log("RestBreakManager: ContinueExperiment method called");

        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.ContinueAfterBreak();
            
            // Load the instructions scene (now generic since blocks are randomized)
            SceneManager.LoadScene(nextSceneName);
            Debug.Log($"RestBreakManager: Loading next scene: {nextSceneName}");
        }
        else
        {
            Debug.LogError("RestBreakManager: ExperimentManager.Instance is null!");
            // Fallback direct scene loading
            SceneManager.LoadScene(nextSceneName);
        }
    }
}