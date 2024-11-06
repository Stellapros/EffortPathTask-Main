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

    public void AddScore(int points, bool isFormalTrial)
    {
        if (!TourManager.Instance.IsTourActive() && !PracticeManager.Instance.IsPracticeTrial())
        {
            totalScore += points;
            Debug.Log($"Formal trial: Score added: {points}. Total score: {totalScore}");
            UpdateScoreDisplay();
        }
        else
        {
            Debug.Log($"Tour/Practice: Score would be {points} in a formal trial.");
        }
    }

    public void ResetScore(bool resetPracticeScore = false)
    {
        if (resetPracticeScore)
        {
            practiceScore = 0;
            Debug.Log("Practice score reset to 0");
        }
        else
        {
            totalScore = 0;
            Debug.Log("Total score reset to 0");
        }
        UpdateScoreDisplay();
    }

private void UpdateScoreDisplay()
{
    if (scoreText != null && SceneManager.GetActiveScene().name == "GridWorld")
    {
        bool isPracticeTrial = PracticeManager.Instance.IsPracticeTrial();
        int scoreToDisplay = isPracticeTrial ? practiceScore : totalScore;
        string scoreType = isPracticeTrial ? "Practice Score" : "Score";
        scoreText.text = $"{scoreType}: {scoreToDisplay}";
        scoreText.gameObject.SetActive(true);
    }
    else
    {
        // If scoreText is null or the current scene is not "GridWorld"
        // Make sure to deactivate the scoreText GameObject to avoid null reference exceptions
        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(false);
        }
    }
}

    public int GetTotalScore() => totalScore;
    public int GetPracticeScore() => practiceScore;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}