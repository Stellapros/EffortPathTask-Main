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
        // Specifically for GridWorld scene
        if (scene.name == "GridWorld")
        {
            // Delay to ensure UI is fully loaded
            StartCoroutine(FindAndUpdateScoreText());
        }
    }

    private System.Collections.IEnumerator FindAndUpdateScoreText()
    {
        // Wait a frame to ensure UI is fully initialized
        yield return null;

        // Find score text in the scene
        scoreText = GameObject.FindAnyObjectByType<TextMeshProUGUI>();

        if (scoreText == null)
        {
            Debug.LogError("ScoreText not found in GridWorld scene!");
            yield break;
        }

        // Always show score in GridWorld
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
    }

    public int GetTotalScore() => totalScore;
    public int GetPracticeScore() => practiceScore;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LogManager.Instance.LogInfoMessage("Score Manager Destroyed", $"Final Total Score: {totalScore}, Final Practice Score: {practiceScore}");
    }

}