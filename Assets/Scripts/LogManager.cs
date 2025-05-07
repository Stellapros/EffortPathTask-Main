using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;

public class LogManager : MonoBehaviour
{
    /// <summary>
    /// Singleton class for managing experiment logging.
    /// </summary>

    #region Singleton
    public static LogManager Instance { get; private set; }
    #endregion

    #region Private Fields
    private const string LOG_DIRECTORY = "ExperimentLogs";
    private string logFilePath;
    private StringBuilder logBuilder;
    private List<string> unloggedTrials = new List<string>();
    private readonly object logLock = new object();
    [SerializeField] private bool m_ShowDebugLogManager;
    private OneDriveUploader oneDriveUploader;

    // Participant info
    private string participantID;
    private int participantAge;
    private string participantGender;
    // private ParticipantInfo participantInfo;
    #endregion

    #region Additional Fields for Behavioral Analysis
    private const float BLOCK_DURATION = 300f; // 2 minutes in seconds
    private List<Vector2> rewardPositions = new List<Vector2>();
    private Dictionary<int, List<float>> reactionTimesByBlock = new Dictionary<int, List<float>>();
    private Dictionary<int, List<float>> movementTimesByEffortLevel = new Dictionary<int, List<float>>();
    private Dictionary<int, int> skipCountByEffortLevel = new Dictionary<int, int>();
    private Dictionary<int, int> successCountByEffortLevel = new Dictionary<int, int>();
    #endregion

    #region Timing Fields
    private Dictionary<int, float> decisionStartTimes = new Dictionary<int, float>();
    private Dictionary<int, float> movementStartTimes = new Dictionary<int, float>();
    private Dictionary<int, string> decisionTypes = new Dictionary<int, string>();
    private Dictionary<int, TrialState> trialStates = new Dictionary<int, TrialState>();
    #endregion

    private class TrialState
    {
        public float decisionStartTime;
        public float movementStartTime;
        public string decisionType;
        public float decisionTime;
        public float movementTime;
        public bool rewardCollected;
        public int buttonPresses;
        public int effortLevel;
        public int requiredPresses;
        public bool isComplete;
        public string outcomeType; // Added to track outcome explicitly
    }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Add this debug to verify PlayerPrefs
            Debug.Log($"PlayerPrefs check - ID exists: {PlayerPrefs.HasKey("ParticipantID")}, " +
                    $"Age exists: {PlayerPrefs.HasKey("ParticipantAge")}, " +
                    $"Gender exists: {PlayerPrefs.HasKey("ParticipantGender")}");

            // Force reload participant info
            LoadParticipantInfo();
            InitializeLogging();
            oneDriveUploader = gameObject.AddComponent<OneDriveUploader>();
            logBuilder = new StringBuilder();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Initialization
    public void SetParticipantInfoDirectly(string id, int age, string gender)
    {
        participantID = id;
        participantAge = age;
        participantGender = gender;

        // Optional: Also save to PlayerPrefs for redundancy
        PlayerPrefs.SetString("ParticipantID", id);
        PlayerPrefs.SetInt("ParticipantAge", age);
        PlayerPrefs.SetString("ParticipantGender", gender);
        PlayerPrefs.Save();

        Debug.Log($"LogManager received direct info: {id}, {age}, {gender}");
    }

    private void LoadParticipantInfo()
    {
        participantID = PlayerPrefs.GetString("ParticipantID", "Unknown");
        participantAge = PlayerPrefs.GetInt("ParticipantAge", 0);
        participantGender = PlayerPrefs.GetString("ParticipantGender", "Unknown");

        Debug.Log($"Loaded participant info: ID={participantID}, Age={participantAge}, Gender={participantGender}");
    }
    public string LogFilePath => logFilePath;

    private bool isInitialized = false; // Add this flag

    private void InitializeLogging()
    {
        if (isInitialized || !string.IsNullOrEmpty(logFilePath))
        {
            Debug.Log($"Logging already initialized with path: {logFilePath}");
            return; // Prevent multiple initializations
        }

        // Create a single log file with timestamp
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string strDir = Path.Combine(Application.dataPath, "_ExpData");
        Directory.CreateDirectory(strDir);
        logFilePath = Path.Combine(strDir, $"decision_task_log_{participantID}_{timestamp}.csv");

        Debug.Log($"Initializing log file path: {logFilePath}");

        // Create header for the log file
        string header = GenerateCSVHeader();

        try
        {
            File.WriteAllText(logFilePath, header);
            if (m_ShowDebugLogManager) Debug.Log($"Initialized logging to: {logFilePath}");
            isInitialized = true; // Mark logging as initialized
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize logging: {e.Message}");
        }
    }

