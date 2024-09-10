using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ArrowKeyCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText;  // Reference to the TextMeshProUGUI component for the counter
    public TextMeshProUGUI timerText;    // Reference to the TextMeshProUGUI component for the timer
    private int counter = 0;
    private float elapsedTime = 0f;
    private float gameTimer = 5f; // Game timer in seconds
    private bool gameInProgress = false;

    void Update()
    {
        if (!gameInProgress)
        {
            // Start the game when any arrow key is pressed
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                gameInProgress = true;
                elapsedTime = 0f;
            }
        }
        else
        {
            elapsedTime += Time.deltaTime;
            UpdateCounterText();
            UpdateTimerText();

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                counter++;
                Debug.Log("Counter value is: " + counter);
            }

            if (elapsedTime >= gameTimer)
            {
                StopGame();
            }
        }
    }

    void UpdateCounterText()
    {
        counterText.text = "Count: " + counter.ToString();
    }

    void UpdateTimerText()
    {
        float timeLeft = Mathf.Max(0, gameTimer - elapsedTime);
        timerText.text = "Time: " + timeLeft.ToString("F1") + "s";
    }

    void StopGame()
    {
        gameInProgress = false;
        PlayerPrefs.SetInt("totalKeyPresses", counter);
        SceneManager.LoadScene("GetReady");
    }
}