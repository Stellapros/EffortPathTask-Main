using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using System.IO;
using System.Threading.Tasks;

public class EndExperiment : MonoBehaviour
{
    [SerializeField] private ExperimentConfig config;
    [SerializeField] private LogManager logManager;
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Image feedbackBackground;
    [SerializeField] private TMP_InputField feedbackInputField;

    [SerializeField] private Button submitButton;
    [SerializeField] public AudioClip buttonClickSound;
    [SerializeField] private Button continueButton;
    [SerializeField] private AudioClip continueButtonSound; // New audio clip for continue button

    [Header("Feedback Settings")]
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color errorColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private AudioClip successSound;  // Sound to play on successful submission
    [SerializeField] private AudioClip errorSound;    // Sound to play on failed submission
    [SerializeField] private float animationDuration = 0.5f;  // Duration of feedback animation
    [SerializeField] private float scaleFactor = 1.2f;  // How much to scale the feedback text

    [Header("Redirection")]
    // does not work if refer to this URL, need to paste the exact link to the "RedirectAndQuit"
    [SerializeField] private string redirectUrl = "https://bhampsychology.eu.qualtrics.com/jfe/form/SV_bjaQmPSeGFMooXI";
    [SerializeField] private bool redirectAfterSubmission = true;
    [SerializeField] private float quitDelay = 1.5f; // Delay before quitting application

    [Header("Server Settings")]
    [SerializeField] private bool offlineMode = false;

    private float totalTime;
    private int totalScore;
    private bool isSubmitting = false;
    private AudioSource audioSource;
    private Coroutine hideMessageCoroutine;
    private Vector3 originalFeedbackScale;

    private void Start()
    {
        // Add null checks for serialized fields
        if (totalTimeText == null) Debug.LogError("totalTimeText is not assigned in the inspector!");
        if (totalScoreText == null) Debug.LogError("totalScoreText is not assigned in the inspector!");
        if (submitButton == null) Debug.LogError("submitButton is not assigned in the inspector!");
        if (feedbackText == null) Debug.LogError("feedbackText is not assigned in the inspector!");
        if (feedbackBackground == null) Debug.LogError("feedbackBackground is not assigned in the inspector!");

        // Initial UI setup
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
            originalFeedbackScale = feedbackText.transform.localScale;

            if (feedbackBackground != null)
            {
                feedbackBackground.gameObject.SetActive(false);
            }
        }

