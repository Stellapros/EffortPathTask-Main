using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    private int totalScore = 0;

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
            // Find the scoreText only in the GridWorld scene
            scoreText = GameObject.FindObjectOfType<TextMeshProUGUI>();
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

    // private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    // {
    //     // Find and assign the scoreText in the new scene
    //     scoreText = GameObject.FindObjectOfType<TextMeshProUGUI>();
    //     UpdateScoreDisplay();
    // }

    // public void AddScore(int points)
    // {
    //     totalScore += points;
    //     UpdateScoreDisplay();
    //     Debug.Log($"Score added: {points}. Total score: {totalScore}");
    // }


public void AddScore(int points, bool isFormalTrial)
{
    if (isFormalTrial)
    {
        totalScore += points;
        UpdateScoreDisplay();
        Debug.Log($"Score added: {points}. Total score: {totalScore}");
    }
    else
    {
        Debug.Log($"Practice trial completed. No score added.");
    }
}

    public void ResetScore()
    {
        totalScore = 0;
        UpdateScoreDisplay();
        Debug.Log("Score reset to 0");
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
    //         // bool isGridWorldScene = SceneManager.GetActiveScene().name == "GridWorld";
    //         scoreText.gameObject.SetActive(isGridWorldScene);
    //         if (isGridWorldScene)
    //         {
    //             scoreText.text = $"Score: {totalScore}";
    //         }
    //     }
    // }

    // private void UpdateScoreDisplay()
    // {
    //     if (scoreText != null)
    //     {
    //         scoreText.text = $"Total Score: {totalScore}";
    //         scoreText.gameObject.SetActive(true);
    //     }
    //     else
    //     {
    //         Debug.LogWarning("ScoreText not found in the current scene.");
    //     }
    // }

    public int GetTotalScore() => totalScore;

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}