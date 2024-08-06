using UnityEngine;
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
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start() => UpdateScoreUI();

    public void AddScore(int value)
    {
        totalScore += value;
        UpdateScoreUI();
    }

    public void IncreaseScore(int value) => AddScore(value);

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {totalScore}";
        }
    }

    public int GetTotalScore() => totalScore;

    public void ResetScore()
    {
        totalScore = 0;
        UpdateScoreUI();
    }
}
