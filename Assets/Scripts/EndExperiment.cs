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
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;

public class EndExperiment : MonoBehaviour
{
    [SerializeField] private ExperimentConfig config;
    [SerializeField] private LogManager logManager;
    [SerializeField] private TextMeshProUGUI totalTimeText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Image feedbackBackground;
    [SerializeField] private TMP_InputField feedbackInputField;
    [SerializeField] private TextMeshProUGUI submissionInstructionText;
    [SerializeField] private Button submitButton;

    [SerializeField] public AudioClip buttonClickSound;
    [SerializeField] private Button continueButton;
    private bool continueButtonClicked = false;
    [SerializeField] private AudioClip continueButtonSound; // New audio clip for continue button
    [Header("Resubmission Settings")]
    [SerializeField] private Button resubmitButton; // Assign in Inspector
    [SerializeField] private bool autoResubmitOnContinue = true; // Toggle auto-resubmit
    private bool submissionSuccess = false;

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

#if UNITY_WEBGL && !UNITY_EDITOR
[DllImport("__Internal")]
private static extern void WebGLAlert(string message);

[DllImport("__Internal")]
private static extern void BlockWindowClose();
#endif

    private float totalTime;
    private int totalScore;
    private bool isSubmitting = false;
    private AudioSource audioSource;
    private Coroutine hideMessageCoroutine;
    private Vector3 originalFeedbackScale;

    private void Awake()
    {
        // Ensure music continues playing in this final scene
        if (BackgroundMusicManager.Instance != null)
        {
            // Make sure music is playing (in case it somehow stopped)
            BackgroundMusicManager.Instance.PlayMusic();
        }
    }

    private void OnApplicationQuit()
    {
        // Optional: Stop music just before application quits
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.StopMusic();
        }

