using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class PracticeScoreManager : MonoBehaviour
{
    public static PracticeScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI practiceScoreText;
    private int currentPracticeScore = 0;
    private ScoreAnimationManager animationManager;

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
        animationManager = gameObject.AddComponent<ScoreAnimationManager>();
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

    private void CreatePracticeScoreText()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        GameObject practiceScoreTextObj = new GameObject("PracticeScoreText");
        practiceScoreTextObj.transform.SetParent(canvas.transform, false);

        practiceScoreText = practiceScoreTextObj.AddComponent<TextMeshProUGUI>();
        practiceScoreText.fontSize = 36;
        practiceScoreText.color = Color.white;
        practiceScoreText.font = TMP_Settings.defaultFontAsset;
        practiceScoreText.alignment = TextAlignmentOptions.TopRight;

        RectTransform rectTransform = practiceScoreText.rectTransform;
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = new Vector2(-20, -70);
        rectTransform.sizeDelta = new Vector2(200, 50);

        UpdateScoreDisplay();
    }

    public void AddScore(int points)
    {
        // Existing debug logs
        Debug.Log($"AddScore called with {points} points");
        Debug.Log($"Current Practice Score BEFORE adding: {currentPracticeScore}");

        currentPracticeScore += points;

        Debug.Log($"Current Practice Score AFTER adding: {currentPracticeScore}");

        // Persistently store score using PlayerPrefs
        PlayerPrefs.SetInt("PersistentPracticeScore", currentPracticeScore);
        PlayerPrefs.Save();

        // Always try to update, with fallback mechanism
        // if (practiceScoreText == null)
        // {
        //     StartCoroutine(FindAndUpdateScoreTextPersistent());
        // }

        // UpdateScoreDisplay();
        // Force update across scenes
        if (practiceScoreText == null)
        {
            StartCoroutine(FindAndUpdateScoreTextPersistent());
        }
        else
        {
            UpdateScoreDisplay();
        }

        // Verify the score text was updated
        if (practiceScoreText != null)
        {
            Debug.Log($"Score display updated. Current text: {practiceScoreText.text}");
        }
        else
        {
            Debug.LogWarning("Score text is null during AddScore!");
            // CreateScoreText();          
        }


        // Ensure animation plays
        // if (animationManager != null)
        // {
        //     animationManager.PlayScoreAnimation(practiceScoreText, points);
        // }
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

        LogManager.Instance?.LogScoreReset(0, true, oldPracticeScore, currentPracticeScore, "Practice Score Reset");
        UpdateScoreDisplay();
    }

    public int GetCurrentScore() => currentPracticeScore;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LogManager.Instance?.LogInfoMessage("Practice Score Manager Destroyed", $"Final Practice Score: {currentPracticeScore}");
    }
}