        // Initially hide the continue button
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(false);
            continueButton.onClick.AddListener(ContinueToSurvey);
        }

        // Show cursor and make it interactable
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Retrieve the experiment start time and calculate total time
        float startTime = PlayerPrefs.GetFloat("ExperimentStartTime", Time.time);
        totalTime = Time.time - startTime;

        DisplayTotalTime();
        DisplayTotalScore();

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitData);
        }

        // Add button navigation controller
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(submitButton);
        navigationController.AddElement(continueButton);

        // Setup audio source if needed
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Ensure LogManager is ready
        if (logManager == null)
        {
            logManager = FindAnyObjectByType<LogManager>();
            if (logManager == null)
            {
                Debug.LogError("LogManager not found in the scene!");
            }
            else
            {
                Debug.Log("LogManager found in the scene.");
            }
        }

        if (logManager != null && !logManager.gameObject.activeInHierarchy)
        {
            Debug.Log("LogManager GameObject was inactive. Enabling it now.");
            logManager.gameObject.SetActive(true);
        }

        if (logManager != null)
        {
            logManager.EnsureLogFileInitialized();
        }
    }

    private void Update()
    {
        // Check for Space key press
        if (Input.GetKeyDown(KeyCode.Space) && !isSubmitting)
        {
            SubmitData();
        }
    }

    private void DisplayTotalTime()
    {
        if (totalTimeText != null)
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalTime);
            totalTimeText.text = $"Total Time: {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }
    }

    private void DisplayTotalScore()
    {
        if (ScoreManager.Instance != null)
        {
            totalScore = ScoreManager.Instance.GetTotalScore();
        }
        else
        {
            Debug.LogError("ScoreManager instance not found!");
        }

        if (totalScoreText != null)
        {
            totalScoreText.text = $"Total Score: {totalScore}";
        }
    }

    private void SubmitData()
    {
        if (isSubmitting) return;

        isSubmitting = true;
        submitButton.interactable = false;

        // Play button click sound
        PlaySound(buttonClickSound);

        // Log the feedback text
        string feedback = "No feedback provided";
        if (feedbackInputField != null && !string.IsNullOrEmpty(feedbackInputField.text))
        {
            feedback = feedbackInputField.text;
        }

        LogFeedback(feedback);

        // Save to PlayerPrefs as backup
        PlayerPrefs.SetString("ParticipantFeedback", feedback);
        PlayerPrefs.Save();

        // In offline mode, skip data submission
        if (offlineMode)
        {
            ShowMessage("Thank you for your participation! (Offline Mode)", false);

            // Show continue button instead of redirecting automatically
            if (continueButton != null && redirectAfterSubmission)
            {
                continueButton.gameObject.SetActive(true);
            }

            isSubmitting = false;
            return;
        }

        // IMPORTANT: Add a small delay to ensure all log data is written
        StartCoroutine(DelayedSubmission());
    }

    private IEnumerator DelayedSubmission()
    {
       // Ensure LogManager is ready
        if (logManager != null && logManager.gameObject.activeInHierarchy)
        {
            try
            {
                logManager.StartCoroutine(logManager.FinalizeAndUploadLogWithDelay());
                Debug.Log("Log finalized successfully.");
            }
            catch (Exception e)
            {
                Debug.Log($"Error finalizing logs: {e.Message}");
            }
        }
        else
        {
            Debug.Log("LogManager is null or inactive!");
        }

        yield return new WaitForSeconds(2f);

        // Now read directly from the log file
        string csvContent = ReadAllLogData();

        // Debug log to check what data we're about to submit
        Debug.Log($"Data to submit contains {csvContent.Split('\n').Length} rows");

        if (config == null || string.IsNullOrEmpty(config.ServerUrl))
        {
            Debug.LogWarning("ExperimentConfig or ServerUrl not set, proceeding with offline mode");
            ShowMessage("Thank you for your participation!", false);

            // Show continue button instead of redirecting automatically
            if (continueButton != null && redirectAfterSubmission)
            {
                continueButton.gameObject.SetActive(true);
            }

            isSubmitting = false;
            yield break;
        }

        StartCoroutine(SubmitDataToServer(csvContent));
    }

    public void ContinueToSurvey()
    {
        // Disable the button to prevent multiple clicks
        if (continueButton != null)
        {
            continueButton.interactable = false;
        }

        // Play a nice sound when the continue button is clicked
        PlaySound(continueButtonSound);

        // Show a thank you message without playing a sound
        ShowMessage("Thank you! Closing application...", false, false);

        // Open the URL and then quit the application
        StartCoroutine(RedirectAndQuit());
    }

    private IEnumerator RedirectAndQuit()
    {
        // Short delay to let the button sound play
        yield return new WaitForSeconds(0.3f);

        // Open the URL first if needed
        if (redirectAfterSubmission && !string.IsNullOrEmpty(redirectUrl))
        {
            Debug.Log("Opening URL: " + redirectUrl);
            Application.OpenURL("https://bhampsychology.eu.qualtrics.com/jfe/form/SV_bjaQmPSeGFMooXI");
            // Application.OpenURL("https://www.google.com");
        }

        // Wait for the delay before quitting
        yield return new WaitForSeconds(quitDelay);

        Debug.Log("Quitting application...");

        // Quit the application
#if UNITY_EDITOR
        // If in editor, stop play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // If in standalone build, quit the application
            Application.Quit();
#endif
    }
