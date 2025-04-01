using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PracticeScoreManager : MonoBehaviour
{
    public static PracticeScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI practiceScoreText;
    private int currentPracticeScore = 0;
    private ScoreAnimationManager scoreAnimationManager;
    public PracticeManager practiceManager;

    // List of scenes where practice score should be displayed
    private readonly string[] scenesWithPracticeScore = { "PracticeGridWorld", "PracticeDecisionPhase" };

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Force reset to 0 at start of practice
            PlayerPrefs.SetInt("PersistentPracticeScore", 0);
            // currentPracticeScore = 0;
            currentPracticeScore = PlayerPrefs.GetInt("PersistentPracticeScore", 0);
            PlayerPrefs.Save();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        scoreAnimationManager = gameObject.AddComponent<ScoreAnimationManager>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Always retrieve the persistent score whenever a scene is loaded
        currentPracticeScore = PlayerPrefs.GetInt("PersistentPracticeScore", 0);
        Debug.Log($"Scene Loaded: {scene.name}, Persistent Score: {currentPracticeScore}");

        bool shouldDisplayPracticeScore = System.Array.Exists(scenesWithPracticeScore,
            sceneName => scene.name.Contains(sceneName));

        if (shouldDisplayPracticeScore)
        {
            // Use a more robust method to find and update score text
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
                    practiceScoreText = text;
                    break;
                }
            }

            // If score text is found, update and break
            if (practiceScoreText != null)
            {
                UpdateScoreDisplay();
                yield break;
            }

            // If not found, wait and try again
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void AddScore(int points)
    {
        if (Instance == null)
        {
            Debug.LogError("PracticeScoreManager.Instance is null!");
            return;
        }

        if (PracticeManager.Instance == null)
        {
            Debug.LogError("PracticeManager.Instance is null!");
            return;
        }

        if (LogManager.Instance == null)
        {
            Debug.LogError("LogManager.Instance is null!");
            return;
        }

        Debug.Log($"AddScore called with {points} points");
        Debug.Log($"Current Practice Score BEFORE adding: {currentPracticeScore}");

        currentPracticeScore += points;

        Debug.Log($"Current Practice Score AFTER adding: {currentPracticeScore}");

        // Persistently store score using PlayerPrefs
        PlayerPrefs.SetInt("PersistentPracticeScore", currentPracticeScore);
        PlayerPrefs.Save();

        // Log the score update
        int trialIndex = PracticeManager.Instance.GetCurrentPracticeTrialIndex();

        // Get both scores for logging (using different variable names)
        int currentPracticeScoreValue = this.currentPracticeScore;
        int currentTotalScoreValue = 0;

        // Get the formal trial score if possible
        if (ScoreManager.Instance != null)
        {
            currentTotalScoreValue = ScoreManager.Instance.GetTotalScore();
        }

        // For practice, always use block 0
        int blockNumber = 0;

        LogManager.Instance.LogScoreUpdateComplete(trialIndex + 1, true, points, "PracticeScoreAdded",
                                                  currentTotalScoreValue, currentPracticeScoreValue, blockNumber);

        UpdateScoreDisplay();

        // Play animation if possible
        if (practiceScoreText != null && scoreAnimationManager != null)
        {
            scoreAnimationManager.PlayScoreAnimation(practiceScoreText, points);
        }
    }

    // private void UpdateScoreDisplay()
    // {
    //     if (practiceScoreText == null)
    //     {
    //         StartCoroutine(FindAndUpdateScoreTextPersistent());
    //         return;
    //     }

    //     Scene currentScene = SceneManager.GetActiveScene();

    //     bool shouldDisplayPracticeScore = System.Array.Exists(scenesWithPracticeScore,
    //         sceneName => currentScene.name.Contains(sceneName));

    //     if (shouldDisplayPracticeScore)
    //     {
    //         // Retrieve the latest score from PlayerPrefs to ensure consistency
    //         currentPracticeScore = PlayerPrefs.GetInt("PersistentPracticeScore", 0);

    //         practiceScoreText.text = $"Score: {currentPracticeScore}";
    //         practiceScoreText.gameObject.SetActive(true);

    //         // Ensure score text is on top
    //         Canvas canvas = practiceScoreText.GetComponentInParent<Canvas>();
    //         if (canvas != null)
    //         {
    //             canvas.sortingOrder = 999;
    //         }

    //         // Reset opacity for active scenes
    //         practiceScoreText.color = new Color(practiceScoreText.color.r, practiceScoreText.color.g, practiceScoreText.color.b, 1f);

    //         Debug.Log($"Updated Practice Score Display in {currentScene.name}: {currentPracticeScore}");
    //     }
    //     else
    //     {
    //         // Dim but don't hide score text in non-score scenes
    //         if (practiceScoreText != null)
    //         {
    //             practiceScoreText.color = new Color(practiceScoreText.color.r, practiceScoreText.color.g, practiceScoreText.color.b, 0.5f);
    //         }
    //     }
    // }

    private void UpdateScoreDisplay()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"UpdateScoreDisplay called in scene: {currentScene.name}");

        // Create score text if it doesn't exist
        if (practiceScoreText == null)
        {
            Debug.Log("Score text is null, attempting to create");
            StartCoroutine(FindAndUpdateScoreTextPersistent());
            return;
        }

        bool shouldDisplayPracticeScore = System.Array.Exists(scenesWithPracticeScore,
            sceneName => currentScene.name.Contains(sceneName));

        if (shouldDisplayPracticeScore)
        {
            // Retrieve the latest score from PlayerPrefs to ensure consistency
            currentPracticeScore = PlayerPrefs.GetInt("PersistentPracticeScore", 0);

            practiceScoreText.text = $"Score: {currentPracticeScore}";
            practiceScoreText.gameObject.SetActive(true);

            // Ensure persistent display
            Canvas canvas = practiceScoreText.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 999; // Ensure always on top
            }

            Debug.Log($"Score display updated and enabled. Current score: {currentPracticeScore}");
        }
        else
        {
            // Instead of hiding, keep it visible but at a lower opacity
            practiceScoreText.color = new Color(practiceScoreText.color.r, practiceScoreText.color.g, practiceScoreText.color.b, 0.5f);
            practiceScoreText.gameObject.SetActive(true);
            Debug.Log("Score display dimmed but not hidden");
        }
    }

    public void ResetScore()
    {
        int oldPracticeScore = currentPracticeScore;
        currentPracticeScore = 0;

        // Clear persistent score in PlayerPrefs
        PlayerPrefs.SetInt("PersistentPracticeScore", 0);
        PlayerPrefs.Save();

        LogManager.Instance.LogScoreReset(0, true, oldPracticeScore, currentPracticeScore, "Practice Score Reset");
        UpdateScoreDisplay();
    }

    public int GetCurrentScore() => currentPracticeScore;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LogManager.Instance?.LogInfoMessage("Practice Score Manager Destroyed", $"Final Practice Score: {currentPracticeScore}");
    }
}