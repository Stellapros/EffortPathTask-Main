using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    private int totalScore = 0;
    private int practiceScore = 0;

private void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Check if LogManager is available before logging
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogInfoMessage("Score System Initialized", $"Initial Total Score: {totalScore}, Initial Practice Score: {practiceScore}");
        }
        else
        {
            Debug.LogWarning("LogManager not available, unable to log score initialization.");
        }

        // Find the scoreText in the current scene
        if (scoreText == null)
        {
            scoreText = GameObject.FindAnyObjectByType<TextMeshProUGUI>();
            if (scoreText == null)
            {
                // Check if LogManager is available before logging
                if (LogManager.Instance != null)
                {
                    LogManager.Instance.LogWarning("UI Element Missing", "ScoreText component not found in the current scene");
                }
                else
                {
                    Debug.LogWarning("LogManager not available, unable to log UI element missing.");
                }
            }
        }
        // UpdateScoreDisplay();
    }
    else if (Instance != this)
    {
        Destroy(gameObject);
    }
}

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GridWorld")
        {
            // Find the scoreText only in the GridWorld scene
            scoreText = GameObject.FindAnyObjectByType<TextMeshProUGUI>();
            if (scoreText == null)
            {
                Debug.LogWarning("ScoreText not found in the GridWorld scene.");
            }
        }
        else
        {
            scoreText = null;
        }
        UpdateScoreDisplay();
    }

//    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
//     {
//         if (scoreText != null)
//         {
//             UpdateScoreDisplay();
//         }
//         else
//         {
//             scoreText = GameObject.FindAnyObjectByType<TextMeshProUGUI>();
//             if (scoreText != null)
//             {
//                 UpdateScoreDisplay();
//             }
//             else
//             {
//                 LogManager.Instance.LogWarning("UI Element Missing", "ScoreText component not found in the current scene");
//             }
//         }
//     }

    public void AddScore(int points, bool isFormalTrial)
    {
        if (isFormalTrial)
        {
            totalScore += points;
            LogManager.Instance.LogScoreUpdate(0, false, points, $"Total Score: {totalScore}");
            Debug.Log($"Formal trial: Score added: {points}. Total score: {totalScore}");
        }
        else
        {
            practiceScore += points;
            LogManager.Instance.LogScoreUpdate(0, true, points, $"Practice Score: {practiceScore}");
            Debug.Log($"Practice trial: Score added: {points}. Practice score: {practiceScore}");
        }
        UpdateScoreDisplay();
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
        if (scoreText != null && SceneManager.GetActiveScene().name == "GridWorld")
        {
            scoreText.text = $"Score: {totalScore}";
            scoreText.gameObject.SetActive(true);
        }
        else if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }
    
// private void UpdateScoreDisplay()
// {
//     if (scoreText != null)
//     {
//         bool isPracticeTrial = PracticeManager.Instance.IsPracticeTrial();
//         int scoreToDisplay = isPracticeTrial ? practiceScore : totalScore;
//         string scoreType = isPracticeTrial ? "Practice Score" : "Score";
//         scoreText.text = $"{scoreType}: {scoreToDisplay}";
//         scoreText.gameObject.SetActive(true);

//         LogManager.Instance.LogMessage("Score Display Updated",
//             $"Display Type: {scoreType}, Value: {scoreToDisplay}");
//     }
//     else
//     {
//         // Try to find the scoreText in the current scene
//         scoreText = GameObject.FindAnyObjectByType<TextMeshProUGUI>();
//         if (scoreText == null)
//         {
//             LogManager.Instance.LogWarning("UI Element Missing",
//                 "ScoreText component not found in the current scene");
//         }
//         else
//         {
//             UpdateScoreDisplay();
//         }
//     }
// }

    public int GetTotalScore() => totalScore;
    public int GetPracticeScore() => practiceScore;


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LogManager.Instance.LogInfoMessage("Score Manager Destroyed", $"Final Total Score: {totalScore}, Final Practice Score: {practiceScore}");
    }
}