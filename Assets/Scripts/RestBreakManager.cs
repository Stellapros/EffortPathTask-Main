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
    // [SerializeField] private TextMeshProUGUI instructionText; // Added instruction text reference
    private bool hasInitialized = false;

    private void Start()
    {
        Debug.Log("RestBreakManager: Start method called");
        Debug.Log($"RestBreakManager: Starting break after block {ExperimentManager.Instance.GetCurrentBlockNumber()}");
        if (!hasInitialized)
        {
            Debug.Log($"[RestBreakManager] Initializing rest break after Block {ExperimentManager.Instance.GetCurrentBlockNumber() - 1}");
            Debug.Log($"[RestBreakManager] Next block will be Block {ExperimentManager.Instance.GetCurrentBlockNumber()} of type {ExperimentManager.Instance.GetCurrentBlockType()}");

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(ContinueExperiment);
            }

            UpdateBlockInfo();
            UpdateScoreDisplay();
            hasInitialized = true;
        }

        // Add space bar instruction to the instruction text
        // if (instructionText != null)
        // {
        //     instructionText.text = "Press 'Space' to continue";
        // }

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
            if (currentBlock >= ExperimentManager.Instance.GetTotalBlocks())
            {
                SceneManager.LoadScene("TirednessRating");
                return;
            }

            blockInfoText.text = $"Nice shot!\n\n" +
                "Take a short break, then hit 'Space' or the 'Continue' button when you're ready to embark on the next island's adventure";

            Debug.Log($"RestBreakManager: Block info updated. Completed block: {currentBlock}, Next block type: {nextBlockType}");
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

    private void Update()
    {
        // Check for space key press to continue
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ContinueExperiment();
        }
    }

    private void ContinueExperiment()
    {
        Debug.Log("RestBreakManager: ContinueExperiment method called");

        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.ContinueAfterBreak();

            // Load the instructions scene for the next block
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