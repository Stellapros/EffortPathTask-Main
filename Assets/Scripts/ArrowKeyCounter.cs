using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using Unity.Collections;

public class ArrowKeyCounter : MonoBehaviour
{
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI instructionText;
    private int counter = 0;
    private float elapsedTime = 0f;
    private float calibrationTime = 5f;
    private float breakTime = 5f; // Duration of the break between phases
    private bool calibrationInProgress = false;
    private bool isInBreak = false;
    private int currentPhase = 0;
    private int totalPhases = 3;
    private int[] phaseResults;
    private bool phaseStarted = false;
    private string nextSceneName = "TourGame";


    // private string[] instructions = new string[]
    // {
    //     "Before you embark on your adventure, we need to calibrate your explorer's energy levels. Press the direction buttons (↑ or ↓ or ← or →) as quickly as you can within 5 seconds. Keep this up - think of it as a warm-up exercise! This calibration ensures the game is perfectly tuned to your personal button-pressing speed and stamina.",
    //     "Great! Now TRY AND BEAT YOUR SCORE! Start pressing when you are ready.",
    //     "LAST CHANCE to beat your score!"
    // };
    private string[] instructions = new string[]
    {
        "Before you embark on your adventure, let's calibrate your explorer's energy levels! For the next 5 seconds, press the direction buttons (↑, ↓, ←, or →) as quickly as you can. Think of it as your warm-up exercise! This calibration helps us fine-tune the game to match your personal speed and stamina.",
        "Great job! Now, TRY TO BEAT YOUR SCORE! Get ready and start pressing when you’re prepared.",
        "This is your LAST CHANCE to beat your score! Give it your all, explorer!"
    };


    private void Start()
    {
        phaseResults = new int[totalPhases];
        UpdateCounterText();
        UpdateTimerText();
        ShowCurrentInstruction();
    }

    private void Update()
    {
        if (isInBreak)
        {
            // Do nothing during break, wait for coroutine to finish
            return;
        }

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
            if (!phaseStarted)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
                    Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
                {
                    phaseStarted = true;
                    elapsedTime = 0f;
                    IncrementCounter();
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
    }

    private void StartCalibration()
    {
        calibrationInProgress = true;
        phaseStarted = false;
        counter = 0;
        elapsedTime = 0f;
        UpdateCounterText();
        UpdateTimerText();
        Debug.Log($"Calibration phase {currentPhase + 1} ready to start");
    }

    private void IncrementCounter()
    {
        counter++;
        UpdateCounterText();
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

    private void ShowCurrentInstruction()
    {
        instructionText.text = instructions[currentPhase];
    }

    private void EndCalibration()
    {
        calibrationInProgress = false;
        phaseStarted = false;
        phaseResults[currentPhase] = counter;
        Debug.Log($"Calibration phase {currentPhase + 1} ended. Key presses: {counter}");

        currentPhase++;
        if (currentPhase < totalPhases)
        {
            StartCoroutine(BreakBetweenPhases());
        }
        else
        {
            CalculateFinalResultsAndProceed();
        }
    }

    private IEnumerator BreakBetweenPhases()
    {
        isInBreak = true;
        float breakTimeLeft = breakTime;

        while (breakTimeLeft > 0)
        {
            instructionText.text = $"Great job! Next opportunity to beat your score starts in {breakTimeLeft:F0} seconds...";
            yield return new WaitForSeconds(0.1f);
            breakTimeLeft -= 0.1f;
        }

        isInBreak = false;
        StartCalibration();
        ShowCurrentInstruction();
    }

    private void CalculateFinalResultsAndProceed()
    {
        int averageKeyPresses = Mathf.RoundToInt((float)phaseResults.Average());
        PlayerPrefs.SetInt("totalKeyPresses", averageKeyPresses);
        Debug.Log($"All calibration phases completed. Average key presses: {averageKeyPresses}");

        // Calculate presses per effort level
        CalculateAndSetPressesPerEffortLevel(averageKeyPresses);

        // Load the next scene or start the experiment
        SceneManager.LoadScene(nextSceneName);
    }

    private void CalculateAndSetPressesPerEffortLevel(int averageKeyPresses)
    {
        float pressRate = (float)averageKeyPresses / calibrationTime;
        int[] pressesPerEffortLevel = new int[3];

        pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(pressRate * 0.5f));  // Easy
        pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(pressRate * 0.7f));  // Medium
        pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(pressRate * 0.9f));  // Hard

        for (int i = 1; i < pressesPerEffortLevel.Length; i++)
        {
            if (pressesPerEffortLevel[i] - pressesPerEffortLevel[i - 1] < 2)
            {
                pressesPerEffortLevel[i] = pressesPerEffortLevel[i - 1] + 2;
            }
        }

        for (int i = 0; i < pressesPerEffortLevel.Length; i++)
        {
            PlayerPrefs.SetInt($"PressesPerEffortLevel_{i}", pressesPerEffortLevel[i]);
            Debug.Log($"Saved PressesPerEffortLevel_{i}: {pressesPerEffortLevel[i]}");
        }
        PlayerPrefs.Save();

        string effortLevelsString = string.Join(", ", pressesPerEffortLevel);
        Debug.Log($"Calibration completed. Average press rate: {pressRate}, Final presses per effort level: {effortLevelsString}");

        string logEntry = $"{System.DateTime.Now}: Calibration - Average press rate: {pressRate}, Effort levels: {effortLevelsString}";
        System.IO.File.AppendAllText("calibration_log.txt", logEntry + System.Environment.NewLine);
    }
}