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

    // private string ReadAllLogData()
    // {
    //     string csvContent = "";

    //     if (logManager != null)
    //     {
    //         try
    //         {
    //             // Force finalize the log file first
    //             if (logManager.gameObject.activeInHierarchy)
    //             {
    //                 logManager.FinalizeLogFile(); // Ensure all data is written to the file
    //             }

    //             // Try to read from the log file path directly
    //             if (!string.IsNullOrEmpty(logManager.LogFilePath) && File.Exists(logManager.LogFilePath))
    //             {

    //                 // Use File.ReadAllText to get the raw file content
    //                 csvContent = File.ReadAllText(logManager.LogFilePath);
    //                 Debug.Log($"Read {csvContent.Split('\n').Length} lines directly from log file: {logManager.LogFilePath}");
    //             }
    //             else
    //             {
    //                 // Fallback to GetCsvContent
    //                 csvContent = logManager.GetCsvContent();
    //                 Debug.Log($"Read {csvContent.Split('\n').Length} lines using GetCsvContent method");
    //             }
    //         }
    //         catch (Exception e)
    //         {
    //             Debug.LogError($"Error reading log file: {e.Message}");
    //         }
    //     }

    //     // Get the feedback from the InputField or from PlayerPrefs as fallback
    //     string feedback = "";
    //     if (feedbackInputField != null && !string.IsNullOrEmpty(feedbackInputField.text))
    //     {
    //         feedback = feedbackInputField.text;
    //     }
    //     else
    //     {
    //         feedback = PlayerPrefs.GetString("ParticipantFeedback", "No feedback provided");
    //     }

    //     // Validation to ensure we have content
    //     if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 1)
    //     {
    //         Debug.LogWarning("CSV content is empty or contains only headers. Creating minimal data.");

    //         // Generate minimal dataset with headers
    //         csvContent = "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
    //             "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
    //             "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,FeedbackText\n" + // Changed Feedback to FeedbackText for consistency
    //             $"{DateTime.Now},Summary,{PlayerPrefs.GetString("ParticipantID", "Unknown")},{PlayerPrefs.GetInt("ParticipantAge", 0)}," +
    //             $"{PlayerPrefs.GetString("ParticipantGender", "Unknown")},,,,,,,,," +
    //             $"{totalTime.ToString("F2")},,{totalScore},,,,,,,{PlayerPrefs.GetString("TirednessRating", "")},{feedback}\n";
    //     }
    //     else
    //     {
    //         // Append the feedback to the existing CSV content
    //         string[] lines = csvContent.Split('\n');

    //         // Check if the header already has the FeedbackText column
    //         bool headerHasFeedback = lines[0].Contains("FeedbackText");

    //         // If the header doesn't have the FeedbackText column, add it
    //         if (!headerHasFeedback)
    //         {
    //             lines[0] += ",FeedbackText"; // Add FeedbackText column to the header
    //         }

    //         // Rebuild the CSV content with the feedback
    //         string newCsvContent = lines[0] + "\n"; // Start with the header

    //         for (int i = 1; i < lines.Length; i++)
    //         {
    //             if (string.IsNullOrEmpty(lines[i])) continue;

    //             // Only add the feedback to the last line (summary line)
    //             if (i == lines.Length - 1 || i == lines.Length - 2 && string.IsNullOrEmpty(lines[lines.Length - 1]))
    //             {
    //                 if (!lines[i].Contains(",ParticipantFeedback,") && !lines[i].EndsWith(",ParticipantFeedback"))
    //                 {
    //                     lines[i] += $",{feedback}";
    //                 }
    //             }

    //             newCsvContent += lines[i] + "\n";
    //         }

    //         csvContent = newCsvContent;
    //     }

    //     // Log the actual content for debugging
    //     Debug.Log($"CSV data to be submitted has {csvContent.Split('\n').Length} lines and {csvContent.Length} characters");

    //     return csvContent;
    // }

    private string ReadAllLogData()
    {
        string csvContent = "";

        if (logManager != null)
        {
            try
            {
                // Force finalize the log file first
                if (logManager.gameObject.activeInHierarchy)
                {
                    Debug.Log("Calling FinalizeLogFile...");
                    logManager.FinalizeLogFile(); // Ensure all data is written to the file
                    Debug.Log($"Successfully finalized log file at: {logManager.LogFilePath}");

                    // Add a small delay to ensure file system has completed the write
                    System.Threading.Thread.Sleep(500);
                }

                // Try to read from the log file path directly
                if (!string.IsNullOrEmpty(logManager.LogFilePath) && File.Exists(logManager.LogFilePath))
                {
                    try
                    {
                        // Use File.ReadAllText to get the raw file content
                        csvContent = File.ReadAllText(logManager.LogFilePath);
                        Debug.Log($"Read {csvContent.Split('\n').Length} lines directly from log file: {logManager.LogFilePath}");

                        // Check the first and last few characters of the content for debugging
                        int startChars = Math.Min(50, csvContent.Length);
                        int endChars = Math.Min(50, csvContent.Length);
                        Debug.Log($"First {startChars} chars: {csvContent.Substring(0, startChars)}");
                        if (csvContent.Length > endChars)
                        {
                            Debug.Log($"Last {endChars} chars: {csvContent.Substring(csvContent.Length - endChars)}");
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error reading log file directly: {e.Message}");
                    }
                }

                // If content is empty or only has headers, try GetCsvContent
                if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 2)
                {
                    Debug.Log("Direct file read failed or returned minimal content. Trying GetCsvContent method...");
                    try
                    {
                        csvContent = logManager.GetCsvContent();
                        Debug.Log($"Read {csvContent.Split('\n').Length} lines using GetCsvContent method");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error using GetCsvContent: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during log reading process: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("LogManager is null!");
        }

        // Get the feedback from the InputField or from PlayerPrefs as fallback
        string feedback = "";
        if (feedbackInputField != null && !string.IsNullOrEmpty(feedbackInputField.text))
        {
            feedback = feedbackInputField.text;
        }
        else
        {
            feedback = PlayerPrefs.GetString("ParticipantFeedback", "No feedback provided");
        }

        // Check content again after all attempts
        if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 2)
        {
            Debug.LogWarning("CSV content is still empty or contains only headers. Creating minimal data.");

            // Generate minimal dataset with headers
            csvContent = "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,ParticipantFeedback\n" +
                $"{DateTime.Now},Summary,{PlayerPrefs.GetString("ParticipantID", "Unknown")},{PlayerPrefs.GetInt("ParticipantAge", 0)}," +
                $"{PlayerPrefs.GetString("ParticipantGender", "Unknown")},,,,,,,,," +
                $"{totalTime.ToString("F2")},,{totalScore},,,,,,,{PlayerPrefs.GetString("TirednessRating", "")},{feedback}\n";
        }
        else
        {
            // Correctly append the feedback to the CSV content
            // First check if ParticipantFeedback column already exists in the header
            string[] lines = csvContent.Split('\n');
            bool headerHasFeedback = lines[0].Contains("ParticipantFeedback");

            // If the header doesn't have ParticipantFeedback, we need to add it
            if (!headerHasFeedback)
            {
                Debug.Log("Adding ParticipantFeedback column to header");
                string newHeader = lines[0] + ",ParticipantFeedback";

                // Rebuild the CSV content with the modified header
                string newCsvContent = newHeader + "\n";
                for (int i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrEmpty(lines[i])) continue;

                    // For the last non-empty line, add the feedback value
                    if (i == lines.Length - 1 || (i < lines.Length - 1 && string.IsNullOrEmpty(lines[i + 1])))
                    {
                        newCsvContent += lines[i] + "," + feedback + "\n";
                    }
                    else
                    {
                        newCsvContent += lines[i] + "\n";
                    }
                }
                csvContent = newCsvContent;
            }
            else
            {
                // If header already has ParticipantFeedback column, ensure the feedback is in the last row
                bool feedbackAdded = false;
                for (int i = lines.Length - 1; i >= 1; i--)
                {
                    if (!string.IsNullOrEmpty(lines[i]))
                    {
                        // Check if this line already has a value for ParticipantFeedback
                        string[] values = lines[i].Split(',');
                        int feedbackIndex = Array.IndexOf(lines[0].Split(','), "ParticipantFeedback");

                        if (feedbackIndex >= 0 && feedbackIndex < values.Length && string.IsNullOrEmpty(values[feedbackIndex]))
                        {
                            // Replace the empty feedback with the current feedback
                            values[feedbackIndex] = feedback;
                            lines[i] = string.Join(",", values);
                            feedbackAdded = true;
                        }
                        break;
                    }
                }

                // If we didn't add the feedback to an existing column, we need to manually add it to the last line
                if (!feedbackAdded)
                {
                    for (int i = lines.Length - 1; i >= 1; i--)
                    {
                        if (!string.IsNullOrEmpty(lines[i]))
                        {
                            lines[i] += "," + feedback;
                            break;
                        }
                    }
                }

                // Rebuild the CSV content
                csvContent = string.Join("\n", lines);
            }
        }

        // Debug log to verify content
        Debug.Log($"Final CSV data to be submitted has {csvContent.Split('\n').Length} lines and {csvContent.Length} characters");

        return csvContent;
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

        string herokuUploadUrl = "https://effortpatch-0b3abd136749.herokuapp.com/upload";
        Debug.Log($"Uploading to URL: {herokuUploadUrl}");

        int maxRetries = 3;
        int retryCount = 0;
        bool success = false;

        // Try to break the data into chunks if it's large
        // Heroku might have limitations on request size
        if (csvContent.Length > 500000) // If larger than ~500KB
        {
            Debug.Log("Data is large, will try to compress it before sending");
            byte[] originalData = Encoding.UTF8.GetBytes(csvContent);

            // Use compression to reduce data size
            using (MemoryStream compressedStream = new MemoryStream())
            {
                using (System.IO.Compression.GZipStream gzipStream = new System.IO.Compression.GZipStream(
                    compressedStream, System.IO.Compression.CompressionMode.Compress))
                {
                    gzipStream.Write(originalData, 0, originalData.Length);
                }

                byte[] compressedData = compressedStream.ToArray();
                Debug.Log($"Compressed data from {originalData.Length} to {compressedData.Length} bytes");

                // Use the compressed data for upload
                while (!success && retryCount < maxRetries)
                {
                    retryCount++;
                    Debug.Log($"Upload attempt {retryCount}/{maxRetries} with compressed data...");

                    // UnityWebRequest request = new UnityWebRequest(herokuUploadUrl, "POST");
                    // request.uploadHandler = new UploadHandlerRaw(compressedData);
                    // request.downloadHandler = new DownloadHandlerBuffer();
                    // request.SetRequestHeader("Content-Type", "application/gzip");
                    // request.SetRequestHeader("Content-Encoding", "gzip");
                    // request.timeout = 60;

                    UnityWebRequest request = new UnityWebRequest(herokuUploadUrl, "POST");
                    // Ensure we're converting all line endings to \n for consistency
                    string normalizedContent = csvContent.Replace("\r\n", "\n").Replace('\r', '\n');
                    byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(normalizedContent);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");
                    request.SetRequestHeader("Accept", "application/json");
                    request.timeout = 60; // Increased timeout for larger data

                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        Debug.Log($"Compressed upload successful! Server response: {request.downloadHandler.text}");
                        success = true;
                    }
                    else
                    {
                        Debug.LogError($"Compressed upload attempt {retryCount} failed: {request.error}");
                        yield return new WaitForSeconds(2f);
                    }
                }
            }
        }

        // If compression upload failed or wasn't attempted, try normal upload
        if (!success)
        {
            retryCount = 0;
            while (!success && retryCount < maxRetries)
            {
                retryCount++;
                Debug.Log($"Upload attempt {retryCount}/{maxRetries}...");

                UnityWebRequest request = new UnityWebRequest(herokuUploadUrl, "POST");
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(csvContent);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "text/plain");
                request.SetRequestHeader("Accept", "application/json");
                request.timeout = 60; // Increased timeout for larger data

                Debug.Log($"Sending {bodyRaw.Length} bytes to server...");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Upload successful! Server response: {request.downloadHandler.text}");
                    success = true;
                }
                else
                {
                    Debug.LogError($"Upload attempt {retryCount} failed: {request.error}");
                    if (request.downloadHandler != null)
                    {
                        Debug.LogError($"Server Response: {request.downloadHandler.text}");
                    }

                    if (retryCount < maxRetries)
                    {
                        Debug.Log("Waiting before retry...");
                        yield return new WaitForSeconds(2f);
                    }
                }
            }
        }

        if (success)
        {
            ShowMessage("Thank you for your participation!", false, true, true);
            Debug.Log("Upload successful. Local backup already saved.");
        }
        else
        {
            ShowMessage("Server submission failed, but data was saved locally!", true, true, true);
            Debug.Log("All upload attempts failed. Using local backup.");
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

            // Also save to a secondary location for redundancy
            try
            {
                string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EffortPathBackups");
                Directory.CreateDirectory(documentsPath);
                string secondaryPath = Path.Combine(documentsPath, filename);
                File.WriteAllText(secondaryPath, csvContent);
                Debug.Log($"Secondary backup saved to: {secondaryPath}");
            }
            catch (Exception)
            {
                // Ignore errors in secondary backup
            }

            return path;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save local backup: {e.Message}");
            return "Failed to save";
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

    // private IEnumerator DelayedSubmission()
    // {
    //     // First ensure the log is finalized before we try to get its content
    //     if (logManager != null && logManager.gameObject.activeInHierarchy)
    //     {
    //         try
    //         {
    //             // This ensures all buffer data is written to the file
    //             logManager.StartCoroutine(logManager.FinalizeAndUploadLogWithDelay());
    //             Debug.Log("Log finalized successfully.");
    //         }
    //         catch (Exception e)
    //         {
    //             Debug.LogError($"Error finalizing logs: {e.Message}");
    //         }
    //     }
    //     else
    //     {
    //         Debug.LogWarning("LogManager is null or inactive.");
    //     }

    //     // Add a short delay to ensure file writes complete
    //     yield return new WaitForSeconds(2f);

    //     // Now read directly from the log file
    //     string csvContent = ReadAllLogData();

    //     // Debug log to check what data we're about to submit
    //     Debug.Log($"Data to submit contains {csvContent.Split('\n').Length} rows");

    //     if (config == null || string.IsNullOrEmpty(config.ServerUrl))
    //     {
    //         Debug.LogWarning("ExperimentConfig or ServerUrl not set, proceeding with offline mode");
    //         ShowMessage("Thank you for your participation!", false);

    //         // Show continue button instead of redirecting automatically
    //         if (continueButton != null && redirectAfterSubmission)
    //         {
    //             continueButton.gameObject.SetActive(true);
    //         }

    //         isSubmitting = false;
    //         yield break;
    //     }


    //     string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
    //     string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
    //     string fileName = $"experiment_data_{participantId}_{timestamp}.csv";

    //     StartCoroutine(SubmitDataToServer(csvContent));

    // }


    private IEnumerator DelayedSubmission()
    {
        Debug.Log("Starting DelayedSubmission process");

        // First, ensure we force any caching or buffered writes to complete
        if (logManager != null)
        {
            Debug.Log("Forcing log finalization...");

            // Call FinalizeLogFile directly to flush any pending writes
            logManager.FinalizeLogFile();

            // Allow some time for the file system to finish writing
            yield return new WaitForSeconds(1f);
        }
        else
        {
            Debug.LogError("LogManager is NULL!");
        }

        // Wait an additional moment to ensure file system operations complete
        Debug.Log("Waiting for file system operations to complete...");
        yield return new WaitForSeconds(2f);

        // Special handling: Check if there's a LogManager.Instance that might be different from our referenced logManager
        if (LogManager.Instance != null && LogManager.Instance != logManager)
        {
            Debug.Log("Using LogManager.Instance to finalize logs...");
            LogManager.Instance.FinalizeLogFile();
            yield return new WaitForSeconds(1f);
        }

        // Read the log data using our robust approach
        string csvContent = GetRobustLogData();

        // Debug output to verify data content
        Debug.Log($"CSV data to be submitted contains {csvContent.Split('\n').Length} rows and {csvContent.Length} characters");
        Debug.Log($"First 100 characters: {csvContent.Substring(0, Math.Min(100, csvContent.Length))}");

        // Create a local backup before attempting server upload
        string backupPath = SaveLocalBackup(csvContent);
        Debug.Log($"Local backup created at: {backupPath}");

        // Proceed with server submission
        StartCoroutine(SubmitDataToServer(csvContent));
    }

    // private string GetRobustLogData()
    // {
    //     string csvContent = "";
    //     Debug.Log("Attempting to get robust log data...");

    //     // First try the path you mentioned specifically
    //     string correctFilePath = Path.Combine(
    //         "/Users/m.li.14@bham.ac.uk/Documents/GitHub/EffortPathTask-2D/Assets/_ExpData",
    //         $"decision_task_log_{PlayerPrefs.GetString("ParticipantID", "Unknown")}_{DateTime.Now.ToString("yyyy-MM-dd")}*.csv");

    //     string[] matchingFiles = Directory.GetFiles(
    //         "/Users/m.li.14@bham.ac.uk/Documents/GitHub/EffortPathTask-2D/Assets/_ExpData",
    //         $"decision_task_log_{PlayerPrefs.GetString("ParticipantID", "Unknown")}*.csv");

    //     if (matchingFiles.Length > 0)
    //     {
    //         // Sort by last write time to get the most recent
    //         System.Array.Sort(matchingFiles, (a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

    //         try
    //         {
    //             // Use a more robust file reading approach
    //             using (FileStream fs = new FileStream(matchingFiles[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
    //             using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
    //             {
    //                 csvContent = reader.ReadToEnd();
    //             }
    //             Debug.Log($"Successfully read {csvContent.Split('\n').Length} lines from correct log file: {matchingFiles[0]}");
    //         }
    //         catch (Exception e)
    //         {
    //             Debug.LogError($"Error reading correct log file: {e.Message}");
    //         }


    //     }

    //     return csvContent;
    // }


    private string GetRobustLogData()
    {
        string csvContent = "";
        Debug.Log("Attempting to get robust log data...");

        // Get participant ID for filename matching
        string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");

        // Try multiple possible locations
        List<string> possiblePaths = new List<string>
    {
        // Primary location - persistent data path (works across platforms)
        Application.persistentDataPath,
        
        // Secondary location - streaming assets (read-only, but works in builds)
        Application.streamingAssetsPath,
        
        // Tertiary location - data path (might work in editor/standalone)
        Application.dataPath + "/_ExpData",
        
        // Last resort - current directory
        Directory.GetCurrentDirectory()
    };

        foreach (string basePath in possiblePaths)
        {
            if (!Directory.Exists(basePath))
                continue;

            Debug.Log($"Searching for log files in: {basePath}");

            try
            {
                // Look for any CSV files matching the participant ID pattern
                string[] matchingFiles = Directory.GetFiles(
                    basePath,
                    $"decision_task_log_{participantId}*.csv",
                    SearchOption.AllDirectories);

                if (matchingFiles.Length > 0)
                {
                    // Sort by last write time to get the most recent
                    System.Array.Sort(matchingFiles, (a, b) =>
                        File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));

                    try
                    {
                        // Use a file sharing approach that works even if file is open
                        using (FileStream fs = new FileStream(matchingFiles[0], FileMode.Open,
                            FileAccess.Read, FileShare.ReadWrite))
                        using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                        {
                            csvContent = reader.ReadToEnd();
                        }
                        Debug.Log($"Successfully read {csvContent.Split('\n').Length} lines from log file: {matchingFiles[0]}");

                        // If we successfully read data, no need to check other paths
                        break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error reading log file {matchingFiles[0]}: {e.Message}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error searching in directory {basePath}: {e.Message}");
            }
        }

        // If we still don't have content, try to get it directly from LogManager
        if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 1)
        {
            Debug.Log("Couldn't find valid log file, trying LogManager directly...");

            if (logManager != null)
            {
                try
                {
                    csvContent = logManager.GetCsvContent();
                    Debug.Log($"Got {csvContent.Split('\n').Length} lines from LogManager.GetCsvContent()");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error getting CSV content from LogManager: {e.Message}");
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
        // Disable the button to prevent multiple clicks
        if (continueButton != null)
        {
            continueButton.interactable = false;
        }

        // Play a nice sound when the continue button is clicked
        PlaySound(continueButtonSound);

        // Show a thank you message without playing a sound and make it persist
        ShowMessage("Thank you! Closing application...", false, false, true);

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