    private string GenerateCSVHeader()
    {
        return "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender," +
               // Block and Trial Info
               "IsPractice,BlockNumber,BlockType,TrialNumber," +
               // Trial Parameters
               "EffortLevel,RequiredPresses,DecisionType,ReactionTime,OutcomeType," +
               // Performance Metrics
               "RewardCollected,Points," +
               // Scores
               "TotalScore,PracticeScore," +
               // Movement Step Details 
               "MovementDuration,StepStartTime,StepEndTime,StepDuration,StepSuccessful,StepPressesRequired," +
               "StartX,StartY,EndX,EndY," +
               // Effort Info
               "TotalPresses,PressData,TimePerPress," +
               "IsRushing,RushingThreshold," +
               // Penalty Info
               "PenaltyType,PenaltyDuration," +
               // Block Status
               "RemainingTime,ElapsedTime," +
               "CompletedTrials,TrialRate," +
               // Calibration Data 
               "CalibrationEasy,CalibrationMedium,CalibrationHard," +
               "CalibrationPhase1Rate,CalibrationPhase2Rate,CalibrationPhase3Rate,CalibrationMaxRate," +
               // Check Questions
               "CheckPhase,QuestionNumber,QuestionType,QuestionText,SelectedAnswer,CorrectAnswer,IsCorrect,ResponseTime," +
               // Check Scores
               "Check1Score,Check2Score,ComprehensionScore,TotalCheckScore,FailedAttempts," +
               // Participant Feedback
               "TirednessRating,ParticipantFeedback," +
               // Navigation
               "ContinueButtonClicked,ButtonAction,RedirectURL,\n";
    }
    #endregion

    #region Logging Methods
    private int AdjustToOneBasedIndex(int index, bool isPractice = false)
    {
        return isPractice ? 0 : index + 1;
    }

    public void LogInfoMessage(string messageType, string details)
    {
        LogEvent("InfoMessage", new Dictionary<string, string>
        {
            {"MessageType", messageType},
            {"Details", details}
        });
    }

