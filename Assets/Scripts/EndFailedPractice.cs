using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.IO;
using System.Text;
using System.Linq;

public class EndFailedPractice : MonoBehaviour
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
    // [SerializeField] private Button continueButton;
    private bool continueButtonClicked = false;
    [Header("Resubmission Settings")]
    [SerializeField] private Button resubmitButton;
    [SerializeField] private bool autoResubmitOnContinue = true;
    private bool submissionSuccess = false;

    [Header("Feedback Settings")]
    [SerializeField] private float messageDuration = 3f;
    [SerializeField] private Color successColor = new Color(0.2f, 0.8f, 0.2f, 1f);
    [SerializeField] private Color errorColor = new Color(0.8f, 0.2f, 0.2f, 1f);
    [SerializeField] private AudioClip successSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float scaleFactor = 1.2f;

    [Header("Server Settings")]
    [SerializeField] private bool offlineMode = false;

    private float totalTime;
    private int totalScore;
    private bool isSubmitting = false;
    private AudioSource audioSource;
    private Coroutine hideMessageCoroutine;
    private Vector3 originalFeedbackScale;

    private void Awake()
    {
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.PlayMusic();
        }
    }

    private void OnApplicationQuit()
    {
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.StopMusic();
        }

        if (!submissionSuccess)
        {
            string csvContent = GetRobustLogData();
            SaveLocalBackup(csvContent);
        }
    }

    private void Start()
    {
        if (totalTimeText == null) Debug.LogError("totalTimeText is not assigned in the inspector!");
        if (totalScoreText == null) Debug.LogError("totalScoreText is not assigned in the inspector!");
        if (submitButton == null) Debug.LogError("submitButton is not assigned in the inspector!");
        if (feedbackText == null) Debug.LogError("feedbackText is not assigned in the inspector!");
        if (feedbackBackground == null) Debug.LogError("feedbackBackground is not assigned in the inspector!");

        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
            originalFeedbackScale = feedbackText.transform.localScale;
            feedbackBackground.gameObject.SetActive(false);
        }

        if (feedbackInputField == null)
        {
            Debug.LogError("feedbackInputField is not assigned in the inspector!");
        }
        else
        {
            feedbackInputField.interactable = true;
            feedbackInputField.onEndEdit.RemoveAllListeners();
            feedbackInputField.onEndEdit.AddListener(OnFeedbackEndEdit);
        }

        if (submissionInstructionText != null)
        {
            submissionInstructionText.text = "Click the 'Submit' button or Press 'Enter' to submit";
        }

        // if (continueButton != null)
        // {
        //     continueButton.gameObject.SetActive(false);
        //     continueButton.onClick.AddListener(ContinueToEnd);
        // }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        float startTime = PlayerPrefs.GetFloat("ExperimentStartTime", Time.time);
        totalTime = Time.time - startTime;

        DisplayTotalTime();
        DisplayTotalScore();

        if (submitButton != null)
        {
            submitButton.onClick.AddListener(SubmitData);
        }

        if (resubmitButton != null)
        {
            resubmitButton.onClick.AddListener(ResubmitData);
            resubmitButton.gameObject.SetActive(false);
        }

        ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.AddElement(submitButton);
        // navigationController.AddElement(continueButton);
        navigationController.AddElement(resubmitButton);

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        if (logManager != null)
        {
            Dictionary<string, string> initialData = new Dictionary<string, string>
            {
                {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
                {"Scene", "EndFailedPractice"},
                {"TotalTime", totalTime.ToString("F2")},
                {"TotalScore", totalScore.ToString()}
            };
            logManager.LogEvent("EndFailedPracticeSceneLoaded", initialData);
        }

        // Show failure message immediately
        ShowMessage("Unfortunately, you did not pass the practice round and cannot proceed to the main experiment. \n" +
                   "Thank you for giving it a try. \n" +
                   "Please return your submission.", true, true, true);
    }

    private void Update()
    {
        bool canSubmit = feedbackInputField == null ||
                        (!feedbackInputField.isFocused && !string.IsNullOrEmpty(feedbackInputField.text));

        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) &&
            !isSubmitting && canSubmit)
        {
            SubmitData();
        }

        if (feedbackInputField != null && Input.GetMouseButtonDown(0))
        {
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
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            feedbackInputField.ActivateInputField();
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

        string[] lines = csvContent.Split('\n');
        Debug.Log($"Submitting CSV with {lines.Length} lines");

        if (lines.Length <= 2)
        {
            Debug.LogError("CSV data contains only headers or is incomplete!");
            ShowMessage("Error: Incomplete data", true);
            SaveLocalBackup(csvContent);
            isSubmitting = false;
            yield break;
        }

        string herokuUploadUrl = "https://effortpatch-0b3abd136749.herokuapp.com/upload";
        Debug.Log($"Uploading to URL: {herokuUploadUrl}");

        int maxRetries = 3;
        int retryCount = 0;
        bool success = false;

        while (!success && retryCount < maxRetries)
        {
            retryCount++;
            Debug.Log($"Upload attempt {retryCount}/{maxRetries}...");

            string normalizedContent = csvContent.Replace("\r\n", "\n").Replace('\r', '\n');
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(normalizedContent);

            UnityWebRequest request = new UnityWebRequest(herokuUploadUrl, "POST");
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "text/plain; charset=utf-8");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 60;

            Debug.Log($"Sending {bodyRaw.Length} bytes to server...");
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
                if (resubmitButton != null)
                {
                    resubmitButton.gameObject.SetActive(true);
                    resubmitButton.interactable = true;
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
            ShowMessage("Thank you for your participation! Feel free to QUIT now.", false, true, true);

            if (resubmitButton != null)
            {
                resubmitButton.gameObject.SetActive(false);
            }
        }
        else
        {
            ShowMessage("Server submission failed... Please retry manually by clicking the 'Resubmit Button' button!", true, true, true);

            if (resubmitButton != null)
            {
                resubmitButton.gameObject.SetActive(true);
                resubmitButton.interactable = true;
            }
            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(false);
                submitButton.interactable = false;
            }
        }

        if (!success)
        {
            ShowMessage("All retries failed. Click 'Resubmit' or save locally.", true);
            isSubmitting = false;
            if (resubmitButton != null)
                resubmitButton.gameObject.SetActive(true);
            resubmitButton.interactable = true;
        }
        else
        {
            if (resubmitButton != null)
                resubmitButton.gameObject.SetActive(false);
        }

        // if (continueButton != null)
        // {
        //     continueButton.gameObject.SetActive(true);
        // }

        isSubmitting = false;
    }

    public void ResubmitData()
    {
        if (isSubmitting) return;
        StartCoroutine(DelayedSubmission());
        ShowMessage("Resubmitting data...", false);
    }

    private string SaveLocalBackup(string csvContent)
    {
        try
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
            string filename = $"practice_failed_data_{participantId}_{timestamp}.csv";

            string path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllText(path, csvContent);

            Debug.Log($"Local backup saved to: {path}");

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
        PlaySound(buttonClickSound);
        EnsureLogCompleteness();

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

        PlayerPrefs.SetString("ParticipantFeedback", feedback);
        PlayerPrefs.Save();

        if (offlineMode)
        {
            ShowMessage("Thank you for your participation! (Offline Mode)", false);
            // if (continueButton != null)
            // {
            //     continueButton.gameObject.SetActive(true);
            // }
            isSubmitting = false;
            return;
        }

        StartCoroutine(DelayedSubmission());
    }

    private IEnumerator DelayedSubmission()
    {
        Debug.Log("Starting DelayedSubmission process");
        if (logManager != null)
        {
            try
            {
                logManager.FinalizeLogFile();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error finalizing logs: {e.Message}");
            }
        }

        Debug.Log("Waiting for log files to be finalized...");
        yield return new WaitForSeconds(3f);

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("EndFailedPracticeSubmission", new Dictionary<string, string> {
                {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
                {"TotalTime", totalTime.ToString("F2")},
                {"TotalScore", totalScore.ToString()},
                {"FeedbackProvided", (feedbackInputField != null && !string.IsNullOrEmpty(feedbackInputField.text)).ToString()}
            });
            yield return new WaitForSeconds(1f);
        }

        string csvContent = GetRobustLogData();
        int lineCount = csvContent.Split('\n').Length;
        Debug.Log($"CSV data to be submitted contains {lineCount} rows");

        if (lineCount <= 2)
        {
            Debug.LogError("WARNING: CSV data appears to contain only headers!");
            ShowMessage("Error: Could not read complete data", true);
            csvContent = CreateEmergencyCSV();
        }

        string backupPath = SaveLocalBackup(csvContent);
        Debug.Log($"Local backup created at: {backupPath}");
        StartCoroutine(SubmitDataToServer(csvContent));
    }

    private string CreateEmergencyCSV()
    {
        Debug.Log("Creating emergency minimal CSV data...");

        string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
        int participantAge = PlayerPrefs.GetInt("ParticipantAge", 0);
        string participantGender = PlayerPrefs.GetString("ParticipantGender", "Unknown");
        string feedback = feedbackInputField != null ? feedbackInputField.text : "No feedback provided";

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                     "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                     "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,ParticipantFeedback");

        sb.AppendLine($"{DateTime.Now},Summary,{participantId},{participantAge},{participantGender},,,,,,,,," +
                      $"{totalTime.ToString("F2")},,{totalScore},,,,,,,{feedback}");

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

        string participantId = PlayerPrefs.GetString("ParticipantID", "Unknown");
        string todayDate = DateTime.Now.ToString("yyyy-MM-dd");

        List<string> possiblePaths = new List<string>
        {
            Application.persistentDataPath,
            Application.dataPath + "/_ExpData",
            Path.Combine(Application.dataPath, "../_ExpData"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EffortPathBackups"),
            Directory.GetCurrentDirectory()
        };

        if (logManager != null && !string.IsNullOrEmpty(logManager.LogFilePath))
        {
            string logDirectory = Path.GetDirectoryName(logManager.LogFilePath);
            if (!string.IsNullOrEmpty(logDirectory))
                possiblePaths.Insert(0, logDirectory);
        }

        if (LogManager.Instance != null && LogManager.Instance != logManager &&
            !string.IsNullOrEmpty(LogManager.Instance.LogFilePath))
        {
            string instanceDirectory = Path.GetDirectoryName(LogManager.Instance.LogFilePath);
            if (!string.IsNullOrEmpty(instanceDirectory) && !possiblePaths.Contains(instanceDirectory))
                possiblePaths.Insert(0, instanceDirectory);
        }

        foreach (string basePath in possiblePaths)
        {
            if (!Directory.Exists(basePath))
            {
                Debug.Log($"Directory does not exist: {basePath}");
                continue;
            }

            try
            {
                string[] allCsvFiles = Directory.GetFiles(basePath, "*.csv", SearchOption.TopDirectoryOnly);
                List<string> matchingFiles = new List<string>();

                foreach (string file in allCsvFiles)
                {
                    string filename = Path.GetFileName(file);
                    if (filename.Contains(participantId) && (filename.Contains(todayDate) || filename.Contains("decision_task_log")))
                    {
                        matchingFiles.Add(file);
                    }
                }

                if (matchingFiles.Count == 0)
                {
                    foreach (string file in allCsvFiles)
                    {
                        if (Path.GetFileName(file).Contains(participantId))
                        {
                            matchingFiles.Add(file);
                        }
                    }
                }

                if (matchingFiles.Count == 0)
                {
                    foreach (string file in allCsvFiles)
                    {
                        if (File.GetLastWriteTime(file).Date == DateTime.Now.Date)
                        {
                            matchingFiles.Add(file);
                        }
                    }
                }

                if (matchingFiles.Count > 0)
                {
                    matchingFiles.Sort((a, b) => File.GetLastWriteTime(b).CompareTo(File.GetLastWriteTime(a)));
                    foreach (string file in matchingFiles)
                    {
                        try
                        {
                            string fileContent;
                            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (StreamReader reader = new StreamReader(fs, Encoding.UTF8))
                            {
                                fileContent = reader.ReadToEnd();
                            }

                            if (!string.IsNullOrEmpty(fileContent) && fileContent.Split('\n').Length > 2)
                            {
                                csvContent = fileContent;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError($"Error reading log file {file}: {e.Message}");
                        }
                    }

                    if (!string.IsNullOrEmpty(csvContent) && csvContent.Split('\n').Length > 2)
                    {
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error searching in directory {basePath}: {e.Message}");
            }
        }

        if (string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 2)
        {
            Debug.Log("Couldn't find valid log file, trying LogManager directly...");
            if (LogManager.Instance != null)
            {
                try
                {
                    csvContent = LogManager.Instance.GetCsvContent();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error getting CSV content from LogManager.Instance: {e.Message}");
                }
            }

            if ((string.IsNullOrEmpty(csvContent) || csvContent.Split('\n').Length <= 2) &&
                logManager != null && logManager != LogManager.Instance)
            {
                try
                {
                    csvContent = logManager.GetCsvContent();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error getting CSV content from serialized logManager: {e.Message}");
                }
            }
        }

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

    private string AddFeedbackToCSV(string csvContent, string feedback)
    {
        string[] lines = csvContent.Split('\n');
        bool hasParticipantFeedback = lines[0].Contains("ParticipantFeedback");

        if (!hasParticipantFeedback)
        {
            lines[0] += ",ParticipantFeedback";
        }

        StringBuilder result = new StringBuilder();
        result.AppendLine(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            if (i == lines.Length - 1 || string.IsNullOrWhiteSpace(lines[i + 1]))
            {
                if (!hasParticipantFeedback)
                {
                    lines[i] += "," + feedback;
                }
                else
                {
                    string[] fields = lines[i].Split(',');
                    int feedbackIndex = Array.IndexOf(lines[0].Split(','), "ParticipantFeedback");

                    if (feedbackIndex >= fields.Length)
                    {
                        lines[i] += "," + feedback;
                    }
                    else
                    {
                        fields[feedbackIndex] = feedback;
                        lines[i] = string.Join(",", fields);
                    }
                }
            }

            result.AppendLine(lines[i]);
        }

        return result.ToString();
    }

    public void ContinueToEnd()
    {
        if (autoResubmitOnContinue && !submissionSuccess && PlayerPrefs.GetInt("DataSubmitted", 0) != 1)
        {
            ResubmitData();
            ShowMessage("Submitting data...", false);
            return;
        }

        if (!submissionSuccess && PlayerPrefs.GetInt("DataSubmitted", 0) != 1)
        {
            ShowMessage("Please submit data first!", true);
            return;
        }

        continueButtonClicked = true;
        PlaySound(buttonClickSound);

        // if (continueButton != null)
        // {
        //     continueButton.interactable = false;
        // }

        ShowMessage("Thank you! Closing now...", false, false, true);

        StartCoroutine(QuitApplication());
    }

    private IEnumerator QuitApplication()
    {
        yield return new WaitForSeconds(0.3f);
        Debug.Log("Quitting application...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ShowMessage(string message, bool isError = false, bool playSound = true, bool persist = false)
    {
        if (feedbackText == null) return;

        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
            hideMessageCoroutine = null;
        }

        feedbackText.text = message;
        feedbackText.color = isError ? errorColor : successColor;

        if (feedbackBackground != null)
        {
            feedbackBackground.gameObject.SetActive(true);
            feedbackBackground.color = isError ?
                new Color(errorColor.r, errorColor.g, errorColor.b, 0.2f) :
                new Color(successColor.r, successColor.g, successColor.b, 0.2f);
        }

        if (playSound)
        {
            PlaySound(isError ? errorSound : successSound);
        }

        feedbackText.transform.localScale = originalFeedbackScale;
        feedbackText.gameObject.SetActive(true);
        StartCoroutine(AnimateFeedback(isError));

        if (!persist)
        {
            hideMessageCoroutine = StartCoroutine(HideMessageAfterDelay());
        }
    }

    private IEnumerator AnimateFeedback(bool isError)
    {
        float elapsedTime = 0f;
        Vector3 targetScale = originalFeedbackScale * scaleFactor;

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

        elapsedTime = 0f;
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
        if (string.IsNullOrWhiteSpace(feedback))
        {
            Debug.Log("No feedback provided");
            return;
        }

        string sanitizedFeedback = System.Text.Encoding.UTF8.GetString(
            System.Text.Encoding.UTF8.GetBytes(feedback)
        );

        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("ParticipantFeedback", new Dictionary<string, string>
            {
                {"ParticipantID", PlayerPrefs.GetString("ParticipantID", "Unknown")},
                {"ParticipantFeedback", sanitizedFeedback},
                {"FeedbackLanguage", DetectLanguage(sanitizedFeedback)},
                {"AdditionalInfo", "Feedback collected on EndFailedPractice scene"}
            });
        }
        else
        {
            PlayerPrefs.SetString("ParticipantFeedback", sanitizedFeedback);
            PlayerPrefs.Save();
        }
    }

    private string DetectLanguage(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "Unknown";

        bool hasChinese = text.Any(c =>
            (c >= 0x4E00 && c <= 0x9FFF) ||
            (c >= 0x3400 && c <= 0x4DBF) ||
            (c >= 0x20000 && c <= 0x2FFFF)
        );

        bool hasEnglish = text.Any(c =>
            (c >= 'A' && c <= 'Z') ||
            (c >= 'a' && c <= 'z')
        );

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