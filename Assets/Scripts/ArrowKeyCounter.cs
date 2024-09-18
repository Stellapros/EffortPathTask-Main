using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ArrowKeyCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText; // Reference to the TextMeshProUGUI component for the counter
    public TextMeshProUGUI timerText; // Reference to the TextMeshProUGUI component for the timer
    private int counter = 0;
    private float elapsedTime = 0f;
    private float calibrationTime = 5f;
    private bool calibrationInProgress = false;

    private void Start()
    {
        UpdateCounterText();
        UpdateTimerText();
    }

    private void Update()
    {
        if (!calibrationInProgress)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                StartCalibration();
            }
        }
        else
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerText();

            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                IncrementCounter();
            }

            if (elapsedTime >= calibrationTime)
            {
                EndCalibration();
            }
        }
    }

    private void StartCalibration()
    {
        calibrationInProgress = true;
        elapsedTime = 0f;
        counter = 0;
        UpdateCounterText();
        Debug.Log("Calibration started");
    }

    private void IncrementCounter()
    {
        counter++;
        UpdateCounterText();
        Debug.Log("Counter value is: " + counter);
    }

    private void UpdateCounterText()
    {
        counterText.text = "Count: " + counter.ToString();
    }

    private void UpdateTimerText()
    {
        float timeLeft = Mathf.Max(0, calibrationTime - elapsedTime);
        timerText.text = "Time: " + timeLeft.ToString("F1") + "s";
    }

    private void EndCalibration()
    {
        calibrationInProgress = false;
        PlayerPrefs.SetInt("totalKeyPresses", counter);
        Debug.Log("Calibration ended. Total key presses: " + counter);

        // Calculate presses per effort level
        CalculateAndSetPressesPerEffortLevel();

        // Load the next scene or start the experiment
        SceneManager.LoadScene("GetReady");
    }

    private void CalculateAndSetPressesPerEffortLevel()
    {
        float pressRate = (float)counter / calibrationTime;
        int[] pressesPerEffortLevel = new int[3];

        pressesPerEffortLevel[0] = Mathf.RoundToInt(pressRate * 0.5f);  // Easy
        pressesPerEffortLevel[1] = Mathf.RoundToInt(pressRate);         // Medium
        pressesPerEffortLevel[2] = Mathf.RoundToInt(pressRate * 1.5f);  // Hard

        // Ensure minimum difference between levels: ensure that each difficulty level 
        // is noticeably different from the previous one
        for (int i = 1; i < pressesPerEffortLevel.Length; i++)
        {
            if (pressesPerEffortLevel[i] - pressesPerEffortLevel[i-1] < 2)
            {
                pressesPerEffortLevel[i] = pressesPerEffortLevel[i-1] + 2;
            }
        }

        // Save the calculated values
        for (int i = 0; i < pressesPerEffortLevel.Length; i++)
        {
            PlayerPrefs.SetInt("PressesPerEffortLevel_" + i, pressesPerEffortLevel[i]);
        }

        Debug.Log("Presses per effort level set: " + string.Join(", ", pressesPerEffortLevel));
    }
}