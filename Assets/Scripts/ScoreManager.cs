using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    private int totalScore = 0;
    private int practiceScore = 0;
    private ScoreAnimationManager scoreAnimationManager;

    // List of scenes where score should be displayed
    private readonly string[] scenesWithScore = { "GridWorld", "DecisionPhase" };

    private void Awake()
    {
        Debug.Log("ScoreManager Awake called");
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
            Debug.Log("ScoreManager instance created and set to DontDestroyOnLoad");
            // CreateScoreText();
        }
        else if (Instance != this)
        {
            Debug.Log("Destroying duplicate ScoreManager");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        scoreAnimationManager = gameObject.AddComponent<ScoreAnimationManager>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldDisplayScore = System.Array.Exists(scenesWithScore,
            sceneName => scene.name.Contains(sceneName));

        if (shouldDisplayScore)
        {
            StartCoroutine(FindAndUpdateScoreTextPersistent());
        }
    }

    private System.Collections.IEnumerator FindAndUpdateScoreTextPersistent()
    {
        // Wait a bit longer to ensure UI is fully loaded
        yield return new WaitForSeconds(0.2f);

        // Try multiple times to find score text
        for (int attempt = 0; attempt < 3; attempt++)
        {
            TextMeshProUGUI[] textComponents = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

            foreach (TextMeshProUGUI text in textComponents)
            {
                if (text.name.Contains("Score", System.StringComparison.OrdinalIgnoreCase))
                {
                    scoreText = text;
                    break;
                }
            }

            // If score text is found, update and break
            if (scoreText != null)
            {
                UpdateScoreDisplay();
                yield break;
            }

            // If not found, wait and try again
            yield return new WaitForSeconds(0.1f);
        }

        // As a last resort, create score text
        // Uncomment if needed
        // CreateScoreText();
    }


    public void AddScore(int points, bool isFormalTrial)
    {
        Debug.Log($"AddScore called - Points: {points}, IsFormalTrial: {isFormalTrial}, Current Scene: {SceneManager.GetActiveScene().name}");

        if (isFormalTrial)
        {
            totalScore += points;
            Debug.Log($"Formal trial: Added {points} points. New total score: {totalScore}");
        }
        else
        {
            practiceScore += points;
            Debug.Log($"Practice trial: Added {points} points. New practice score: {practiceScore}");
        }

        // Get current trial number from TrialManager if available
        int trialNumber = 0;
        if (ExperimentManager.Instance != null)
        {
            trialNumber = ExperimentManager.Instance.GetCurrentTrialIndex() + 1;
        }

        // Get scores for logging (using different variable names to avoid conflict)
        int currentTotalScore = this.totalScore;
        int currentPracticeScore = this.practiceScore;

        // Check if we should use practice score from PracticeScoreManager for accuracy
        if (!isFormalTrial && PracticeScoreManager.Instance != null)
        {
            currentPracticeScore = PracticeScoreManager.Instance.GetCurrentScore();
        }

        // Get block number - for practice always use 0, for formal trials get from ExperimentManager
        // int blockNumber = 0;
        int blockNumber = 1;
        if (isFormalTrial && ExperimentManager.Instance != null)
        {
            // blockNumber = PlayerPrefs.GetInt("CurrentBlock", 0);              
            blockNumber = ExperimentManager.Instance.GetCurrentBlockNumber();
        }

        // Log the score update with complete information
        LogManager.Instance.LogScoreUpdateComplete(trialNumber, !isFormalTrial, points, "ScoreAdded",
                                                   currentTotalScore, currentPracticeScore, blockNumber);

        if (scoreText == null)
        {
            StartCoroutine(FindAndUpdateScoreTextPersistent());
        }
        else
        {
            UpdateScoreDisplay();
        }

        if (scoreText != null)
        {
            Debug.Log($"Score display updated. Current text: {scoreText.text}");
        }
        else
        {
            Debug.LogWarning("Score text is null during AddScore!");
        }

        // Play animation if possible
        if (scoreText != null && scoreAnimationManager != null)
        {
            scoreAnimationManager.PlayScoreAnimation(scoreText, points);
        }
    }

    private void UpdateScoreDisplay()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"UpdateScoreDisplay called in scene: {currentScene.name}");

        // Create score text if it doesn't exist
        if (scoreText == null)
        {
            Debug.Log("Score text is null, attempting to create");
            StartCoroutine(FindAndUpdateScoreTextPersistent());
            return;
        }

        bool shouldDisplayScore = System.Array.Exists(scenesWithScore,
            sceneName => currentScene.name.Contains(sceneName));

        if (shouldDisplayScore)
        {
            scoreText.text = $"Score: {totalScore}";
            scoreText.gameObject.SetActive(true);

            // Ensure persistent display
            Canvas canvas = scoreText.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 999; // Ensure always on top
            }

            Debug.Log($"Score display updated and enabled. Current score: {totalScore}");
        }
        else
        {
            // Instead of hiding, keep it visible but at a lower opacity
            scoreText.color = new Color(scoreText.color.r, scoreText.color.g, scoreText.color.b, 0.5f);
            scoreText.gameObject.SetActive(true);
            Debug.Log("Score display dimmed but not hidden");
        }
    }

    public void ResetScore(bool resetPracticeScore = false)
    {
        if (resetPracticeScore)
        {
            int oldPracticeScore = practiceScore;
            practiceScore = 0;
            LogManager.Instance.LogScoreReset(0, true, oldPracticeScore, practiceScore, "Practice Score Reset");
            Debug.Log("Practice score reset to 0");
        }
        else
        {
            int oldTotalScore = totalScore;
            totalScore = 0;
            LogManager.Instance.LogScoreReset(0, false, oldTotalScore, totalScore, "Total Score Reset");
            Debug.Log("Total score reset to 0");
        }
        UpdateScoreDisplay();
    }

    public int GetTotalScore() => totalScore;
    public int GetPracticeScore() => practiceScore;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogInfoMessage("Score Manager Destroyed", $"Final Total Score: {totalScore}, Final Practice Score: {practiceScore}");
        }
    }
}