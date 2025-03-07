using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;

public class LogManager : MonoBehaviour
{
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
    public string participantID;
    private int participantAge;
    private string participantGender;
    // private ParticipantInfo participantInfo;
    #endregion

    #region Additional Fields for Behavioral Analysis
    private const float BLOCK_DURATION = 120f; // 2 minutes in seconds
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
        DontDestroyOnLoad(gameObject); // Ensure the LogManager persists across scenes
        InitializeLogging(); // Initialize logging only once
        Debug.Log("LogManager initialized and set to persist across scenes.");
    }
    else if (Instance != this)
    {
        Debug.Log("Duplicate LogManager instance destroyed.");
        Destroy(gameObject); // Destroy duplicate instances
    }
}

private void OnEnable()
{
    Debug.Log("LogManager GameObject enabled.");
}

private void OnDisable()
{
    Debug.Log("LogManager GameObject disabled.");
}
    #endregion

    #region Initialization
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
    string strDir = Path.Combine(Application.persistentDataPath, LOG_DIRECTORY); // Use persistentDataPath for cross-platform compatibility
    Directory.CreateDirectory(strDir);
    logFilePath = Path.Combine(strDir, $"decision_task_log_{participantID}_{timestamp}.csv");

    Debug.Log($"Logging initialized. Log file is saved at: {logFilePath}");

    // Create header for the log file
    string header = "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                   "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                   "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,ParticipantFeedback\n";

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

    // Replace the EnsureLogFileInitialized with a simpler check
    public void EnsureLogFileInitialized()
    {
        if (!isInitialized)
        {
            Debug.Log("Ensuring log file is initialized");
            InitializeLogging();
        }
    }
    #endregion

    #region Logging Methods
    private int AdjustToOneBasedIndex(int index)
    {
        return index + 1;
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
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()}
        });
    }


    // Modified trial start logging to handle both practice and regular trials
    public void LogTrialStart(int trialNumber, int blockNumber, int effortLevel, int requiredPresses, bool isPractice)
    {
        // Ensure proper trial number handling
        int adjustedTrialNumber = isPractice ? trialNumber : AdjustToOneBasedIndex(trialNumber);

        var state = new TrialState
        {
            effortLevel = effortLevel,
            requiredPresses = requiredPresses,
            isComplete = false
        };
        trialStates[adjustedTrialNumber] = state;

        LogEvent(isPractice ? "PracticeTrialStart" : "TrialStart", new Dictionary<string, string>
        {
            {"TrialNumber", adjustedTrialNumber.ToString()},
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"EffortLevel", effortLevel.ToString()},
            {"RequiredPresses", requiredPresses.ToString()},
            {"IsPractice", isPractice.ToString().ToLower()}
        });
    }

    public void LogRewardPosition(int trialNumber, Vector2 position, int effortLevel)
    {
        rewardPositions.Add(position);
        LogEvent("RewardPosition", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"PositionX", position.x.ToString("F2")},
            {"PositionY", position.y.ToString("F2")},
            {"EffortLevel", effortLevel.ToString()},
            {"DistanceFromCenter", Vector2.Distance(Vector2.zero, position).ToString("F2")}
        });
    }

    public void LogEffortLevelMetrics(int trialNumber, int effortLevel, int requiredPresses, float completionTime)
    {
        if (!movementTimesByEffortLevel.ContainsKey(effortLevel))
        {
            movementTimesByEffortLevel[effortLevel] = new List<float>();
        }
        movementTimesByEffortLevel[effortLevel].Add(completionTime);

        LogEvent("EffortMetrics", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"EffortLevel", effortLevel.ToString()},
            {"RequiredPresses", requiredPresses.ToString()},
            {"CompletionTime", completionTime.ToString("F3")},
            {"AverageTimeForLevel", movementTimesByEffortLevel[effortLevel].Average().ToString("F3")}
        });
    }

    public void LogDecisionMetrics(int trialNumber, int blockNumber, string decisionType, float reactionTime)
    {
        if (!reactionTimesByBlock.ContainsKey(blockNumber))
        {
            reactionTimesByBlock[blockNumber] = new List<float>();
        }
        reactionTimesByBlock[blockNumber].Add(reactionTime);

        LogEvent("DecisionMetrics", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"DecisionType", decisionType},
            {"ReactionTime", reactionTime.ToString("F3")},
            {"AverageRTForBlock", reactionTimesByBlock[blockNumber].Average().ToString("F3")},
            {"MedianRTForBlock", GetMedian(reactionTimesByBlock[blockNumber]).ToString("F3")}
        });
    }

    public void LogBehavioralSummary(int blockNumber)
    {
        var summary = new Dictionary<string, float>();
        foreach (var kvp in skipCountByEffortLevel)
        {
            int totalTrials = skipCountByEffortLevel[kvp.Key] + successCountByEffortLevel.GetValueOrDefault(kvp.Key, 0);
            float skipRate = totalTrials > 0 ? (float)kvp.Value / totalTrials : 0f;
            summary[$"SkipRate_Effort{kvp.Key}"] = skipRate;
        }

        LogEvent("BlockSummary", new Dictionary<string, string>
        {
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"AverageRT", reactionTimesByBlock.GetValueOrDefault(blockNumber, new List<float>()).Average().ToString("F3")},
            {"SkipRates", string.Join(";", summary.Select(kvp => $"{kvp.Key}={kvp.Value:F3}"))}
        });
    }


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
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"CheckPhase", checkPhase.ToString()},
            {"QuestionNumber", questionNumber.ToString()},
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

        LogEvent("CHECK_QUESTION", logData);
    }

    public void LogCheckPhaseComplete(
        int checkPhase,
        int totalQuestions,
        int correctAnswers,
        float completionTime,
        string phaseType,  // e.g., "FrequencyCheck", "ComprehensionCheck", "PairwiseCheck"
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

        // Add any phase-specific data
        if (phaseSpecificData != null)
        {
            foreach (var item in phaseSpecificData)
            {
                logData[$"PhaseSpecific_{item.Key}"] = item.Value;
            }
        }

        LogEvent("CHECK_PHASE_COMPLETE", logData);
    }

    public void LogCheckResults(
        Dictionary<string, int> phaseScores,  // key: phase name, value: score
        int totalScore,
        int attemptNumber,
        bool passedChecks,
        Dictionary<string, string> additionalMetrics = null)
    {
        var logData = new Dictionary<string, string>
        {
            {"TotalScore", totalScore.ToString()},
            {"AttemptNumber", attemptNumber.ToString()},
            {"PassedChecks", passedChecks.ToString()},
            {"MinimumRequiredScore", "11"},  // Configurable constant
            {"MaxPossibleScore", "13"},      // Configurable constant
            {"CompletionTimestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        };

        // Add individual phase scores
        foreach (var score in phaseScores)
        {
            logData[$"Score_{score.Key}"] = score.Value.ToString();
        }

        // Add any additional metrics
        if (additionalMetrics != null)
        {
            foreach (var metric in additionalMetrics)
            {
                logData[$"Additional_{metric.Key}"] = metric.Value;
            }
        }

        LogEvent("CHECK_RESULTS", logData);
    }

    public void LogCheckRetry(
        int attemptNumber,
        Dictionary<string, int> previousScores,
        string retryReason)
    {
        var logData = new Dictionary<string, string>
        {
            {"AttemptNumber", attemptNumber.ToString()},
            {"RetryReason", retryReason},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        };

        // Add previous scores
        foreach (var score in previousScores)
        {
            logData[$"PreviousScore_{score.Key}"] = score.Value.ToString();
        }

        LogEvent("CHECK_RETRY", logData);
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

    public void LogDecisionOutcome(int trialNumber, int blockNumber, string decisionType, bool rewardCollected, float decisionTime, float movementTime, int buttonPresses, int effortLevel, int requiredPresses)
    {
        string outcomeType = DetermineOutcomeType(decisionType, rewardCollected);

        LogEvent("DecisionOutcome", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
        {"DecisionType", decisionType},
        {"DecisionTime", decisionTime.ToString("F3")},
        {"RewardCollected", rewardCollected.ToString()},
        {"MovementTime", movementTime.ToString("F3")},
        {"ButtonPresses", buttonPresses.ToString()},
        {"EffortLevel", effortLevel.ToString()},
        {"RequiredPresses", requiredPresses.ToString()},
        {"OutcomeType", outcomeType}
    });
    }

    #endregion

    #region Movement Phase Logging
    public void LogGridWorldPhaseStart(int trialNumber)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        state.movementStartTime = Time.time;

        LogEvent("GridWorldStart", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"StartTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
    }

    public void LogPlayerMovement(int trialNumber, Vector2 startPosition, Vector2 endPosition, float movementTime)
    {
        LogEvent("PlayerMovement", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"StartX", startPosition.x.ToString("F2")},
            {"StartY", startPosition.y.ToString("F2")},
            {"EndX", endPosition.x.ToString("F2")},
            {"EndY", endPosition.y.ToString("F2")},
            {"MovementTime", movementTime.ToString("F3")},
            {"Distance", Vector2.Distance(startPosition, endPosition).ToString("F2")}
        });
    }


    public void LogCollisionTime(int trialNumber)
    {
        // Ensure we have a valid ExperimentManager before logging
        if (ExperimentManager.Instance != null)
        {
            // Get the current trial index dynamically
            int currentTrialIndex = ExperimentManager.Instance.GetCurrentTrialIndex();
            Debug.Log($"Logging collision time for trial index: {currentTrialIndex}");

            // Log the collision time
            LogEvent("CollisionTime", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(currentTrialIndex).ToString()},
            {"CollisionTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
        }
        else
        {
            Debug.LogError("ExperimentManager is null when trying to log collision time!");
        }
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
        }

        LogScoreUpdateComplete(trialNumber, isPractice, points, outcome, totalScore, practiceScore, blockNumber);
    }

    public void LogScoreUpdateComplete(int trialNumber, bool isPractice, int points, string outcome,
                                      int totalScore, int practiceScore, int blockNumber)
    {
        LogEvent("ScoreUpdate", new Dictionary<string, string>
    {
        {"TrialNumber", trialNumber.ToString()},
        {"IsPractice", isPractice.ToString()},
        {"Points", points.ToString()},
        {"Outcome", outcome},
        {"TotalScore", totalScore.ToString()},
        {"PracticeScore", practiceScore.ToString()},
        // {"BlockNumber", blockNumber.ToString()}
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
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
            {"PenaltyType", penaltyType},
            {"Duration", duration.ToString()}
        });
    }


    public void LogTrialEnd(int trialNumber, bool rewardCollected, float trialDuration, float actionReactionTime)
    {
        LogEvent("TrialEnd", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"Outcome", rewardCollected ? "Success" : "Failure"},
        {"TrialDuration", trialDuration.ToString("F2")},
        {"ReactionTime", actionReactionTime.ToString("F2")},
        {"Timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    public void LogTrialOutcome(int trialNumber, int blockNumber)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        if (state.isComplete) return;  // Prevent duplicate logging

        state.isComplete = true;

        // Determine outcome type based on the complete trial state
        string outcomeType = state.outcomeType ?? DetermineOutcomeType(state.decisionType, state.rewardCollected);

        LogEvent("TrialOutcome", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"DecisionType", state.decisionType},
            {"DecisionTime", state.decisionTime.ToString("F3")},
            {"RewardCollected", state.rewardCollected.ToString().ToLower()}, // Ensure boolean is lowercase
            {"MovementTime", state.movementTime.ToString("F3")},
            {"ButtonPresses", state.buttonPresses.ToString()},
            {"EffortLevel", state.effortLevel.ToString()},
            {"RequiredPresses", state.requiredPresses.ToString()},
            {"OutcomeType", outcomeType}
        });

        // Cleanup trial state after logging
        trialStates.Remove(trialNumber);
    }


    public void LogBlockTimeStatus(int blockNumber, float remainingTime, int completedTrials)
    {
        float elapsedTime = BLOCK_DURATION - remainingTime;
        float trialsPerMinute = elapsedTime > 0 ? (completedTrials / elapsedTime * 60) : 0;

        LogEvent("BlockTimeStatus", new Dictionary<string, string>
        {
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"RemainingTime", remainingTime.ToString("F2")},
            {"CompletedTrials", completedTrials.ToString()},
            {"ElapsedTime", elapsedTime.ToString("F2")},
            {"TrialRate", trialsPerMinute.ToString("F2")}
        });
    }

    public void LogBlockEnd(int blockNumber)
    {
        LogEvent("BlockEnd", new Dictionary<string, string>
        {
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
        });
    }

    public void LogExperimentEnd()
    {
        LogEvent("ExperimentEnd", new Dictionary<string, string>
        {
            {"EndTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
        });
        DumpUnloggedTrials();
    }
    #endregion

    #region Helper Methods
    public void LogEvent(string eventType, Dictionary<string, string> parameters)
    {
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
        // Define the default values for all columns
        var logData = new Dictionary<string, string>
    {
        {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")},
        {"EventType", eventType},
        {"ParticipantID", participantID},
        {"ParticipantAge", participantAge.ToString()},
        {"ParticipantGender", participantGender},
        {"BlockNumber", "-"},
        {"TrialNumber", "-"},
        {"EffortLevel", "-"},
        {"RequiredPresses", "-"},
        {"DecisionType", "-"},
        {"DecisionTime", "-"},
        {"OutcomeType", "-"},
        {"RewardCollected", "-"},
        {"MovementDuration", "-"},
        {"TotalPresses", "-"},
        {"TotalScore", "-"},
        {"PracticeScore", "-"},
        {"AdditionalInfo", "-"},
        {"StartX", "-"},
        {"StartY", "-"},
        {"EndX", "-"},
        {"EndY", "-"},
        {"StepDuration", "-"},
        {"TirednessRating", "-"},
        {"ParticipantFeedback", "-"} // Ensure Feedback column is included
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
                Debug.LogWarning($"Unknown parameter key: {param.Key}");
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

    private void ValidateRequiredParameters(string eventType, Dictionary<string, string> parameters)
    {
        var requiredParams = GetRequiredParameters(eventType);
        var missingParams = requiredParams.Where(p => !parameters.ContainsKey(p)).ToList();

        if (missingParams.Any())
        {
            Debug.LogWarning($"Missing required parameters for event {eventType}: {string.Join(", ", missingParams)}");
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

    private List<string> GetRequiredParameters(string eventType)
    {
        switch (eventType)
        {
            case "TrialStart":
                return new List<string> { "TrialNumber", "BlockNumber", "EffortLevel", "RequiredPresses" };
            case "TrialEnd":
                return new List<string> { "TrialNumber", "BlockNumber", "DecisionType", "OutcomeType", "RewardCollected", "MovementDuration", "ButtonPresses" };
            case "Decision":
                return new List<string> { "TrialNumber", "BlockNumber", "DecisionType", "DecisionRT" };
            case "MovementStart":
                return new List<string> { "TrialNumber", "StartX", "StartY" };
            case "MovementEnd":
                return new List<string> { "TrialNumber", "EndX", "EndY", "MovementDuration", "RewardCollected" };
            default:
                return new List<string>();
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

    #region Analysis Helper Methods
    private float GetMedian(List<float> values)
    {
        var sortedValues = values.OrderBy(v => v).ToList();
        int mid = sortedValues.Count / 2;
        return sortedValues.Count % 2 == 0
            ? (sortedValues[mid - 1] + sortedValues[mid]) / 2
            : sortedValues[mid];
    }

    private float GetSuccessRate(int effortLevel)
    {
        int successes = successCountByEffortLevel.GetValueOrDefault(effortLevel, 0);
        int totalAttempts = successes + skipCountByEffortLevel.GetValueOrDefault(effortLevel, 0);
        return totalAttempts > 0 ? (float)successes / totalAttempts : 0f;
    }
    #endregion
    // Add these methods to the LogManager class:

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
        int averagePresses, int easyPresses, int mediumPresses, int hardPresses)
    {
        // Log overall calibration summary
        LogEvent("CalibrationSummary", new Dictionary<string, string>
    {
        {"Tag", "CALIB_SUMMARY"},
        {"Category", "Summary"},
        {"Phase1Presses", phase1Presses.ToString()},
        {"Phase2Presses", phase2Presses.ToString()},
        {"Phase3Presses", phase3Presses.ToString()},
        {"AveragePresses", averagePresses.ToString()},
        {"StandardDeviation", CalculateStandardDeviation(phase1Presses, phase2Presses, phase3Presses).ToString("F2")}
    });

        // Log difficulty level assignments
        LogEvent("CalibrationLevels", new Dictionary<string, string>
    {
        {"Tag", "CALIB_LEVELS"},
        {"Category", "DifficultySettings"},
        {"EasyLevel_RequiredPresses", easyPresses.ToString()},
        {"MediumLevel_RequiredPresses", mediumPresses.ToString()},
        {"HardLevel_RequiredPresses", hardPresses.ToString()},
        {"PressRangeSpread", (hardPresses - easyPresses).ToString()}
    });
    }
    private float CalculateStandardDeviation(params int[] values)
    {
        float mean = (float)values.Average();
        float sumOfSquares = values.Sum(x => (x - mean) * (x - mean));
        return Mathf.Sqrt(sumOfSquares / values.Length);
    }
    #endregion

    public string GetCsvContent()
    {
        if (string.IsNullOrEmpty(logFilePath))
        {
            Debug.LogWarning("Log file path is not set! Initializing log file...");
            InitializeLogging();

            // If it's still null after trying to initialize, return an empty CSV with headers
            if (string.IsNullOrEmpty(logFilePath))
            {
                Debug.LogError("Failed to initialize log file path!");

                // Create header for the log file
                return "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                               "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                               "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,ParticipantFeedback\n"; // Added Feedback column
            }
        }

        try
        {
            // Read the entire file content
            string csvContent = File.ReadAllText(logFilePath);
            Debug.Log($"Read {csvContent.Split('\n').Length} lines from the log file."); // Debug log to verify line count

            // Check if the content is valid
            if (string.IsNullOrEmpty(csvContent))
            {
                Debug.LogWarning("CSV content is empty!");

                // Create header for the log file
                return "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                               "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                               "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,ParticipantFeedback\n"; // Added Feedback column
            }

            return csvContent;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read log file: {e.Message}");

            // Return minimal CSV with headers as fallback

            // Create header for the log file
            return "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                           "EffortLevel,RequiredPresses,DecisionType,DecisionTime,OutcomeType,RewardCollected,MovementDuration," +
                           "TotalPresses,TotalScore,PracticeScore,AdditionalInfo,StartX,StartY,EndX,EndY,StepDuration,TirednessRating,ParticipantFeedback\n"; // Added Feedback column
        }
    }


    public void TestUpload()
    {
        Debug.Log("Testing CSV upload...");
        StartCoroutine(FinalizeAndUploadLogWithDelay());
    }

    public IEnumerator FinalizeAndUploadLogWithDelay()
    {
        // First, finalize the log file
        FinalizeLogFile();

        // Add a longer delay to ensure all data is written
        yield return new WaitForSeconds(2f);

        // Read the CSV content - ensure it's reading the full file
        string csvContent = File.ReadAllText(logFilePath);
        if (string.IsNullOrEmpty(csvContent))
        {
            Debug.LogError("No CSV content to upload!");
            yield break;
        }

        // Debug: Log the CSV content size to verify it contains all data
        Debug.Log($"CSV Content size to Upload: {csvContent.Length} bytes, {csvContent.Split('\n').Length} lines");

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string fileName = $"decision_task_log_{participantID}_{timestamp}.csv";

        // Check if oneDriveUploader exists
        if (oneDriveUploader == null)
        {
            Debug.LogError("OneDriveUploader is null! Attempting to add component.");
            oneDriveUploader = gameObject.AddComponent<OneDriveUploader>();
        }

        // Upload the file
        yield return oneDriveUploader.UploadFileToOneDrive(csvContent, fileName);
    }

    public void FinalizeLogFile()
    {
        lock (logLock)
        {
            try
            {
                if (logBuilder != null && logBuilder.Length > 0)
                {
                    File.AppendAllText(logFilePath, logBuilder.ToString());
                    logBuilder.Clear();
                    Debug.Log("Finalized log file by writing all buffered data.");
                }

                // Ensure the file system fully flushes data
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.Flush();
                }

                Debug.Log($"Finalized log file: {logFilePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to finalize log file: {e.Message}");
            }
        }
    }
}