    public void LogExperimentSetup(List<int> blockOrder, int totalBlocks, int trialsPerBlock)
    {
        // Create a dictionary to store block information
        var blockInfo = new Dictionary<string, string>();

        // Add basic experiment configuration
        blockInfo["TotalBlocks"] = totalBlocks.ToString();
        blockInfo["TrialsPerBlock"] = trialsPerBlock.ToString();
        blockInfo["TotalTrials"] = (totalBlocks * trialsPerBlock).ToString();

        // Format block order (convert to 1-based indexing for logging)
        string formattedBlockOrder = string.Join(",",
            blockOrder.Select(b => (b + 1).ToString()));
        blockInfo["BlockOrder"] = formattedBlockOrder;

        // Calculate some basic statistics about the block order
        var blockStats = blockOrder
            .GroupBy(b => b)
            .OrderBy(g => g.Key)
            .Select(g => $"Block{g.Key + 1}={g.Count()}");
        blockInfo["BlockDistribution"] = string.Join(";", blockStats);

        // Get experiment start time
        blockInfo["SetupTime"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Add validation information
        blockInfo["IsValidConfiguration"] = (blockOrder.Count == totalBlocks).ToString();
        blockInfo["ConfigurationHash"] = CalculateConfigHash(blockOrder, totalBlocks, trialsPerBlock);

        // Log the experiment setup event
        LogEvent("ExperimentSetup", blockInfo);

        if (m_ShowDebugLogManager)
        {
            Debug.Log($"Logged experiment setup with {totalBlocks} blocks and {trialsPerBlock} trials per block");
            Debug.Log($"Block order: {formattedBlockOrder}");
        }
    }

    private string CalculateConfigHash(List<int> blockOrder, int totalBlocks, int trialsPerBlock)
    {
        // Create a string that represents the core configuration
        string configString = $"{string.Join("", blockOrder)}-{totalBlocks}-{trialsPerBlock}";

        // Calculate a simple hash of the configuration
        int hash = 17;
        foreach (char c in configString)
        {
            hash = hash * 31 + c;
        }

        return Math.Abs(hash).ToString("X8"); // Return as 8-character hex string
    }

    public void LogExperimentStart(bool isPractice)
    {
        LogEvent("ExperimentStart", new Dictionary<string, string>
        {
            {"IsPractice", isPractice.ToString()},
            {"StartTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
        });
    }

    public void LogBlockStart(int blockNumber)
    {
        LogEvent("BlockStart", new Dictionary<string, string>
    {
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
        {"BlockType", GetBlockTypeString(blockNumber)}
    });
    }

    public void LogTrialStart(int trialIndex, int blockNumber, float startTime)
    {
        LogEvent("TrialStart", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialIndex).ToString()},
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
        {"StartTime", startTime.ToString("F3")},
        {"DateTime", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    // public void LogTrialRemainingTime(int trialNumber, int blockNumber, float remainingTime)
    // {
    //     LogEvent("TrialRemainingTime", new Dictionary<string, string>
    // {
    //     {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
    //     {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
    //     {"RemainingTime", remainingTime.ToString("F2")},
    //     {"ElapsedTime", (BLOCK_DURATION - remainingTime).ToString("F2")}
    // });
    // }

    public void LogBlockTimeStatus(int blockNumber, int completedTrials)
    {
        // Get synchronized time values from ExperimentManager
        var (remainingTime, elapsedTime) = ExperimentManager.Instance.GetBlockTime();
        float trialsPerMinute = elapsedTime > 0 ? (completedTrials / elapsedTime * 60f) : 0f;

        LogEvent("BlockTimeStatus", new Dictionary<string, string>
    {
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
        {"RemainingTime", remainingTime.ToString("F2")},
        {"ElapsedTime", elapsedTime.ToString("F2")},
        {"CompletedTrials", completedTrials.ToString()},
        {"TrialRate", trialsPerMinute.ToString("F2")},
        {"TimeSyncHash", $"{remainingTime.GetHashCode()}:{elapsedTime.GetHashCode()}"} // For debugging
    });

        Debug.Log($"BlockTimeStatus | Block: {blockNumber} | " +
                 $"Remaining: {remainingTime:F2} | Elapsed: {elapsedTime:F2} | " +
                 $"Trials: {completedTrials} | Rate: {trialsPerMinute:F2}/min");
    }

    // public void LogBlockTimeElapsed(int trialNumber, int blockNumber, float remainingTime, float elapsedTime)
    // {

    //     LogEvent("BlockTimeElapsed", new Dictionary<string, string>
    // {
    //     {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
    //     {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
    //     {"RemainingTime", Mathf.Clamp(remainingTime, 0, BLOCK_DURATION).ToString("F2")},
    //     {"ElapsedTime", Mathf.Clamp(elapsedTime, 0, BLOCK_DURATION).ToString("F2")}
    // });
    // }

    private string GetBlockTypeString(int blockNumber)
    {
        if (ExperimentManager.Instance == null ||
            blockNumber < 0 ||
            blockNumber >= ExperimentManager.Instance.randomizedBlockOrder.Length)
        {
            return "Unknown";
        }

        return ExperimentManager.Instance.randomizedBlockOrder[blockNumber].ToString();
    }


    // public void LogBlockEnd(int blockNumber)
    // {
    //     LogEvent("BlockEnd", new Dictionary<string, string>
    //     {
    //         {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
    //         {"EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
    //     });
    // }

    // New overload (3 parameters)
    public void LogBlockEnd(int blockNumber, int totalTrials, int rushedTrials)
    {
        LogEvent("BlockEnd", new Dictionary<string, string>
    {
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
        {"TotalTrials", totalTrials.ToString()},
        {"RushedTrials", rushedTrials.ToString()},
        {"RushingPercentage", totalTrials > 0 ?
            $"{(float)rushedTrials/totalTrials * 100:F1}%" : "0%"},
        {"BlockType", GetBlockTypeString(blockNumber)},
        {"EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
    });
    }
    #endregion

    #region Check Question Logging
    public void LogCheckQuestionResponse(
        int trialNumber,
        int checkPhase,
        int questionNumber,
        string questionType,  // e.g., "FruitFrequency", "ComprehensionQuiz", "PairComparison"
        string questionText,
        string selectedAnswer,
        string correctAnswer,
        bool isCorrect,
        float responseTime,
        Dictionary<string, string> additionalData = null)
    {
        var logData = new Dictionary<string, string>
        {
            {"TrialNumber", (trialNumber + 1).ToString()}, // Convert to 1-based here
            {"CheckPhase", checkPhase.ToString()},
            {"QuestionNumber", (questionNumber + 1).ToString()}, // Convert to 1-based here
            {"QuestionType", questionType},
            {"QuestionText", questionText},
            {"SelectedAnswer", selectedAnswer},
            {"CorrectAnswer", correctAnswer},
            {"IsCorrect", isCorrect.ToString()},
            {"ResponseTime", responseTime.ToString("F3")},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        };

        // Add any additional data if provided
        if (additionalData != null)
        {
            foreach (var item in additionalData)
            {
                logData[$"Additional_{item.Key}"] = item.Value;
            }
        }
        Debug.Log($"LogCheckQuestionResponse called - Trial: {trialNumber}, Q: {questionNumber}");
        LogEvent("CHECK_QUESTION", logData);
    }

    public void LogCheckPhaseComplete(
        int checkPhase,
        int totalQuestions,
        int correctAnswers,
        float completionTime,
        string phaseType,
        Dictionary<string, string> phaseSpecificData = null)
    {
        var logData = new Dictionary<string, string>
    {
        {"CheckPhase", checkPhase.ToString()},
        {"PhaseType", phaseType},
        {"TotalQuestions", totalQuestions.ToString()},
        {"CorrectAnswers", correctAnswers.ToString()},
        {"Score", ((float)correctAnswers / totalQuestions * 100).ToString("F1")},
        {"CompletionTime", completionTime.ToString("F3")},
        {"AverageTimePerQuestion", (completionTime / totalQuestions).ToString("F3")},
        {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    };

        // Always include all check scores
        logData["Check1Score"] = PlayerPrefs.GetInt("Check1Score", 0).ToString();
        logData["Check2Score"] = PlayerPrefs.GetInt("Check2Score", 0).ToString();
        logData["ComprehensionScore"] = correctAnswers.ToString();
        logData["TotalCheckScore"] = (PlayerPrefs.GetInt("Check1Score", 0) +
                                    PlayerPrefs.GetInt("Check2Score", 0) +
                                    correctAnswers).ToString();

        LogEvent("CHECK_PHASE_COMPLETE", logData);
    }

    #endregion

    #region Decision Phase Logging
    public void LogWorkDecision(int trialNumber, float decisionTime = -1)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        if (decisionTime < 0)
        {
            decisionTime = Time.time - state.decisionStartTime;
        }

        state.decisionType = "Work";
        state.decisionTime = decisionTime;

        LogEvent("Decision", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"BlockNumber", ExperimentManager.Instance.GetCurrentBlockNumber().ToString()},  // Added BlockNumber
        {"DecisionType", "Work"},
        {"DecisionTime", decisionTime.ToString("F3")},
        {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    public void LogSkipDecision(int trialNumber, float decisionTime = -1)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        if (decisionTime < 0)
        {
            decisionTime = Time.time - state.decisionStartTime;
        }

        state.decisionType = "Skip";
        state.decisionTime = decisionTime;

        LogEvent("Decision", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"BlockNumber", ExperimentManager.Instance.GetCurrentBlockNumber().ToString()},  // Added BlockNumber
        {"DecisionType", "Skip"},
        {"DecisionTime", decisionTime.ToString("F3")},
        {"PenaltyDuration", "3.0"},
        {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    public void LogDecisionOutcome(
            int trialNumber,
            int blockNumber,
            string decisionType,
            bool rewardCollected,
            float reactionTime,
            float movementTime, // This is TOTAL movement time
            int buttonPresses,
            int effortLevel,
            int requiredPresses,
            bool skipAdjustment = false,
            string pressData = "-",
            float timePerPress = -1f,
            int points = 10,
            int loggedTotalScore = 0,
            int loggedPracticeScore = 0,
            string practiceType = "") // New parameter for specific practice type
    {
        // Get synchronized time values from ExperimentManager
        var (remainingTime, elapsedTime) = ExperimentManager.Instance.GetBlockTime();

        // Calculate metrics
        string outcomeType = DetermineOutcomeType(decisionType, rewardCollected);
        int requiredTotalPresses = requiredPresses * 5;
        bool isPracticeTrial = blockNumber == -1;

        if (timePerPress < 0 && buttonPresses > 0 && movementTime > 0)
        {
            timePerPress = movementTime / buttonPresses;
        }
        if (buttonPresses <= 0)
        {
            timePerPress = -1f;
        }

        var (calculatedTimePerPress, isRushing) = CalculateRushingMetrics(movementTime, requiredPresses);

        // Get scores
        int totalScore = loggedTotalScore;
        int practiceScore = isPracticeTrial ? loggedPracticeScore : (ScoreManager.Instance?.GetPracticeScore() ?? 0);

        // Determine the specific BlockType for practice trials
        string blockTypeString;
        if (isPracticeTrial)
        {
            // If practiceType is provided, use it for more specific practice BlockType
            if (!string.IsNullOrEmpty(practiceType))
            {
                blockTypeString = $"Practice_{practiceType}";
            }
            else
            {
                // Default to generic "Practice" if no specific type is provided
                blockTypeString = "Practice";
            }
        }
        else
        {
            // For non-practice trials, use the existing GetBlockTypeString method
            blockTypeString = GetBlockTypeString(blockNumber);
        }

        LogEvent("DecisionOutcome", new Dictionary<string, string>
        {
            // Core trial info
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"BlockType", blockTypeString},
            {"IsPractice", isPracticeTrial.ToString()},
            
            // Decision data
            {"DecisionType", decisionType},
            {"ReactionTime", reactionTime.ToString("F3")},
            {"OutcomeType", outcomeType},
            {"RewardCollected", rewardCollected.ToString()},
            
            // Performance metrics
            {"MovementDuration", movementTime.ToString("F3")},
            {"ButtonPresses", buttonPresses.ToString()},
            {"EffortLevel", effortLevel.ToString()},
            {"RequiredPresses", requiredPresses.ToString()},
            {"RequiredTotalPresses", requiredTotalPresses.ToString()},
            {"TimePerPress", timePerPress.ToString("F3")},
            {"IsRushing", isRushing.ToString()},
            {"RushingThreshold", "0.1"},
            {"Notes", "Rushing=True indicates implausibly fast inputs (>10 presses/sec)"},
            {"PressData", pressData},
            {"Points", points.ToString()},

            {"TotalPresses", buttonPresses.ToString()},
            // {"StepDuration", movementTime.ToString("F3")}, 
            {"StartX", "-"}, // These position fields would need to be passed in or retrieved
            {"StartY", "-"}, // from PlayerController if you want actual values
            {"EndX", "-"},
            {"EndY", "-"},
            {"PenaltyType", decisionType == "Skip" ? "Skip" : "-"},
            {"PenaltyDuration", decisionType == "Skip" ? DecisionManager.GetSkipDelay().ToString() : "-"},

            // Synchronized time values
            {"RemainingTime", remainingTime.ToString("F2")},
            {"ElapsedTime", elapsedTime.ToString("F2")},
            
            // Scores
            {"TotalScore", totalScore.ToString()},
            {"PracticeScore", practiceScore.ToString()},
            
            
            // Block progress
            {"CompletedTrials", ExperimentManager.Instance.trialsCompletedInCurrentBlock.ToString()},
            {"TrialRate", (elapsedTime > 0 ?
                (ExperimentManager.Instance.trialsCompletedInCurrentBlock / elapsedTime * 60f).ToString("F2") : "0")},
            
            // Debug info
            {"TimeSyncHash", $"{remainingTime.GetHashCode()}:{elapsedTime.GetHashCode()}"}
        });


        Debug.Log($"DecisionOutcome | Trial: {trialNumber} | " +
                 $"Remaining: {remainingTime:F2} | Elapsed: {elapsedTime:F2} | " +
                 $"Decision: {decisionType} | Outcome: {outcomeType}");
    }
    #endregion


    #region Score Logging
    public void LogScoreUpdate(int trialNumber, bool isPractice, int points, string outcome)
    {
        // Get the most accurate scores possible
        int totalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalScore() : 0;
        int practiceScore = isPractice && PracticeScoreManager.Instance != null ?
                           PracticeScoreManager.Instance.GetCurrentScore() :
                           (ScoreManager.Instance != null ? ScoreManager.Instance.GetPracticeScore() : 0);

        // For practice, always use block 0
        int blockNumber = 0;

        // For formal trials, get the block from ExperimentManager
        if (!isPractice && ExperimentManager.Instance != null)
        {
            blockNumber = ExperimentManager.Instance.GetCurrentBlockNumber();
            Debug.Log($"Formal trial block number: {blockNumber}");
        }

        LogScoreUpdateComplete(trialNumber, isPractice, points, outcome, totalScore, practiceScore, blockNumber);
    }

    public void LogScoreUpdateComplete(int trialNumber, bool isPractice, int points, string outcome,
                                      int totalScore, int practiceScore, int blockNumber)
    {
        // Ensure block number is 0 for practice trials
        if (isPractice)
        {
            blockNumber = 0;
        }

        Debug.Log($"Logging score update - Trial: {trialNumber}, Block: {blockNumber}, IsPractice: {isPractice}");

        LogEvent("ScoreUpdate", new Dictionary<string, string>
    {
        {"TrialNumber", trialNumber.ToString()},
        {"IsPractice", isPractice.ToString()},
        {"Points", points.ToString()},
        {"Outcome", outcome},
        {"TotalScore", totalScore.ToString()},
        {"PracticeScore", practiceScore.ToString()},
        {"BlockNumber", blockNumber.ToString()}
    });
    }

    public void LogScoreReset(int trialNumber, bool isPractice, int oldScore, int newScore, string reason)
    {
        // Get the current state of both scores for complete logging
        int totalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalScore() : 0;
        int practiceScore = isPractice && PracticeScoreManager.Instance != null ?
                            PracticeScoreManager.Instance.GetCurrentScore() :
                            (ScoreManager.Instance != null ? ScoreManager.Instance.GetPracticeScore() : 0);

        // For practice, always use block 0
        int blockNumber = 0;

        // For formal trials, get the block from ExperimentManager
        if (!isPractice && ExperimentManager.Instance != null)
        {
            blockNumber = ExperimentManager.Instance.GetCurrentBlockNumber();
        }

        LogEvent("ScoreReset", new Dictionary<string, string>
    {
        {"TrialNumber", trialNumber.ToString()},
        {"IsPractice", isPractice.ToString()},
        {"OldScore", oldScore.ToString()},
        {"NewScore", newScore.ToString()},
        {"ResetReason", reason},
        {"CurrentTotalScore", totalScore.ToString()},
        {"CurrentPracticeScore", practiceScore.ToString()},
        // {"BlockNumber", blockNumber.ToString()}
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
    });
    }
    #endregion


    public void LogPenaltyStart(int trialNumber, string penaltyType, float duration)
    {
        LogEvent("PenaltyStart", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"PenaltyType", penaltyType}, // Log the penalty type (e.g., "NoDecision")
        {"PenaltyDuration", duration.ToString()} // Log the duration of the penalty (e.g., "5")
    });
    }

    public void LogTrialOutcome(int trialNumber, int blockNumber, float remainingTime = -1f)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        if (state.isComplete) return;  // Prevent duplicate logging

        state.isComplete = true;

        // Determine outcome type based on the complete trial state
        string outcomeType = state.outcomeType ?? DetermineOutcomeType(state.decisionType, state.rewardCollected);

        // Calculate elapsed time if remaining time is provided
        float elapsedTime = remainingTime >= 0 ? (BLOCK_DURATION - remainingTime) : -1f;

        var logData = new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
        {"BlockType", GetBlockTypeString(blockNumber)},  // Added BlockType
        {"DecisionType", state.decisionType},
        {"DecisionTime", state.decisionTime.ToString("F3")},
        {"RewardCollected", state.rewardCollected.ToString().ToLower()},
        {"MovementTime", state.movementTime.ToString("F3")},
        {"ButtonPresses", state.buttonPresses.ToString()},
        {"EffortLevel", state.effortLevel.ToString()},
        {"RequiredPresses", state.requiredPresses.ToString()},
        {"OutcomeType", outcomeType}
    };

        // Add remaining time if provided
        if (remainingTime >= 0)
        {
            logData["RemainingTime"] = remainingTime.ToString("F2");
            logData["ElapsedTime"] = elapsedTime.ToString("F2");
        }

        LogEvent("TrialOutcome", logData);

        // Cleanup trial state after logging
        trialStates.Remove(trialNumber);
    }



    // public void LogMovementStep(
    //     int trialNumber,
    //     Vector2 startPos,
    //     Vector2 endPos,
    //     float stepDuration,
    //     bool successful,
    //     float stepStartTime,
    //     float stepEndTime,
    //     int pressesRequired,
    //     int blockNumber)
    // {
    //     // Determine if this is a practice trial
    //     bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;
    //     // string blockType = isPracticeTrial ? "Practice" : "Formal";

    //     // Get effort level based on trial type
    //     int effortLevel = isPracticeTrial
    //         ? PracticeManager.Instance.GetCurrentTrialEffortLevel()
    //         : ExperimentManager.Instance.GetCurrentTrialEffortLevel();

    //     // Calculate rushing metrics
    //     var (timePerPress, isRushing) = CalculateRushingMetrics(stepDuration, pressesRequired);

    //     // Get the overall trial outcome (whether reward was collected)
    //     // This should align with DecisionOutcome
    //     string outcomeType = PlayerPrefs.GetString("CurrentTrialOutcome", "Pending");

    //     LogEvent("MovementStep", new Dictionary<string, string>
    //     {
    //         {"TrialNumber", trialNumber.ToString()},
    //         {"BlockNumber", blockNumber.ToString()},
    //        {"BlockType", isPracticeTrial ? "Practice" : GetBlockTypeString(blockNumber)},
    //         {"EffortLevel", effortLevel.ToString()},
    //         {"RequiredPresses", pressesRequired.ToString()},
    //         {"StartX", startPos.x.ToString("F2")},
    //         {"StartY", startPos.y.ToString("F2")},
    //         {"EndX", endPos.x.ToString("F2")},
    //         {"EndY", endPos.y.ToString("F2")},
    //         {"StepDuration", stepDuration.ToString("F3")},
    //         {"StepSuccessful", successful.ToString()},
    //         {"OutcomeType", outcomeType},
    //         {"StepStartTime", stepStartTime.ToString("F3")},
    //         {"StepEndTime", stepEndTime.ToString("F3")},
    //         {"StepPressesRequired", pressesRequired.ToString()},
    //         {"TimePerPress", timePerPress.ToString("F3")},
    //         {"IsRushing", isRushing.ToString()},
    //         {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    //     });
    // }



    public void LogMovementStep(
        int trialNumber,
        Vector2 startPos,
        Vector2 endPos,
        float stepDuration,
        bool successful,
        float stepStartTime,
        float stepEndTime,
        int pressesRequired,
        int blockNumber,
        string blockType) // Now accepts specific blockType parameter
    {
        // Determine if this is a practice trial
        bool isPracticeTrial = PlayerPrefs.GetInt("IsPracticeTrial", 0) == 1;

        // Get effort level based on trial type
        int effortLevel = isPracticeTrial
            ? PracticeManager.Instance.GetCurrentTrialEffortLevel()
            : ExperimentManager.Instance.GetCurrentTrialEffortLevel();

        // Calculate rushing metrics
        var (timePerPress, isRushing) = CalculateRushingMetrics(stepDuration, pressesRequired);

        // Get the overall trial outcome (whether reward was collected)
        string outcomeType = PlayerPrefs.GetString("CurrentTrialOutcome", "Pending");

        // Important: Make sure you're using the 0-based blockNumber for GetBlockTypeString
        // If blockNumber was already made 1-based for the log, we need to convert it back
        int zeroBasedBlockNumber = blockNumber - 1; // Only do this if blockNumber was 1-based

        // Use the provided blockType for practice trials instead of generic "Practice"
        string blockTypeString;
        if (isPracticeTrial)
        {
            // Use the specific block type passed in (which should be "Practice_X")
            blockTypeString = blockType;
            Debug.Log($"Using specific practice block type in log: {blockTypeString}");
        }
        else
        {
            // For non-practice trials, get the standard block type
            blockTypeString = GetBlockTypeString(zeroBasedBlockNumber);
        }

        LogEvent("MovementStep", new Dictionary<string, string>
        {
            {"TrialNumber", trialNumber.ToString()},
            {"BlockNumber", blockNumber.ToString()}, // Keep this as is (likely 1-based for UI)
            {"BlockType", blockTypeString},
            {"EffortLevel", effortLevel.ToString()},
            {"RequiredPresses", pressesRequired.ToString()},
            {"StartX", startPos.x.ToString("F2")},
            {"StartY", startPos.y.ToString("F2")},
            {"EndX", endPos.x.ToString("F2")},
            {"EndY", endPos.y.ToString("F2")},
            {"StepDuration", stepDuration.ToString("F3")},
            {"StepSuccessful", successful.ToString()},
            {"OutcomeType", outcomeType},
            {"StepStartTime", stepStartTime.ToString("F3")},
            {"StepEndTime", stepEndTime.ToString("F3")},
            {"StepPressesRequired", pressesRequired.ToString()},
            {"TimePerPress", timePerPress.ToString("F3")},
            {"IsRushing", isRushing.ToString()},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
    }


    private (float timePerPress, bool isRushing) CalculateRushingMetrics(float movementTime, int requiredPresses)
    {
        // Standard threshold: Minimum 0.1 seconds per press to be considered valid 
        // => 30 presses per 5s => 30/300=0.1; flags only slow speeds
        const float RUSH_THRESHOLD_SEC_PER_PRESS = 0.1f;

        if (requiredPresses <= 0 || movementTime <= 0)
            return (-1, false);

        float timePerPress = movementTime / requiredPresses;

        // Now True only if presses are implausibly fast (< 0.1s each)
        bool isRushing = timePerPress < RUSH_THRESHOLD_SEC_PER_PRESS;

        return (timePerPress, isRushing);
    }


    public void LogExperimentEnd()
    {
        LogEvent("ExperimentEnd", new Dictionary<string, string>
        {
            {"EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
        });
        DumpUnloggedTrials();
    }


    #region Helper Methods
    private StringBuilder cachedLogData = new StringBuilder();
    public void LogEvent(string eventType, Dictionary<string, string> parameters)
    {
        // Ensure block number is 0 for practice trials
        if (parameters.ContainsKey("IsPractice") && bool.Parse(parameters["IsPractice"]))
        {
            parameters["BlockNumber"] = "0";
        }

        Debug.Log($"Logging event: {eventType} with {parameters.Count} parameters");

        foreach (var kvp in parameters)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}");
        }

        // First ensure logging is initialized
        if (!isInitialized)
        {
            Debug.LogWarning($"Logging not initialized when trying to log event: {eventType}. Initializing now.");
            InitializeLogging();
            if (!isInitialized)
            {
                Debug.LogError($"Failed to initialize logging for event: {eventType}");
                return; // Don't proceed if initialization failed
            }
        }
        var logData = new Dictionary<string, string>
{
    // Basic Info
    {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")},
    {"EventType", eventType},
    {"ParticipantID", participantID},
    {"ParticipantAge", participantAge.ToString()},
    {"ParticipantGender", participantGender},
    
    // Block/Trial Info
    {"IsPractice", "-"},
    {"BlockNumber", "-"},
    {"BlockType", "-"},
    {"TrialNumber", "-"},
    
    // Trial Parameters
    {"EffortLevel", "-"},
    {"RequiredPresses", "-"},
    {"DecisionType", "-"},
    {"ReactionTime", "-"},
    {"OutcomeType", "-"},
    
    // Performance Metrics
    {"RewardCollected", "-"},
    {"Points", "-"},

    // Scores
    {"TotalScore", "-"},
    {"PracticeScore", "-"},

           // Movement Step Details 
               {"MovementDuration", "-"},
        {"StepStartTime", "-"},
        {"StepEndTime", "-"},
        {"StepDuration", "-"},
        {"StepSuccessful", "-"},
        {"StepPressesRequired", "-"},
        
    // Position Data
    {"StartX", "-"},
    {"StartY", "-"},
    {"EndX", "-"},
    {"EndY", "-"},


    // Effort Info
    {"TotalPresses", "-"},
    {"PressData", "-"},
    {"TimePerPress", "-"},
    {"IsRushing", "-"},
    {"RushingThreshold", "-"},

    // Penalty Info
    {"PenaltyType", "-"},
    {"PenaltyDuration", "-"},
    
    // Block Status
    {"RemainingTime", "-"},
    {"ElapsedTime", "-"},
    {"CompletedTrials", "-"},
    {"TrialRate", "-"},
    
    // Calibration Data
    {"CalibrationEasy", "-"},
    {"CalibrationMedium", "-"},
    {"CalibrationHard", "-"},
    {"CalibrationPhase1Rate", "-"},
    {"CalibrationPhase2Rate", "-"},
    {"CalibrationPhase3Rate", "-"},
    {"CalibrationMaxRate", "-"},
    
    // Check Questions
    {"CheckPhase", "-"},
    {"QuestionNumber", "-"},
    {"QuestionType", "-"},
    {"QuestionText", "-"},
    {"SelectedAnswer", "-"},
    {"CorrectAnswer", "-"},
    {"IsCorrect", "-"},
    {"ResponseTime", "-"},
    
    // Check Scores
    {"Check1Score", "-"},
    {"Check2Score", "-"},
    {"ComprehensionScore", "-"},
    {"TotalCheckScore", "-"},
    {"FailedAttempts", "-"},
        
    // Participant Feedback
    {"TirednessRating", "-"},
    {"ParticipantFeedback", "-"},
    
    // Navigation
    {"ContinueButtonClicked", "-"},
    {"ButtonAction", "-"},
    {"RedirectURL", "-"}
};

        // Override defaults with actual values if provided
        foreach (var param in parameters)
        {
            if (logData.ContainsKey(param.Key))
            {
                logData[param.Key] = param.Value;
            }
            else
            {
                // Debug.LogWarning($"Unknown parameter key: {param.Key}");
            }
        }

        // Construct the log entry
        StringBuilder logEntry = new StringBuilder();
        foreach (var field in logData)
        {
            logEntry.Append($"{field.Value},");
        }

        // Remove trailing comma
        logEntry.Length--;

        // Write the log entry to the file using StreamWriter
        lock (logLock)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine(logEntry.ToString());
                    writer.Flush(); // Ensure the data is written to the file
                }
                if (m_ShowDebugLogManager) Debug.Log($"Logged event: {eventType}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write log entry: {e.Message}");
                unloggedTrials.Add(logEntry.ToString());
            }
        }
    }

    private string DetermineOutcomeType(string decisionType, bool rewardCollected)
    {
        switch (decisionType)
        {
            case "NoDecision":
                return "Timeout";
            case "Skip":
                return "Skip";
            case "Work":
                return rewardCollected ? "Success" : "Failure";
            default:
                return "Unknown";
        }
    }

    public void DumpUnloggedTrials()
    {
        if (unloggedTrials.Count > 0)
        {
            try
            {
                File.AppendAllLines(logFilePath, unloggedTrials);
                unloggedTrials.Clear();
                Debug.Log("Successfully dumped unlogged trials");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to dump unlogged trials: {e.Message}");
            }
        }
    }
    #endregion


    #region Calibration Logging
    public void LogCalibrationPhase(int phaseNumber, int totalPresses, float calibrationTime)
    {
        LogManager.Instance.LogEvent("CalibrationPhase", new Dictionary<string, string>
    {
        {"Tag", $"CALIB_PHASE_{phaseNumber}"},
        {"PhaseNumber", phaseNumber.ToString()},
        {"TotalPresses", totalPresses.ToString()},
        {"Duration", calibrationTime.ToString("F2")},
        {"PressesPerSecond", (totalPresses / calibrationTime).ToString("F2")},
        {"Category", "PhaseMetrics"},
        {"PhaseType", "Calibration"} // Add this line to distinguish calibration phase
    });
    }

    public void LogCalibrationResults(int phase1Presses, int phase2Presses, int phase3Presses,
    int maxPresses, int easyPresses, int mediumPresses, int hardPresses,
    float phase1Rate, float phase2Rate, float phase3Rate, float maxPressRate)
    {
        LogEvent("CalibrationResults", new Dictionary<string, string> {
        {"CalibrationPhase1Presses", phase1Presses.ToString()},
        {"CalibrationPhase2Presses", phase2Presses.ToString()},
        {"CalibrationPhase3Presses", phase3Presses.ToString()},
        {"CalibrationMaxPresses", maxPresses.ToString()},
        {"CalibrationEasyPresses", easyPresses.ToString()},
        {"CalibrationMediumPresses", mediumPresses.ToString()},
        {"CalibrationHardPresses", hardPresses.ToString()},
        {"CalibrationPhase1Rate", phase1Rate.ToString("F2")},
        {"CalibrationPhase2Rate", phase2Rate.ToString("F2")},
        {"CalibrationPhase3Rate", phase3Rate.ToString("F2")},
        {"CalibrationMaxRate", maxPressRate.ToString("F2")}
    });
    }

    #endregion

    public string GetCsvContent()
    {
        if (string.IsNullOrEmpty(logFilePath))
        {
            // Return just the simplified header if file doesn't exist
            return GenerateCSVHeader();
        }

        try
        {
            string csvContent = File.ReadAllText(logFilePath);
            return csvContent;
        }
        catch
        {
            return GenerateCSVHeader();
        }
    }

    public IEnumerator FinalizeAndUploadLogWithDelay()
    {
        // Ensure all data is written to the file
        lock (logLock)
        {
            try
            {
                // Use StreamWriter to flush the data to the file
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.Flush(); // Ensure all buffered data is written to the file
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to flush log file: {e.Message}");
            }
        }

        // Add a small delay to ensure all data is written to disk
        yield return new WaitForSeconds(2f); // Increased delay to 2 seconds

        // Read the CSV content
        string csvContent = GetCsvContent();
        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogError("No CSV content to upload!");
            yield break;
        }

        // Debug: Log the CSV content to verify it contains all data
        Debug.Log("CSV Content to Upload:\n" + csvContent);

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"decision_task_log_{participantID}_{timestamp}.csv";

        // Check if oneDriveUploader exists
        if (oneDriveUploader == null)
        {
            Debug.LogError("OneDriveUploader is null! Attempting to add component.");
            oneDriveUploader = gameObject.AddComponent<OneDriveUploader>();
            if (oneDriveUploader == null)
            {
                Debug.LogError("Failed to add OneDriveUploader component!");
                yield break;
            }
        }

        // Upload the file
        yield return oneDriveUploader.UploadFileToOneDrive(csvContent, Path.GetFileName(logFilePath));
    }


