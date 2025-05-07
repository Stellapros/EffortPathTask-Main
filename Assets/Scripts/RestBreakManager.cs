using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class RestBreakManager : MonoBehaviour
{
    /// <summary>
    /// This class manages the rest break between blocks in the experiment.
    /// </summary>
    
    [SerializeField] private Button continueButton;
    [SerializeField] private TextMeshProUGUI blockInfoText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private string nextSceneName = "Block_Instructions";
    [SerializeField] public AudioClip buttonClickSound;
    private AudioSource audioSource;
    private bool canContinue = false;
    [SerializeField] private float minimumDisplayTime = 5f; // Minimum time before allowing continue
    private float startTime;

    private void Start()
    {
        Debug.Log("RestBreakManager: Start method called");
        startTime = Time.time;

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(ContinueExperiment);
            continueButton.interactable = false; // Disable at start
        }

        UpdateBlockInfo();
        UpdateScoreDisplay();

        // Add navigation controller
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(continueButton);
    }

    private void Update()
    {
        // Enable continue after minimum display time
        if (!canContinue && Time.time - startTime >= minimumDisplayTime)
        {
            canContinue = true;
            if (continueButton != null) continueButton.interactable = true;
        }

        // Check for space key press to continue (only if allowed)
        if (canContinue && Input.GetKeyDown(KeyCode.Space))
        {
            ContinueExperiment();
        }
    }

    private void UpdateBlockInfo()
    {
        if (blockInfoText != null && ExperimentManager.Instance != null)
        {
            int currentBlock = ExperimentManager.Instance.GetCurrentBlockNumber();
            ExperimentManager.BlockType nextBlockType = ExperimentManager.Instance.GetNextBlockType();

            blockInfoText.text = $"Nice shot!\n\n" +
                "Take a short break, then hit 'Space' or the 'Continue' button when you're ready to embark on the next island's adventure";
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null && ScoreManager.Instance != null)
        {
            int totalScore = ScoreManager.Instance.GetTotalScore();
            scoreText.text = $"Current Total Score: {totalScore}";
        }
    }

    private void ContinueExperiment()
    {
        if (!canContinue) return;

        Debug.Log("RestBreakManager: ContinueExperiment method called");

        if (ExperimentManager.Instance != null)
        {
            ExperimentManager.Instance.ContinueAfterBreak();
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}