private string ReadAllLogData()
{
    Debug.Log("Starting to read log data...");
    

    // Ensure the log file is finalized before reading
    if (logManager != null && logManager.gameObject.activeInHierarchy)
    {
        try
        {
            logManager.FinalizeLogFile();
            Debug.Log("Log file finalized.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error finalizing log: {e.Message}");
        }
    }

    // Add a small delay to allow file system changes to propagate
    System.Threading.Thread.Sleep(1000);

    string csvContent = "";
    string logFilePath = logManager != null ? logManager.LogFilePath : "";

    if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
    {
        try
        {
            csvContent = File.ReadAllText(logFilePath);
            Debug.Log($"Read {csvContent.Split('\n').Length} lines from file.");
            Debug.Log($"CSV Content: {csvContent}"); // Add this line to log the entire CSV content
        }
        catch (Exception e)
        {
            Debug.LogError($"Error reading file: {e.Message}");
        }
    }

    // Ensure fallback method is used if data is missing
    if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 1)
    {
        try
        {
            csvContent = logManager.GetCsvContent();
            Debug.Log($"Method 2: Read {csvContent.Split('\n').Length} lines via LogManager.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting CSV via LogManager: {e.Message}");
        }
    }

    // If still empty, create a minimal dataset
    if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 1)
    {
        Debug.LogWarning("Could not read data from log file. Creating minimal dataset.");
        csvContent = CreateMinimalDataset();
    }

    return csvContent;
}


    // private string ReadAllLogData()
    // {
    //     Debug.Log("Starting to read log data...");

    //     if (logManager != null)
    //     {
    //         try
    //         {
    //             logManager.FinalizeLogFile(); // Force log file to be fully written
    //             Debug.Log("Log file finalized.");
    //         }
    //         catch (Exception e)
    //         {
    //             Debug.LogError($"Error finalizing log: {e.Message}");
    //         }
    //     }

    //     System.Threading.Thread.Sleep(1000); // Allow time for file writing

    //     // string logFilePath = logManager != null ? logManager.LogFilePath : "";
    //     string logFilePath = "/Users/m.li.14@bham.ac.uk/Documents/GitHub/EffortPathTask-2D/Assets/_ExpData/";


    //     if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
    //     {
    //         Debug.LogError($"Log file not found at expected path: {logFilePath}");
    //         return "";
    //     }

    //     string csvContent = File.ReadAllText(logFilePath);
    //     Debug.Log($"✅ CSV Read Success: {csvContent.Split('\n').Length} lines found.");

    //     // Ensure CSV isn't just headers
    //     if (csvContent.Split('\n').Length <= 1)
    //     {
    //         Debug.LogError("❌ CSV contains only headers. Possible logging failure.");
    //     }

    //     return csvContent;
    // }


    private string CreateMinimalDataset()
    {
        // Create minimal dataset with header and one data row
        string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
        string feedback = PlayerPrefs.GetString("ParticipantFeedback", "No feedback");

        return "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
            "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
            "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,FeedbackText\n" +
            $"{DateTime.Now},Summary,{participantId},{PlayerPrefs.GetInt("ParticipantAge", 0)}," +
            $"{PlayerPrefs.GetString("ParticipantGender", "Unknown")},,,,,,,,," +
            $"{totalTime.ToString("F2")},,{totalScore},,,,,,,{feedback}\n";
    }

