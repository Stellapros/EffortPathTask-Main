using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

public class ArrowKeyCounter : MonoBehaviour
{
    /// <summary>
    /// This script manages the calibration process for the arrow key counter.
    /// </summary>
    
    [Header("UI References")]
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI instructionText;
    public GameObject previousButton;
    public Image progressBarFill;

    [Header("Loggings")]
    private LogManager logManager;
    private float lastKeyPressTime;
    private System.Collections.Generic.List<float> interKeyIntervals = new System.Collections.Generic.List<float>();

    [Header("Calibration Settings")]
    private float calibrationTime = 5f;
    private float breakTime = 5f;
    private float webGLTimeScale = 1.0f; // Add this to adjust for potential time scaling issues

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

    [Header("Sound Effects")]
    public AudioClip keyPressSound;  // Assign in Inspector
    [Range(0, 1)] public float keyPressVolume = 0.5f; // Volume slider in Inspector
    private AudioSource audioSource; // Will create dynamically

    // private string[] instructions = new string[]
    // {
    //     "Before you embark on your adventure, let's calibrate your explorer's energy levels! For the next 5 seconds, press the RIGHT direction button (→) using your RIGHT hand as fast as you can. The more you press, the more your progress bar fills. Push it to the max!",
    //     "Great effort! But can you go even faster? Now, TRY TO BEAT YOUR SCORE and push the bar even HIGHER! Get ready... and start pressing when you're prepared.",
    //     "This is your LAST CHANCE to reach the top! Give it your all. Every tap counts! GO! Explorer!"
    // };
    //     private string[] instructions = new string[]
    // {
    //         "Before you embark on your adventure, let's calibrate your explorer's energy levels! For the next 5 seconds, press the RIGHT direction button (→) using your RIGHT hand as fast as you can. The more you press, the more your progress bar fills. Push it to the max!",
    //         "Now, TRY TO BEAT YOUR SCORE and push the bar even HIGHER! Get ready... go.",
    //         "LAST CHANCE to reach the top! Give it your all. Every tap counts! GO! Explorer!"
    // };

    // Modify the instructions array to include the GO signal
    private string[] instructions = new string[]
    {
    "Before you embark on your adventure, let's calibrate your explorer's energy levels!\n\nFor the next 5 seconds, press the RIGHT direction button (→) using your RIGHT hand as fast as you can.\n\nPress RIGHT ARROW when ready...",
    "Now, TRY TO BEAT YOUR SCORE and push the bar even HIGHER!\n\nPress RIGHT ARROW when ready to GO!",
    "LAST CHANCE to reach the top! Give it your all!\n\nPress RIGHT ARROW when ready to GO!"
    };

    // Add this new variable
    private string goSignal = "GO!";

    private class CalibrationStats
    {
        public int MaximumPresses { get; set; }
        public int AveragePresses { get; set; }
        public float MaxPressRate { get; set; }
    }

