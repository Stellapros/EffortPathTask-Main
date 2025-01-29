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

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        animationManager = gameObject.AddComponent<ScoreAnimationManager>();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Specifically for GridWorld scene
        if (scene.name == "GridWorld")
        {
            // Delay to ensure UI is fully loaded
            StartCoroutine(FindAndUpdateScoreText());
        }
    }

    private System.Collections.IEnumerator FindAndUpdateScoreText()
    {
        // Wait a couple of frames to ensure UI is fully initialized
        yield return new WaitForSeconds(0.1f);

        // Find all TextMeshProUGUI components in the scene

        TextMeshProUGUI[] textComponents = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        
        // Find the score text component (usually named "ScoreText" or contains "Score")
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.name.Contains("Score", System.StringComparison.OrdinalIgnoreCase))
            {
                scoreText = text;
                break;
            }
        }

        // If we still haven't found it, take the first TextMeshProUGUI component
        if (scoreText == null && textComponents.Length > 0)
        {
            scoreText = textComponents[0];
        }

        if (scoreText == null)
        {
            Debug.LogError("ScoreText not found in GridWorld scene!");
            // Create a new TextMeshProUGUI if none exists
            CreateScoreText();
            yield break;
        }

        // Ensure the score text is visible and updated
        scoreText.gameObject.SetActive(true);
        UpdateScoreDisplay();
    }

        private void CreateScoreText()
    {
        // Create a new UI Text component if none exists
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(canvas.transform, false);
        scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreText.fontSize = 36;
        scoreText.alignment = TextAlignmentOptions.TopRight;
        scoreText.rectTransform.anchorMin = new Vector2(1, 1);
        scoreText.rectTransform.anchorMax = new Vector2(1, 1);
        scoreText.rectTransform.pivot = new Vector2(1, 1);
        scoreText.rectTransform.anchoredPosition = new Vector2(-20, -20);
        UpdateScoreDisplay();
    }

    public void AddScore(int points, bool isFormalTrial)
    {
        if (isFormalTrial)
        {
            totalScore += points;
            Debug.Log($"Formal trial: Score added: {points}. Total score: {totalScore}");
        }
        else
        {
            practiceScore += points;
            Debug.Log($"Practice trial: Score added: {points}. Practice score: {practiceScore}");
        }

        // Immediately update score display
        UpdateScoreDisplay();
                if (scoreText != null && animationManager != null)
        {
            animationManager.PlayScoreAnimation(scoreText, points);
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

    private void UpdateScoreDisplay()
    {
        // Always try to find the score text if it's null
        if (scoreText == null)
        {
            StartCoroutine(FindAndUpdateScoreText());
            return;
        }

        // Only update if we're in a GridWorld scene
        if (SceneManager.GetActiveScene().name.Contains("GridWorld"))
        {
            scoreText.text = $"Score: {totalScore}";
            scoreText.gameObject.SetActive(true);
        }
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