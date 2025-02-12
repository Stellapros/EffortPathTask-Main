using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    private int totalScore = 0;
    private int practiceScore = 0;
    private ScoreAnimationManager animationManager;

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
        animationManager = gameObject.AddComponent<ScoreAnimationManager>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Debug.Log($"ScoreManager: Scene loaded - {scene.name}");

        // Ensure score text persists across scene loads
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
    
    private void CreateScoreText()
    {
        // Find existing canvas or create new one
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999; // Ensure it's on top
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create score text object
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(canvas.transform, false);

        // Set up TextMeshProUGUI component
        scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreText.fontSize = 36;
        scoreText.color = Color.white;
        scoreText.font = TMP_Settings.defaultFontAsset;
        scoreText.alignment = TextAlignmentOptions.TopRight;

        // Position the score text
        RectTransform rectTransform = scoreText.rectTransform;
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-20, -20);
        rectTransform.sizeDelta = new Vector2(200, 50);

        UpdateScoreDisplay();
    }

    public void AddScore(int points, bool isFormalTrial)
    {
        Debug.Log($"≈ called - Points: {points}, IsFormalTrial: {isFormalTrial}, Current Scene: {SceneManager.GetActiveScene().name}");

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

        // Force immediate display update
        // UpdateScoreDisplay();

        // Force update across scenes
        if (scoreText == null)
        {
            StartCoroutine(FindAndUpdateScoreTextPersistent());
        }
        else
        {
            UpdateScoreDisplay();
        }

        // Verify the score text was updated
        if (scoreText != null)
        {
            Debug.Log($"Score display updated. Current text: {scoreText.text}");
        }
        else
        {
            Debug.LogWarning("Score text is null during AddScore!");
            // CreateScoreText();          
        }

        // Play animation if possible
        // if (scoreText != null && animationManager != null)
        // {
        //     animationManager.PlayScoreAnimation(scoreText, points);
        // }
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