private IEnumerator SubmitDataToServer(string csvContent = null)
{
    if (string.IsNullOrEmpty(csvContent))
    {
        csvContent = ReadAllLogData();
    }

    Debug.Log($"Uploading {csvContent.Split('\n').Length} lines of CSV data.");
    Debug.Log($"CSV Content Before Upload:\n{csvContent}");

    if (csvContent.Split('\n').Length <= 1)
    {
        Debug.LogError("CSV only contains headers. Aborting upload.");
        ShowMessage("Error: No valid data to upload!", true);
        isSubmitting = false;
        yield break;
    }

     string googleScriptURL = "https://script.google.com/macros/s/AKfycbzFthiIj9whUhk0yNAQpFAOTZqRzX73ojOuyYwNP59xIZ3vZmZJxvabbIdccBjMIrTd/exec";

    WWWForm form = new WWWForm();
    form.AddField("csv_data", csvContent);
    Debug.Log($"CSV data being sent:\n{csvContent}");

    using (UnityWebRequest request = UnityWebRequest.Post(googleScriptURL, form))
    {
        request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
        request.timeout = 30;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("CSV successfully uploaded to Google Drive!");
            Debug.Log("Server response: " + request.downloadHandler.text);
            ShowMessage("Thank you for your participation!", false);
        }
        else
        {
            Debug.LogError($"Upload failed: {request.error}");
            if (request.downloadHandler != null && !string.IsNullOrEmpty(request.downloadHandler.text))
            {
                Debug.LogError("Server response: " + request.downloadHandler.text);
            }

            ShowMessage("Server submission failed, but data was saved locally!", true);
        }
    }

    if (continueButton != null && redirectAfterSubmission)
    {
        continueButton.gameObject.SetActive(true);
    }

    isSubmitting = false;
}

    private string SaveLocalBackup(string csvContent)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
            string filename = $"experiment_data_{participantId}_{timestamp}.csv";

            // Save to persistent data path (survives app reinstalls)
            string path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllText(path, csvContent);

            Debug.Log($"Local backup saved to: {path}");
            return path;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save local backup: {e.Message}");
            return "Failed to save";
        }
    }

    private void ShowMessage(string message, bool isError = false, bool playSound = true)
    {
        if (feedbackText == null) return;

        // Stop any existing hide message coroutine
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
        }

        // Set up the feedback text
        feedbackText.text = message;
        feedbackText.color = isError ? errorColor : successColor;

        // Set up the background
        if (feedbackBackground != null)
        {
            feedbackBackground.gameObject.SetActive(true);
            feedbackBackground.color = isError ?
                new Color(errorColor.r, errorColor.g, errorColor.b, 0.2f) :
                new Color(successColor.r, successColor.g, successColor.b, 0.2f);
        }

        // Play appropriate sound only if playSound is true
        if (playSound)
        {
            PlaySound(isError ? errorSound : successSound);
        }

        // Reset scale and show the text
        feedbackText.transform.localScale = originalFeedbackScale;
        feedbackText.gameObject.SetActive(true);

        // Start animation coroutine
        StartCoroutine(AnimateFeedback(isError));

        // Start coroutine to hide message after duration
        hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay());
    }

    private IEnumerator AnimateFeedback(bool isError)
    {
        float elapsedTime = 0f;
        Vector3 targetScale = originalFeedbackScale * scaleFactor;

        // Scale up animation
        while (elapsedTime < animationDuration / 2)
        {
            feedbackText.transform.localScale = Vector3.Lerp(
                originalFeedbackScale,
                targetScale,
                elapsedTime / (animationDuration / 2)
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset elapsed time
        elapsedTime = 0f;

        // Scale down animation
        while (elapsedTime < animationDuration / 2)
        {
            feedbackText.transform.localScale = Vector3.Lerp(
                targetScale,
                originalFeedbackScale,
                elapsedTime / (animationDuration / 2)
            );

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Ensure we end exactly at the original scale
        feedbackText.transform.localScale = originalFeedbackScale;
    }

    private void HideMessage()
    {
        if (feedbackText == null) return;
        feedbackText.gameObject.SetActive(false);

        if (feedbackBackground != null)
        {
            feedbackBackground.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideMessageAfterDelay()
    {
        yield return new WaitForSeconds(messageDuration);
        HideMessage();
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    public void LogFeedback(string feedback)
    {
        // Use the existing LogManager to log the tiredness rating
        if (LogManager.Instance != null)
        {
            // Log feedback to the main experiment log file
            LogManager.Instance.LogEvent("ParticipantFeedback", new Dictionary<string, string>
        {
        {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
        {"ParticipantFeedback", feedback}, // Add the feedback text to the log
        {"AdditionalInfo", "Feedback collected on EndExperiment scene"}
        });
            Debug.Log($"ParticipantFeedback logged: {feedback}");
        }
        else
        {
            // Also save to PlayerPrefs as fallback
            PlayerPrefs.SetString("ParticipantFeedback", feedback);
            PlayerPrefs.Save();
        }
    }
}

internal class JSONObject
{
    private string responseText;

    public JSONObject(string responseText)
    {
        this.responseText = responseText;
    }

    internal bool GetBoolean(string v)
    {
        throw new NotImplementedException();
    }

    internal string GetString(string v)
    {
        throw new NotImplementedException();
    }
}