    private void Start()
    {
        // Add this check for WebGL platform
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // Adjust time scale if needed (can tweak based on testing)
            webGLTimeScale = 1.0f;

            // WebGL might need different audio settings
            if (audioSource != null)
            {
                audioSource.ignoreListenerPause = true;
            }
        }

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

        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
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
            // Start calibration immediately and show GO signal
            instructionText.text = goSignal;
            StartCalibration();
            // Process this first key press
            phaseStarted = true;
            elapsedTime = 0f;
            IncrementCounter();
        }
    }
    else
    {
        // Modified to use unscaled delta time for WebGL
        elapsedTime += Time.unscaledDeltaTime * webGLTimeScale;
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

    private void GoToPreviousScene()
    {
        SceneManager.LoadScene(previousSceneName);
    }

    private void UpdateProgressBar()
    {
        if (progressBarFill != null)
        {
            currentProgress = (float)counter / targetPresses;
            progressBarFill.fillAmount = Mathf.Clamp01(currentProgress);

            // Play sound with volume control
            if (keyPressSound != null)
            {
                audioSource.PlayOneShot(keyPressSound, keyPressVolume);
            }
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
        // Don't reset phaseStarted here since we're starting immediately
        counter = 0;
        elapsedTime = 0f;
        interKeyIntervals.Clear();
        lastKeyPressTime = Time.time;
        ResetProgressBar();
        UpdateCounterText();
        UpdateTimerText();
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

    private IEnumerator BreakBetweenPhases()
    {
        isInBreak = true;
        float breakTimeLeft = breakTime;
        float lastUpdateTime = Time.unscaledTime;

        while (breakTimeLeft > 0)
        {
            // More reliable timing for WebGL
            float currentTime = Time.unscaledTime;
            float delta = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            breakTimeLeft -= delta;
            instructionText.text = $"Great job! Next round starts in {Mathf.Ceil(breakTimeLeft)} seconds...";
            yield return null; // Simpler yield for WebGL
        }

        isInBreak = false;
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


    // Nw method to handle potential WebGL focus issues
    private void OnApplicationFocus(bool hasFocus)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            if (hasFocus)
            {
                // Resume timing if we regain focus
                webGLTimeScale = 1.0f;
            }
            else
            {
                // Pause timing if we lose focus
                webGLTimeScale = 0.0f;
            }
        }
    }

    private void CalculateFinalResultsAndProceed()
    {
        // Calculate both maximum and average
        CalibrationStats stats = CalculateCalibrationStats();

        // Store both values
        PlayerPrefs.SetInt("totalKeyPresses", stats.MaximumPresses);
        PlayerPrefs.SetInt("averageKeyPresses", stats.AveragePresses);

        // Calculate and save effort levels based on MAXIMUM
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

        // // Calculate effort levels based on presses per movement -- ROUND 3: 22 May 2025 N = 25
        // pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(pressesPerMovement * 0.20f));
        // pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(pressesPerMovement * 0.40f));
        // pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(pressesPerMovement * 0.60f));

        // Calculate effort levels based on presses per movement -- ROUND 4: xx May 2025 N = xx
        pressesPerEffortLevel[0] = Mathf.Max(1, Mathf.RoundToInt(pressesPerMovement * 0.30f));
        pressesPerEffortLevel[1] = Mathf.Max(2, Mathf.RoundToInt(pressesPerMovement * 0.50f));
        pressesPerEffortLevel[2] = Mathf.Max(3, Mathf.RoundToInt(pressesPerMovement * 0.70f));

        // Ensure minimum difference of 1 between levels
        for (int i = 1; i < pressesPerEffortLevel.Length; i++)
        {
            if (pressesPerEffortLevel[i] - pressesPerEffortLevel[i - 1] < 1)
            {
                pressesPerEffortLevel[i] = pressesPerEffortLevel[i - 1] + 1;
            }
        }

        return pressesPerEffortLevel;
    }

    private void SaveEffortLevels(int[] pressesPerEffortLevel)
    {
        for (int i = 0; i < pressesPerEffortLevel.Length; i++)
        {
            PlayerPrefs.SetInt($"PressesPerEffortLevel_{i + 1}", pressesPerEffortLevel[i]);
            Debug.Log($"Saved PressesPerEffortLevel_{i + 1}: {pressesPerEffortLevel[i]}");

            Debug.Log($"PressesPerEffortLevel_1: {PlayerPrefs.GetInt("PressesPerEffortLevel_1")}");
            Debug.Log($"PressesPerEffortLevel_2: {PlayerPrefs.GetInt("PressesPerEffortLevel_2")}");
            Debug.Log($"PressesPerEffortLevel_3: {PlayerPrefs.GetInt("PressesPerEffortLevel_3")}");
        }
        PlayerPrefs.Save();
    }

    private void LogCalibrationResults(int[] pressesPerEffortLevel, CalibrationStats stats)
    {
        float phase1Rate = (float)phaseResults[0] / calibrationTime;
        float phase2Rate = (float)phaseResults[1] / calibrationTime;
        float phase3Rate = (float)phaseResults[2] / calibrationTime;

        logManager?.LogEvent("CalibrationComplete", new Dictionary<string, string>
    {
        {"CalibrationPhase1", phaseResults[0].ToString()},
        {"CalibrationPhase2", phaseResults[1].ToString()},
        {"CalibrationPhase3", phaseResults[2].ToString()},
        {"CalibrationMax", stats.MaximumPresses.ToString()},
        {"CalibrationAvg", stats.AveragePresses.ToString()},
        {"CalibrationEasy", pressesPerEffortLevel[0].ToString()},
        {"CalibrationMedium", pressesPerEffortLevel[1].ToString()},
        {"CalibrationHard", pressesPerEffortLevel[2].ToString()},
        {"CalibrationPhase1Rate", phase1Rate.ToString("F2")},
        {"CalibrationPhase2Rate", phase2Rate.ToString("F2")},
        {"CalibrationPhase3Rate", phase3Rate.ToString("F2")},
        {"CalibrationMaxRate", stats.MaxPressRate.ToString("F2")}
    });
        Debug.Log(
        $"Easy: {pressesPerEffortLevel[0]}, " +
        $"Medium: {pressesPerEffortLevel[1]}, " +
        $"Hard: {pressesPerEffortLevel[2]}"
    );

        Debug.Log(
        $"Total Presses: {phaseResults.Max()} | " +
        $"No Clamping: {pressesPerEffortLevel[0]}/{pressesPerEffortLevel[1]}/{pressesPerEffortLevel[2]} | " +
        $"With Clamping: {pressesPerEffortLevel[0]}/{pressesPerEffortLevel[1]}/" +
        $"{Mathf.Max(pressesPerEffortLevel[2], pressesPerEffortLevel[1] + 2)}"
    );

    }
}