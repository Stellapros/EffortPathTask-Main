using UnityEngine;

public class PracticeScoreManager : MonoBehaviour
{
    public static PracticeScoreManager Instance { get; private set; }
    private float currentScore = 0f;

    public void Initialize()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetScore()
    {
        currentScore = 0f;
    }

    public void AddScore(float scoreToAdd, bool isPractice)
    {
        currentScore += scoreToAdd;
        Debug.Log($"Practice Score Updated: {currentScore}");
    }

    public float GetCurrentScore()
    {
        return currentScore;
    }
}