    public void FinalizeLogFile()
    {
        lock (logLock)
        {
            try
            {
                Debug.Log($"Finalizing log file: {logFilePath}");

                // First check if the builder has content
                if (logBuilder != null && logBuilder.Length > 0)
                {
                    Debug.Log($"Writing {logBuilder.Length} characters from logBuilder");

                    // Append the builder content to the file
                    File.AppendAllText(logFilePath, logBuilder.ToString());
                    logBuilder.Clear();
                }

                // Also check if there's any cached data
                if (cachedLogData != null && cachedLogData.Length > 0)
                {
                    Debug.Log($"Writing {cachedLogData.Length} characters from cachedLogData");

                    // Get the first line of the log file (header) to check if it exists
                    string header = "";
                    bool fileExists = File.Exists(logFilePath);

                    if (fileExists)
                    {
                        try
                        {
                            using (StreamReader reader = new StreamReader(logFilePath))
                            {
                                header = reader.ReadLine();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Could not read header: {ex.Message}");
                        }
                    }

                    // Write the cached data with or without header as appropriate
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        if (!fileExists || string.IsNullOrEmpty(header))
                        {
                            // File doesn't exist or has no header, so include header
                            writer.WriteLine("Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                                "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                                "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,ParticipantFeedback");
                        }

                        writer.Write(cachedLogData.ToString());
                        writer.Flush();
                    }

                    cachedLogData.Clear();
                }

                // Finally, force the file to flush to disk
                using (FileStream fs = new FileStream(logFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fs.Flush(true); // True forces flush to disk
                }

                // Verify the file has content
                long fileSize = new FileInfo(logFilePath).Length;
                Debug.Log($"Log file size after finalization: {fileSize} bytes");

                if (fileSize == 0)
                {
                    Debug.LogError("Log file is empty after finalization!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to finalize log file: {e.Message}");
            }
        }

        // Add a delay to ensure file system operations complete
        System.Threading.Thread.Sleep(1000); // 1 second delay
    }
}