        if (!submissionSuccess)
        {
            // Save emergency backup (if not already saved)
            string csvContent = GetRobustLogData();
            SaveLocalBackup(csvContent);

#if UNITY_WEBGL && !UNITY_EDITOR
        // Show browser alert (WebGL only)
        WebGLAlert("Your data is being saved locally.");
        
        // Optional: Block accidental tab closing
        BlockWindowClose();
#endif
        }
    }

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

        if (feedbackInputField == null)
        {
            Debug.LogError("feedbackInputField is not assigned in the inspector!");
        }
        else
        {
            // Make sure it's interactable
            feedbackInputField.interactable = true;

            // Ensure it has the correct event listener
            feedbackInputField.onEndEdit.RemoveAllListeners();
            feedbackInputField.onEndEdit.AddListener(OnFeedbackEndEdit);
        }

        // Set the instruction text
        if (submissionInstructionText != null)
        {
            submissionInstructionText.text = "Click the 'Submit' button or Press 'Enter' to submit";
        }
        else
        {
            Debug.LogWarning("submissionInstructionText is not assigned in the inspector!");
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

        // Initialize resubmit button (hidden by default)
        if (resubmitButton != null)
        {
            resubmitButton.onClick.AddListener(ResubmitData);
            resubmitButton.gameObject.SetActive(false);
        }

        // Add button navigation controller
        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(submitButton);
        navigationController.AddElement(continueButton);
        navigationController.AddElement(resubmitButton);

        // Setup audio source if needed
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // Ensure LogManager is ready
        if (logManager != null)
        {
            // Log initial scene data
            Dictionary<string, string> initialData = new Dictionary<string, string>
        {
            {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
            {"Scene", "EndExperiment"},
            {"TotalTime", totalTime.ToString("F2")},
            {"TotalScore", totalScore.ToString()}
        };

            // Log that we're on the final scene
            logManager.LogEvent("EndExperimentSceneLoaded", initialData);
        }
    }


    private void Update()
    {
        // Check if we're not currently editing the feedback field
        bool canSubmit = feedbackInputField == null ||
                        (!feedbackInputField.isFocused && !string.IsNullOrEmpty(feedbackInputField.text));

        // Submit when Enter is pressed, but only when NOT editing feedback
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) &&
            !isSubmitting && canSubmit)
        {
            SubmitData();
        }

        // You could also add a check to activate the input field on click
        if (feedbackInputField != null && Input.GetMouseButtonDown(0))
        {
            // Check if mouse is over the input field (simplified)
            if (RectTransformUtility.RectangleContainsScreenPoint(
                feedbackInputField.GetComponent<RectTransform>(),
                Input.mousePosition))
            {
                feedbackInputField.ActivateInputField();
            }
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

    private void OnFeedbackEndEdit(string text)
    {
        // Only prevent submission when Enter is pressed in the input field
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            // This keeps focus in the input field
            feedbackInputField.ActivateInputField();

            // Prevent event propagation
            Event.current?.Use();
        }
    }

    private IEnumerator SubmitDataToServer(string csvContent)
    {
        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogError("CSV content is empty! Cannot submit to server.");
            ShowMessage("Error: No data to submit", true);
            isSubmitting = false;
            yield break;
        }

        // Validate the CSV data
        string[] lines = csvContent.Split('\n');
        Debug.Log($"Submitting CSV with {lines.Length} lines");

        if (lines.Length <= 2)
        {
            Debug.LogError("CSV data contains only headers or is incomplete!");
            ShowMessage("Error: Incomplete data", true);
            SaveLocalBackup(csvContent); // Save what we have anyway
            isSubmitting = false;
            yield break;
        }

        // Check for expected content
        bool hasExperimentSetup = csvContent.Contains("ExperimentSetup");
        bool hasExperimentStart = csvContent.Contains("ExperimentStart");

        if (!hasExperimentSetup || !hasExperimentStart)
        {
            Debug.LogWarning($"CSV data may be incomplete! ExperimentSetup: {hasExperimentSetup}, ExperimentStart: {hasExperimentStart}");
            // Continue with the submission anyway, but log the warning
        }

        string herokuUploadUrl = "https://effortpatch-0b3abd136749.herokuapp.com/upload";
        Debug.Log($"Uploading to URL: {herokuUploadUrl}");

        int maxRetries = 3;
        int retryCount = 0;
        bool success = false;

        // Add validation for upload content length
        Debug.Log($"Content to upload is {csvContent.Length} bytes");

        // Try to upload data in a standard way first
        while (!success && retryCount < maxRetries)
        {
            retryCount++;
            Debug.Log($"Upload attempt {retryCount}/{maxRetries}...");

            // Ensure we're converting all line endings to \n for consistency
            string normalizedContent = csvContent.Replace("\r\n", "\n").Replace('\r', '\n');
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(normalizedContent);

            UnityWebRequest request = new UnityWebRequest(herokuUploadUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 60; // 60 second timeout

            Debug.Log($"Sending {bodyRaw.Length} bytes to server...");

            // Show an informative message to the user
            ShowMessage($"Uploading data... Attempt {retryCount}", false, false);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Upload successful! Server response: {request.downloadHandler.text}");
                success = true;
                submissionSuccess = true;
                PlayerPrefs.SetInt("DataSubmitted", 1);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError($"Upload attempt {retryCount} failed: {request.error}");
                // Explicitly show the resubmit button on failure
                if (resubmitButton != null)
                {
                    // Ensure the button is both activated and made explicitly interactable
                    resubmitButton.gameObject.SetActive(true);
                    resubmitButton.interactable = true;

                    Debug.Log($"Resubmit button state - Active: {resubmitButton.gameObject.activeInHierarchy}, Interactable: {resubmitButton.interactable}");
                }
                if (submitButton != null)
                {
                    submitButton.gameObject.SetActive(false);
                }

                if (request.downloadHandler != null)
                {
                    Debug.LogError($"Server Response: {request.downloadHandler.text}");
                }

                if (retryCount < maxRetries)
                {
                    Debug.Log("Waiting before retry...");
                    ShowMessage($"Upload failed, retrying in 3 seconds...", true, false);
                    yield return new WaitForSeconds(3f);
                }
            }
        }

        if (success)
        {
            submissionSuccess = true;
            PlayerPrefs.SetInt("DataSubmitted", 1);
            PlayerPrefs.Save();
            ShowMessage("Thank you for your participation!", false, true, true);

            // Hide resubmit button (if submission succeeds)
            if (resubmitButton != null)
            {
                resubmitButton.gameObject.SetActive(false);
            }
        }
        else
        {
            ShowMessage("Server submission failed... Please retry manually by clicking the 'Resubmit Button' button!", true, true, true);

            // Show resubmit button (if submission fails)
            if (resubmitButton != null)
            {
                resubmitButton.gameObject.SetActive(true);
                resubmitButton.interactable = true;
                Debug.Log("Resubmit button activated and made interactable.");
            }
            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(false);
                submitButton.interactable = false;
            }
        }


        // Final state update
        if (!success)
        {
            ShowMessage("All retries failed. Click 'Resubmit' or save locally.", true);
            isSubmitting = false; // <-- Allow resubmission
            if (resubmitButton != null)
                resubmitButton.gameObject.SetActive(true);
            resubmitButton.interactable = true;
            Debug.Log("Resubmit button forcibly activated and made interactable after final failure.");
        }
        else
        {
            if (resubmitButton != null)
                resubmitButton.gameObject.SetActive(false);
        }

        // Enable continue button only if submission was attempted (success or failure)
        if (continueButton != null && redirectAfterSubmission)
        {
            continueButton.gameObject.SetActive(true);
        }

        isSubmitting = false;
    }

    public void ResubmitData()
    {
        if (isSubmitting) return;
        StartCoroutine(DelayedSubmission()); // Reuse existing submission logic
        ShowMessage("Resubmitting data...", false);
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

            // Also save to a secondary location for redundancy
            try
            {
                // Try Documents folder first
                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EffortPathBackups");
                Directory.CreateDirectory(documentsPath);
                string secondaryPath = Path.Combine(documentsPath, filename);
                File.WriteAllText(secondaryPath, csvContent);
                Debug.Log($"Secondary backup saved to: {secondaryPath}");

                // Also try saving in the application directory
                try
                {
                    string appPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
                    Directory.CreateDirectory(appPath);
                    string tertiaryPath = Path.Combine(appPath, filename);
                    File.WriteAllText(tertiaryPath, csvContent);
                    Debug.Log($"Tertiary backup saved to: {tertiaryPath}");
                }
                catch (Exception)
                {
                    // Ignore errors in tertiary backup
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Secondary backup failed: {e.Message}, trying alternative location");

                // Try desktop as fallback
                try
                {
                    string desktopPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "EffortPathBackups");
                    Directory.CreateDirectory(desktopPath);
                    string alternativePath = Path.Combine(desktopPath, filename);
                    File.WriteAllText(alternativePath, csvContent);
                    Debug.Log($"Alternative backup saved to: {alternativePath}");
                }
                catch (Exception)
                {
                    // Ignore errors in alternative backup
                }
            }

            return path;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save local backup: {e.Message}");
            return "Failed to save";
        }
    }

    private void EnsureLogCompleteness()
    {
        if (logManager != null)
        {
            // If we can't find ExperimentSetup in the log, add it now as a fallback
            string currentLog = logManager.GetCsvContent();
            if (!currentLog.Contains("ExperimentSetup"))
            {
                Dictionary<string, string> setupData = new Dictionary<string, string>
            {
                {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
                {"ParticipantAge", PlayerPrefs.GetInt("ParticipantAge", 0).ToString()},
                {"ParticipantGender", PlayerPrefs.GetString("ParticipantGender", "Unknown")},
                {"ExperimentVersion", Application.version},
                {"RestoredFromBackup", "True"}
            };

                logManager.LogEvent("ExperimentSetup", setupData);
            }

            // If we can't find ExperimentStart in the log, add it now as a fallback
            if (!currentLog.Contains("ExperimentStart"))
            {
                Dictionary<string, string> startData = new Dictionary<string, string>
            {
                {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
                {"StartTimeRestored", "True"},
                {"StartTime", PlayerPrefs.GetFloat("ExperimentStartTime", Time.time).ToString()}
            };

                logManager.LogEvent("ExperimentStart", startData);
            }
        }
    }

    private void SubmitData()
    {
        if (isSubmitting) return;

        isSubmitting = true;
        submitButton.interactable = false;

        // Play button click sound
        PlaySound(buttonClickSound);

        // Ensure log completeness before submission
        EnsureLogCompleteness();

        // Log the feedback text
        string feedback = "No feedback provided";
        if (feedbackInputField != null && !string.IsNullOrEmpty(feedbackInputField.text))
        {
            feedback = feedbackInputField.text;
        }

        LogFeedback(feedback);

        LogManager.Instance.LogEvent("FinalSubmissionData", new Dictionary<string, string>
    {
        {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
        {"ContinueButtonClicked", continueButtonClicked.ToString()},
        {"FeedbackProvided", (!string.IsNullOrEmpty(feedback)).ToString()},
        {"TotalTime", totalTime.ToString("F2")},
        {"TotalScore", totalScore.ToString()}
    });

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
        Debug.Log("Starting DelayedSubmission process");

        // Make sure both LogManager instances have finalized their logs
        bool logFinalized = false;

        // Try finalizing with both LogManager references to be thorough
        if (logManager != null)
        {
            Debug.Log("Finalizing logs with serialized logManager...");
            try
            {
                logManager.FinalizeLogFile();
                logFinalized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error finalizing logs with serialized logManager: {e.Message}");
            }
        }

        // Also try with LogManager.Instance if different
        if (LogManager.Instance != null && LogManager.Instance != logManager)
        {
            Debug.Log("Finalizing logs with LogManager.Instance...");
            try
            {
                LogManager.Instance.FinalizeLogFile();
                logFinalized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error finalizing logs with LogManager.Instance: {e.Message}");
            }
        }

        if (!logFinalized)
        {
            Debug.LogWarning("Could not finalize logs with either LogManager reference!");
        }

        // Wait to ensure file operations complete - increase this time
        Debug.Log("Waiting for log files to be finalized...");
        yield return new WaitForSeconds(3f);

        // Force a final log entry to ensure we have timestamp continuity
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("EndExperimentSubmission", new Dictionary<string, string> {
            {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
            {"TotalTime", totalTime.ToString("F2")},
            {"TotalScore", totalScore.ToString()},
            {"FeedbackProvided", (feedbackInputField != null && !string.IsNullOrEmpty(feedbackInputField.text)).ToString()}
        });

            // Wait additional time for this entry to be written
            yield return new WaitForSeconds(1f);
        }

        // Get the log data using our improved robust approach
        string csvContent = GetRobustLogData();

        // Detailed validation of retrieved content
        int lineCount = csvContent.Split('\n').Length;
        Debug.Log($"CSV data to be submitted contains {lineCount} rows and {csvContent.Length} characters");

        if (lineCount <= 2)
        {
            Debug.LogError("WARNING: CSV data appears to contain only headers!");
            ShowMessage("Error: Could not read complete data", true);

            // Create emergency backup with minimal info
            csvContent = CreateEmergencyCSV();
        }

        // Always create a local backup before attempting server upload
        string backupPath = SaveLocalBackup(csvContent);
        Debug.Log($"Local backup created at: {backupPath}");

        // Proceed with server submission
        StartCoroutine(SubmitDataToServer(csvContent));
    }

    private string CreateEmergencyCSV()
    {
        Debug.Log("Creating emergency minimal CSV data...");

        string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
        int participantAge = PlayerPrefs.GetInt("ParticipantAge", 0);
        string participantGender = PlayerPrefs.GetString("ParticipantGender", "Unknown");
        string tirednessRating = PlayerPrefs.GetString("TirednessRating", "");
        string feedback = feedbackInputField != null ? feedbackInputField.text : "No feedback provided";

        StringBuilder sb = new StringBuilder();

        // Create header
        sb.AppendLine("Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                     "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                     "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,ParticipantFeedback");

        // Create summary data row
        sb.AppendLine($"{DateTime.Now},Summary,{participantId},{participantAge},{participantGender},,,,,,,,," +
                      $"{totalTime.ToString("F2")},,{totalScore},,,,,,,{tirednessRating},{feedback}");

        // Create a distinct filename for this emergency backup
        string emergencyPath = Path.Combine(Application.persistentDataPath,
                                           $"emergency_backup_{participantId}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.csv");
        try
        {
            File.WriteAllText(emergencyPath, sb.ToString());
            Debug.Log($"Emergency backup created at: {emergencyPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to write emergency backup: {e.Message}");
        }

        return sb.ToString();
    }

    private string GetRobustLogData()
    {
        string csvContent = "";
        Debug.Log("Attempting to get robust log data...");

        // Get participant ID and today's date for filename matching
        string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");

        // Try multiple possible locations - add more search locations
        List<string> possiblePaths = new List<string>
    {
        Application.persistentDataPath,
        Application.dataPath + "/_ExpData",
        Path.Combine(Application.dataPath, "../_ExpData"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EffortPathBackups"),
        Directory.GetCurrentDirectory()
    };

        // Check for LogManager path first
        if (logManager != null && !string.IsNullOrEmpty(logManager.LogFilePath))
        {
            string logDirectory = Path.GetDirectoryName(logManager.LogFilePath);
            if (!string.IsNullOrEmpty(logDirectory))
                possiblePaths.Insert(0, logDirectory);
        }

        // Try LogManager.Instance path if different
        if (LogManager.Instance != null && LogManager.Instance != logManager &&
            !string.IsNullOrEmpty(LogManager.Instance.LogFilePath))
        {
            string instanceDirectory = Path.GetDirectoryName(LogManager.Instance.LogFilePath);
            if (!string.IsNullOrEmpty(instanceDirectory) && !possiblePaths.Contains(instanceDirectory))
                possiblePaths.Insert(0, instanceDirectory);
        }

        // Additional debug logging
        Debug.Log($"Participant ID: {participantId}, Today's date: {todayDate}");
        Debug.Log($"Searching {possiblePaths.Count} possible directories for log files");

        foreach (string basePath in possiblePaths)
        {
            if (!Directory.Exists(basePath))
            {
                Debug.Log($"Directory does not exist: {basePath}");
                continue;
            }

            Debug.Log($"Searching for log files in: {basePath}");

            try
            {
                // Search for any CSV files matching our pattern
                string[] allCsvFiles = Directory.GetFiles(basePath, "*.csv", SearchOption.TopDirectoryOnly);
                Debug.Log($"Found {allCsvFiles.Length} total CSV files in {basePath}");

                // Debug all found CSV files
                foreach (string file in allCsvFiles)
                {
                    Debug.Log($"Found CSV file: {Path.GetFileName(file)} - Modified: {File.GetLastWriteTime(file)}");
                }

                List<string> matchingFiles = new List<string>();

                // First try exact match with participant ID and today's date
                foreach (string file in allCsvFiles)
                {
                    string filename = Path.GetFileName(file);
                    if (filename.Contains(participantId) && (filename.Contains(todayDate) || filename.Contains("decision_task_log")))
                    {
                        matchingFiles.Add(file);
                        // Debug.Log($"Exact match found: {filename}");
                    }
                }

                // If no exact matches, look for any CSV files with participant ID
                if (matchingFiles.Count == 0)
                {
                    foreach (string file in allCsvFiles)
                    {
                        if (Path.GetFileName(file).Contains(participantId))
                        {
                            matchingFiles.Add(file);
                            Debug.Log($"Participant ID match found: {Path.GetFileName(file)}");
                        }
                    }
                }

                // If still no matches, take any CSV files modified today as a last resort
                if (matchingFiles.Count == 0)
                {
                    foreach (string file in allCsvFiles)
                    {
                        if (File.GetLastWriteTime(file).Date == DateTime.Now.Date)
                        {
                            matchingFiles.Add(file);
                            Debug.Log($"Date match found: {Path.GetFileName(file)}");
                        }
                    }
                }

                if (matchingFiles.Count > 0)
                {
                    // Sort by last write time to get the most recent
                    matchingFiles.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    Debug.Log($"Found {matchingFiles.Count} potential log files, sorted by most recent");

                    foreach (string file in matchingFiles)
                    {
                        try
                        {
                            string fileContent;
                            // Use a file sharing approach that works even if file is open
                            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                            {
                                fileContent = reader.ReadToEnd();
                            }

                            // Check for expected headers/content
                            bool hasExperimentSetup = fileContent.Contains("ExperimentSetup");
                            bool hasExperimentStart = fileContent.Contains("ExperimentStart");

                            Debug.Log($"File {Path.GetFileName(file)} contains: " +
                                     $"ExperimentSetup: {hasExperimentSetup}, " +
                                     $"ExperimentStart: {hasExperimentStart}, " +
                                     $"Lines: {fileContent.Split('\n').Length}");

                            // Validate the content has actual data beyond just headers
                            if (!string.IsNullOrEmpty(fileContent) && fileContent.Split('\n').Length > 2)
                            {
                                csvContent = fileContent;
                                Debug.Log($"Successfully read {csvContent.Split('\n').Length} lines from log file: {file}");
                                // Write out first 100 chars to verify content
                                Debug.Log($"Content preview: {csvContent.Substring(0, Math.Min(100, csvContent.Length))}");
                                break;
                            }
                            else
                            {
                                Debug.LogWarning($"Found file {file} but content appears empty or has only headers");
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error reading log file {file}: {e.Message}");
                        }
                    }

                    // If we got content, break out of the path loop
                    if (!string.IsNullOrEmpty(csvContent) && csvContent.Split('\n').Length > 2)
                    {
                        Debug.Log("Found valid CSV content, breaking path search loop");
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error searching in directory {basePath}: {e.Message}");
            }
        }

        // If we still don't have content, try to get it directly from LogManager
        if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 2)
        {
            Debug.Log("Couldn't find valid log file, trying LogManager directly...");

            // Try LogManager instance first
            if (LogManager.Instance != null)
            {
                try
                {
                    csvContent = LogManager.Instance.GetCsvContent();
                    Debug.Log($"Got {csvContent.Split('\n').Length} lines from LogManager.Instance.GetCsvContent()");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error getting CSV content from LogManager.Instance: {e.Message}");
                }
            }

            // Try serialized logManager if it's different
            if ((string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 2) &&
                logManager != null && logManager != LogManager.Instance)
            {
                try
                {
                    csvContent = logManager.GetCsvContent();
                    Debug.Log($"Got {csvContent.Split('\n').Length} lines from serialized logManager.GetCsvContent()");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error getting CSV content from serialized logManager: {e.Message}");
                }
            }
        }

        // Add feedback to the CSV data if we have any
        if (!string.IsNullOrEmpty(csvContent) && feedbackInputField != null)
        {
            string feedback = feedbackInputField.text;
            if (!string.IsNullOrEmpty(feedback))
            {
                csvContent = AddFeedbackToCSV(csvContent, feedback);
            }
        }

        // Final content check
        if (!string.IsNullOrEmpty(csvContent))
        {
            bool hasExperimentSetup = csvContent.Contains("ExperimentSetup");
            bool hasExperimentStart = csvContent.Contains("ExperimentStart");
            Debug.Log($"Final CSV contains: ExperimentSetup: {hasExperimentSetup}, " +
                     $"ExperimentStart: {hasExperimentStart}, Lines: {csvContent.Split('\n').Length}");
        }

        return csvContent;
    }

    // Helper method to properly add feedback to existing CSV data
    private string AddFeedbackToCSV(string csvContent, string feedback)
    {
        string[] lines = csvContent.Split('\n');

        // Check if header has ParticipantFeedback column
        bool hasParticipantFeedback = lines[0].Contains("ParticipantFeedback");

        if (!hasParticipantFeedback)
        {
            // Add the column to header
            lines[0] += ",ParticipantFeedback";
        }

        StringBuilder result = new StringBuilder();
        result.AppendLine(lines[0]);

        // Process all non-header lines
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            // For the last non-empty line, add feedback
            if (i == lines.Length - 1 || string.IsNullOrWhiteSpace(lines[i + 1]))
            {
                // Check if we need to add the feedback column
                if (!hasParticipantFeedback)
                {
                    lines[i] += "," + feedback;
                }
                else
                {
                    // Replace existing feedback or add it if missing
                    string[] fields = lines[i].Split(',');
                    int feedbackIndex = Array.IndexOf(lines[0].Split(','), "ParticipantFeedback");

                    if (feedbackIndex >= fields.Length)
                    {
                        // Column exists in header but not in this row
                        lines[i] += "," + feedback;
                    }
                    else
                    {
                        // Column exists, replace value
                        fields[feedbackIndex] = feedback;
                        lines[i] = string.Join(",", fields);
                    }
                }
            }

            result.AppendLine(lines[i]);
        }

        return result.ToString();
    }

    private IEnumerator FinalizeLogWithRetry()
    {
        int maxAttempts = 3;
        for (int i = 0; i < maxAttempts; i++)
        {
            Debug.Log($"Finalizing log attempt {i + 1}/{maxAttempts}...");

            // Call FinalizeLogFile
            logManager.FinalizeLogFile();

            // Also try the coroutine version if available
            try
            {
                StartCoroutine(logManager.FinalizeAndUploadLogWithDelay());
                Debug.Log("Finalize and upload completed.");
                break;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error in FinalizeAndUploadLogWithDelay: {e.Message}");

                if (i < maxAttempts - 1)
                {
                    Debug.Log("Waiting before retry...");
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    public void ContinueToSurvey()
    {
        // Auto-resubmit if enabled and data isn't submitted
        if (autoResubmitOnContinue && !submissionSuccess && PlayerPrefs.GetInt("DataSubmitted", 0) != 1)
        {
            ResubmitData(); // Trigger resubmission
            ShowMessage("Submitting data...", false);
            return; // Exit early (user can click Continue again later)
        }

        // Block redirection if still not submitted
        if (!submissionSuccess && PlayerPrefs.GetInt("DataSubmitted", 0) != 1)
        {
            ShowMessage("Please submit data first!", true);
            return;
        }

        // Proceed to redirection if submitted
        continueButtonClicked = true;
        PlaySound(continueButtonSound);
        StartCoroutine(RedirectAndQuit());

        // Log the button click
        LogManager.Instance.LogEvent("ContinueButtonClicked", new Dictionary<string, string>
    {
        {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
        {"ButtonAction", "RedirectToSurvey"},
        {"RedirectURL", redirectUrl},
        {"TimeSinceStart", totalTime.ToString("F2")}
    });


        // Disable the button to prevent multiple clicks
        if (continueButton != null)
        {
            continueButton.interactable = false;
        }

        // Play a nice sound when the continue button is clicked
        PlaySound(continueButtonSound);

        // Show a thank you message without playing a sound and make it persist
        ShowMessage("Thank you! Closing now...", false, false, true);

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
        }

        // Wait for the delay before quitting
        yield return new WaitForSeconds(quitDelay);

        Debug.Log("Quitting application...");

        // No need to hide message as application is quitting
        // HideMessage(); // Uncommenting this would hide the message before quitting

#if UNITY_EDITOR
        // If in editor, stop play mode
        UnityEditor.EditorApplication.isPlaying = false;
#else
    // If in standalone build, quit the application
    Application.Quit();
#endif
    }

    private void ShowMessage(string message, bool isError = false, bool playSound = true, bool persist = false)
    {
        if (feedbackText == null) return;

        // Stop any existing hide message coroutine
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
            hideMessageCoroutine = null;
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

        // Start coroutine to hide message after duration ONLY if persist is false
        if (!persist)
        {
            hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay());
        }
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


    // Supports both English and Chinese input
    public void LogFeedback(string feedback)
    {
        // Validate and sanitize feedback input
        if (string.IsNullOrWhiteSpace(feedback))
        {
            Debug.Log("No feedback provided");
            return;
        }

        // Ensure the feedback is properly encoded to support Unicode characters
        string sanitizedFeedback = System.Text.Encoding.UTF8.GetString(
            System.Text.Encoding.UTF8.GetBytes(feedback)
        );

        // Use the existing LogManager to log the feedback
        if (LogManager.Instance != null)
        {
            // Log feedback to the main experiment log file
            LogManager.Instance.LogEvent("ParticipantFeedback", new Dictionary<string, string>
        {
            {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
            {"ParticipantFeedback", sanitizedFeedback}, // Encoded feedback
            {"FeedbackLanguage", DetectLanguage(sanitizedFeedback)},
            {"AdditionalInfo", "Feedback collected on EndExperiment scene"}
        });
            Debug.Log($"ParticipantFeedback logged: {sanitizedFeedback}");
        }
        else
        {
            // Fallback to PlayerPrefs
            PlayerPrefs.SetString("ParticipantFeedback", sanitizedFeedback);
            PlayerPrefs.Save();
        }
    }

    private string DetectLanguage(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "Unknown";

        // Simple Unicode block detection for language
        bool hasChinese = text.Any(c =>
            // CJK (Chinese, Japanese, Korean) Unified Ideographs
            (c >= 0x4E00 && c <= 0x9FFF) ||
            // CJK Unified Ideographs Extension A
            (c >= 0x3400 && c <= 0x4DBF) ||
            // CJK Unified Ideographs Extension B-G
            (c >= 0x20000 && c <= 0x2FFFF)
        );

        bool hasEnglish = text.Any(c =>
            (c >= 'A' && c <= 'Z') ||
            (c >= 'a' && c <= 'z')
        );

        // Determine language
        if (hasChinese && !hasEnglish)
            return "Chinese";
        else if (hasEnglish && !hasChinese)
            return "English";
        else if (hasChinese && hasEnglish)
            return "Mixed";
        else
            return "Other";
    }
}
