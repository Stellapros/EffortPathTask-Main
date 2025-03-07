using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;

public class ArrowKeyCounter : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI instructionText;
    public GameObject previousButton;
    public Image progressBarFill;

    [Header("Loggings")]
    private LogManager logManager;
    // private int calibrationPhaseNumber = 0;
    private float lastKeyPressTime;
    private System.Collections.Generic.List<float> interKeyIntervals = new System.Collections.Generic.List<float>();

    [Header("Calibration Settings")]
    private float calibrationTime = 5f;
    private float breakTime = 5f;

    // For slower filling, use a smaller number (e.g., 0.01f means 100 presses)
    // For faster filling, use a larger number (e.g., 0.02f means 50 presses)
    private int targetPresses = 60;  // Target number of presses to fill the bar
    // private float progressIncrement = 0.01667f;  // 0.0125f means it takes 80 presses to fill (1/0.0125 = 80)
    private int counter = 0;
    private float currentProgress = 0f;
    private float elapsedTime = 0f;
    private bool calibrationInProgress = false;
    private bool isInBreak = false;
    private int currentPhase = 0;
    private int totalPhases = 3;
    private int[] phaseResults;
    private bool phaseStarted = false;
    private string nextSceneName = "TourGame";
    private string previousSceneName = "StartScreen";

    private string[] instructions = new string[]
    {
        "Before you embark on your adventure, let's calibrate your explorer's energy levels! For the next 5 seconds, press the RIGHT direction button (→) using your RIGHT hand as fast as you can. The more you press, the more your progress bar fills. Push it to the max!",
        "Great effort! But can you go even faster? Now, TRY TO BEAT YOUR SCORE and push the bar even HIGHER! Get ready... and start pressing when you're prepared.",
        "This is your LAST CHANCE to reach the top! Give it your all. Every tap counts! GO! Explorer!"
    };
    private class CalibrationStats
    {
        public int MaximumPresses { get; set; }
        public int AveragePresses { get; set; }
        public float MaxPressRate { get; set; }
    }

    private void Start()
    {
        logManager = LogManager.Instance;
        if (logManager == null)
        {
            Debug.LogError("LogManager not found!");
        }

        phaseResults = new int[totalPhases];
        UpdateCounterText();
        UpdateTimerText();
        ShowCurrentInstruction();
        ResetProgressBar();

        if (previousButton != null)
        {
            previousButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(GoToPreviousScene);
        }
    }

    private void Update()
    {
        if (isInBreak)
        {
            return;
        }

        bool keyPressed = Input.GetKeyDown(KeyCode.RightArrow);

        if (!calibrationInProgress)
        {
            if (keyPressed)
            {
                StartCalibration();
            }
        }
        else
        {
            if (!phaseStarted)
            {
                if (keyPressed)
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

                if (keyPressed)
                {
                    IncrementCounter();
                    UpdateProgressBar();
                }

                if (elapsedTime >= calibrationTime)
                {
                    EndCalibration();
                }
            }
        }
    }

    private void GoToPreviousScene()
    {
        SceneManager.LoadScene(previousSceneName);
    }


    // private void UpdateProgressBar()
    // {
    //     if (progressBarFill != null)
    //     {
    //         // Update based on number of presses relative to target
    //         float progress = (float)counter / targetPresses;
    //         progressBarFill.fillAmount = Mathf.Clamp01(progress);
    //     }
    // }

    private void UpdateProgressBar()
    {
        if (progressBarFill != null)
        {
            currentProgress = (float)counter / targetPresses;
            progressBarFill.fillAmount = Mathf.Clamp01(currentProgress);
        }
    }


    private void ResetProgressBar()
    {
        if (progressBarFill != null)
        {
            currentProgress = 0f;
            progressBarFill.fillAmount = 0f;
        }
    }

    private void StartCalibration()
    {
        calibrationInProgress = true;
        phaseStarted = false;
        counter = 0;
        elapsedTime = 0f;
        interKeyIntervals.Clear();
        lastKeyPressTime = Time.time;
        ResetProgressBar();
        UpdateCounterText();
        UpdateTimerText();

        // Log phase start
        // logManager?.LogCalibrationPhaseStart(calibrationPhaseNumber + 1, calibrationTime);
        // Debug.Log($"Calibration phase {currentPhase + 1} ready to start");
    }

    private void IncrementCounter()
    {
        counter++;
        float currentTime = Time.time;
        float interval = currentTime - lastKeyPressTime;
        lastKeyPressTime = currentTime;

        if (counter > 1) // Don't log first interval
        {
            interKeyIntervals.Add(interval);
        }

        // Log each keypress
        // logManager?.LogCalibrationKeyPress(calibrationPhaseNumber + 1, counter, interval, elapsedTime);
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

        // Log individual phase result
        logManager?.LogCalibrationPhase(currentPhase + 1, counter, calibrationTime);

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

    // private void CalculateFinalResultsAndProceed()
    // {
    //     int averageKeyPresses = Mathf.RoundToInt((float)phaseResults.Average());
    //     PlayerPrefs.SetInt("totalKeyPresses", averageKeyPresses);

    //     CalculateAndSetPressesPerEffortLevel(averageKeyPresses);

    //     // Log final calibration results
    //     logManager?.LogCalibrationResults(
    //         phaseResults[0],
    //         phaseResults[1],
    //         phaseResults[2],
    //         averageKeyPresses,
    //         PlayerPrefs.GetInt("PressesPerEffortLevel_0"),
    //         PlayerPrefs.GetInt("PressesPerEffortLevel_1"),
    //         PlayerPrefs.GetInt("PressesPerEffortLevel_2")
    //     );

    //     SceneManager.LoadScene(nextSceneName);
    // }

    private IEnumerator BreakBetweenPhases()
    {
        isInBreak = true;
        float breakTimeLeft = breakTime;

        while (breakTimeLeft > 0)
        {
            instructionText.text = $"Great job! Next chance to beat your score starts in {breakTimeLeft:F0} seconds...";
            yield return new WaitForSeconds(0.1f);
            breakTimeLeft -= 0.1f;
        }

        isInBreak = false;
        StartCalibration();
        ShowCurrentInstruction();
    }

    // Revised calibration = 1:3:5 till the press rate reaches 10
    // Using 10%, 30%, and 50% of the average press rate
    // private void CalculateAndSetPressesPerEffortLevel(int averageKeyPresses)
    // {
    //     float pressRate = (float)averageKeyPresses / calibrationTime;
    //     int[] pressesPerEffortLevel = new int[3];

    //     pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(pressRate * 0.1f));
    //     pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(pressRate * 0.3f));
    //     pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(pressRate * 0.5f));

    //     for (int i = 1; i < pressesPerEffortLevel.Length; i++)
    //     {
    //         if (pressesPerEffortLevel[i] - pressesPerEffortLevel[i - 1] < 2)
    //         {
    //             pressesPerEffortLevel[i] = pressesPerEffortLevel[i - 1] + 2;
    //         }
    //     }

    //     for (int i = 0; i < pressesPerEffortLevel.Length; i++)
    //     {
    //         PlayerPrefs.SetInt($"PressesPerEffortLevel_{i}", pressesPerEffortLevel[i]);
    //         Debug.Log($"Saved PressesPerEffortLevel_{i}: {pressesPerEffortLevel[i]}");
    //     }
    //     PlayerPrefs.Save();

    //     string effortLevelsString = string.Join(", ", pressesPerEffortLevel);
    //     Debug.Log($"Calibration completed. Average press rate: {pressRate}, Final presses per effort level: {effortLevelsString}");

    //     string logEntry = $"{System.DateTime.Now}: Calibration - Average press rate: {pressRate}, Effort levels: {effortLevelsString}";
    //     System.IO.File.AppendAllText("calibration_log.txt", logEntry + System.Environment.NewLine);
    // }


    private void CalculateFinalResultsAndProceed()
    {
        // Calculate both maximum and average
        CalibrationStats stats = CalculateCalibrationStats();

        // Store both values
        PlayerPrefs.SetInt("totalKeyPresses", stats.MaximumPresses);
        PlayerPrefs.SetInt("averageKeyPresses", stats.AveragePresses);

        // Calculate and save effort levels based on maximum
        int[] pressesPerEffortLevel = CalculateEffortLevels(stats.MaxPressRate);
        SaveEffortLevels(pressesPerEffortLevel);

        // Log results with both values
        LogCalibrationResults(pressesPerEffortLevel, stats);

        // Proceed to next scene
        SceneManager.LoadScene(nextSceneName);
    }

    private CalibrationStats CalculateCalibrationStats()
    {
        return new CalibrationStats
        {
            MaximumPresses = phaseResults.Max(),
            AveragePresses = Mathf.RoundToInt((float)phaseResults.Average()),
            MaxPressRate = (float)phaseResults.Max() / calibrationTime  // Keep this for logging purposes
        };
    }

    private int[] CalculateEffortLevels(float maxPressRate)
    {
        int[] pressesPerEffortLevel = new int[3];
        int maxPresses = phaseResults.Max();
        float pressesPerMovement = maxPresses / 5f;

        // // Calculate effort levels based on press rate percentages
        // pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(maxPressRate * 0.49f));
        // pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(maxPressRate * 0.56f));
        // pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(maxPressRate * 0.70f));

        // // Calculate effort levels based on press rate percentages
        // pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(maxPressRate * 0.10f));
        // pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(maxPressRate * 0.30f));
        // pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(maxPressRate * 0.50f));

        // // Calculate effort levels based on press rate percentages
        // pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(maxPressRate * 0.30f));
        // pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(maxPressRate * 0.60f));
        // pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(maxPressRate * 0.90f));

        // Calculate effort levels based on presses per movement
        pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(pressesPerMovement * 0.30f));
        pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(pressesPerMovement * 0.60f));
        pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(pressesPerMovement * 0.90f));

        // // Ensure minimum difference of 2 between levels
        // for (int i = 1; i < pressesPerEffortLevel.Length; i++)
        // {
        //     if (pressesPerEffortLevel[i] - pressesPerEffortLevel[i - 1] < 2)
        //     {
        //         pressesPerEffortLevel[i] = pressesPerEffortLevel[i - 1] + 2;
        //     }
        // }

        return pressesPerEffortLevel;
    }

    private void SaveEffortLevels(int[] pressesPerEffortLevel)
    {
        for (int i = 0; i < pressesPerEffortLevel.Length; i++)
        {
            PlayerPrefs.SetInt($"PressesPerEffortLevel_{i}", pressesPerEffortLevel[i]);
            Debug.Log($"Saved PressesPerEffortLevel_{i}: {pressesPerEffortLevel[i]}");
        }
        PlayerPrefs.Save();
    }

    private void LogCalibrationResults(int[] pressesPerEffortLevel, CalibrationStats stats)
    {
        // Console logging
        string effortLevelsString = string.Join(", ", pressesPerEffortLevel);
        Debug.Log($"Calibration completed. Maximum press rate: {stats.MaxPressRate}, " +
                  $"Maximum presses: {stats.MaximumPresses}, Average presses: {stats.AveragePresses}, " +
                  $"Final presses per effort level: {effortLevelsString}");

        // File logging
        string logEntry = $"{System.DateTime.Now}: Calibration - " +
                         $"Maximum press rate: {stats.MaxPressRate}, " +
                         $"Maximum presses: {stats.MaximumPresses}, " +
                         $"Average presses: {stats.AveragePresses}, " +
                         $"Effort levels: {effortLevelsString}";
        System.IO.File.AppendAllText("calibration_log.txt", logEntry + System.Environment.NewLine);

        // LogManager logging
        logManager?.LogCalibrationResults(
            phaseResults[0],
            phaseResults[1],
            phaseResults[2],
            stats.MaximumPresses,  // Using maximum for calibration
            pressesPerEffortLevel[0],
            pressesPerEffortLevel[1],
            pressesPerEffortLevel[2]
        );
    }
}