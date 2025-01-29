using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PracticeScoreManager : MonoBehaviour
{
    public static PracticeScoreManager Instance { get; private set; }
    private float currentPracticeScore = 0f;
    private ScoreAnimationManager animationManager;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI practiceScoreText;

    public void Initialize()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        animationManager = gameObject.AddComponent<ScoreAnimationManager>();
        StartCoroutine(FindAndUpdateScoreTexts());
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("PracticeGridWorld"))
        {
            StartCoroutine(FindAndUpdateScoreTexts());
        }
    }

    private System.Collections.IEnumerator FindAndUpdateScoreTexts()
    {
        yield return new WaitForSeconds(0.1f);

        TextMeshProUGUI[] textComponents = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        
        // Find both score texts
        foreach (TextMeshProUGUI text in textComponents)
        {
            if (text.name.Contains("PracticeScore", System.StringComparison.OrdinalIgnoreCase))
            {
                practiceScoreText = text;
            }
            else if (text.name.Contains("Score", System.StringComparison.OrdinalIgnoreCase))
            {
                scoreText = text;
            }
        }

        // Create texts if not found
        if (scoreText == null || practiceScoreText == null)
        {
            CreateScoreTexts();
        }

        UpdateScoreDisplay();
    }

    private void CreateScoreTexts()
    {
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            canvas = new GameObject("Canvas");
            Canvas canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        // Create regular score text
        if (scoreText == null)
        {
            GameObject scoreTextObj = new GameObject("ScoreText");
            scoreTextObj.transform.SetParent(canvas.transform, false);
            scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
            scoreText.fontSize = 36;
            scoreText.alignment = TextAlignmentOptions.TopRight;
            scoreText.rectTransform.anchorMin = new Vector2(1, 1);
            scoreText.rectTransform.anchorMax = new Vector2(1, 1);
            scoreText.rectTransform.pivot = new Vector2(1, 1);
            scoreText.rectTransform.anchoredPosition = new Vector2(-20, -20);
        }

        // Create practice score text
        if (practiceScoreText == null)
        {
            GameObject practiceScoreTextObj = new GameObject("PracticeScoreText");
            practiceScoreTextObj.transform.SetParent(canvas.transform, false);
            practiceScoreText = practiceScoreTextObj.AddComponent<TextMeshProUGUI>();
            practiceScoreText.fontSize = 36;
            practiceScoreText.alignment = TextAlignmentOptions.TopRight;
            practiceScoreText.rectTransform.anchorMin = new Vector2(1, 1);
            practiceScoreText.rectTransform.anchorMax = new Vector2(1, 1);
            practiceScoreText.rectTransform.pivot = new Vector2(1, 1);
            practiceScoreText.rectTransform.anchoredPosition = new Vector2(-20, -70); // Position below the main score
            practiceScoreText.color = new Color(0.584f, 0.761f, 0.749f); // Same as normal score color
        }
    }

    public void ResetScore()
    {
        currentPracticeScore = 0f;
        UpdateScoreDisplay();
    }

    public void AddScore(float scoreToAdd, bool isPractice)
    {
        if (isPractice)
        {
            currentPracticeScore += scoreToAdd;
            Debug.Log($"Practice Score Updated: {currentPracticeScore}");
            
            if (practiceScoreText != null && animationManager != null)
            {
                animationManager.PlayScoreAnimation(practiceScoreText, (int)scoreToAdd);
            }
        }
        
        UpdateScoreDisplay();
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText == null || practiceScoreText == null)
        {
            StartCoroutine(FindAndUpdateScoreTexts());
            return;
        }

        // Always show both scores in GridWorld scenes
        if (SceneManager.GetActiveScene().name.Contains("PracticeGridWorld"))
        {
            if (ScoreManager.Instance != null)
            {
                scoreText.text = $"Score: {ScoreManager.Instance.GetTotalScore()}";
                scoreText.gameObject.SetActive(true);
            }
            
            practiceScoreText.text = $"Practice Score: {currentPracticeScore}";
            practiceScoreText.gameObject.SetActive(true);
        }
    }

    public float GetCurrentScore()
    {
        return currentPracticeScore;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}