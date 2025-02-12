using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public class LogManager : MonoBehaviour
{
    #region Singleton
    public static LogManager Instance { get; private set; }
    #endregion

    #region Private Fields
    private const string LOG_DIRECTORY = "ExperimentLogs";
    private string logFilePath;
    private List<string> unloggedTrials = new List<string>();
    private readonly object logLock = new object();
    [SerializeField] private bool m_ShowDebugLogManager;

    // Participant info
    private string participantID;
    private int participantAge;
    private string participantGender;
    private ParticipantInfo participantInfo;
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
    }

    private static class LogType
    {
        public const string SETUP = "SETUP";
        public const string SESSION = "SESSION";
        public const string BLOCK = "BLOCK";
        public const string TRIAL = "TRIAL";
        public const string DECISION = "DECISION";
        public const string MOVEMENT = "MOVEMENT";
        public const string SCORE = "SCORE";
        public const string PENALTY = "PENALTY";
        public const string CALIBRATION = "CALIBRATION";
    }

    #region Unity Lifecycle
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadParticipantInfo();
            InitializeLogging();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Initialization
    private void LoadParticipantInfo()
    {
        participantID = PlayerPrefs.GetString("ParticipantID", "Unknown");
        participantAge = PlayerPrefs.GetInt("ParticipantAge", 0);
        participantGender = PlayerPrefs.GetString("ParticipantGender", "Unvknown");
    }

    public string LogFilePath => logFilePath;

    private void InitializeLogging()
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string strDir = Path.Combine(Application.dataPath, "_ExpData");
        Directory.CreateDirectory(strDir);
        logFilePath = Path.Combine(strDir, $"decision_task_log_{participantID}_{timestamp}.csv");

        // Create header for the log file
        string header = "Timestamp,EventType,ParticipantID,ParticipantAge,ParticipantGender,BlockNumber,TrialNumber," +
                       "EffortLevel,RequiredPresses,DecisionType,DecisionRT,OutcomeType,RewardCollected,MovementDuration," +
                       "ButtonPresses,TotalScore,CheckQuestionNumber,CheckQuestionResponse,CheckQuestionCorrect,AdditionalInfo\n";
        try
        {
            File.WriteAllText(logFilePath, header);
            if (m_ShowDebugLogManager) Debug.Log($"Initialized logging to: {logFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize logging: {e.Message}");
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

    public void LogTrialStart(int trialNumber, int blockNumber, int effortLevel, int requiredPresses, bool isPractice)
    {
        var state = new TrialState
        {
            effortLevel = effortLevel,
            requiredPresses = requiredPresses,
            isComplete = false
        };
        trialStates[trialNumber] = state;

        LogEvent("TrialStart", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"EffortLevel", effortLevel.ToString()},
            {"RequiredPresses", requiredPresses.ToString()},
            {"IsPractice", isPractice.ToString()}
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
    public void LogDecisionPhaseStart(int trialNumber)
    {
        if (!trialStates.ContainsKey(trialNumber))
        {
            trialStates[trialNumber] = new TrialState();
        }

        var state = trialStates[trialNumber];
        state.decisionStartTime = Time.time;

        LogEvent("DecisionPhaseStart", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"StartTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    // public void LogDecisionMade(int trialNumber, string decisionType, float reactionTime = -1)
    // {
    //     // Calculate decision time if not provided
    //     if (reactionTime < 0 && decisionStartTimes.ContainsKey(trialNumber))
    //     {
    //         reactionTime = Time.time - decisionStartTimes[trialNumber];
    //         decisionStartTimes.Remove(trialNumber); // Cleanup
    //     }

    //     decisionTypes[trialNumber] = decisionType;

    //     LogEvent("Decision", new Dictionary<string, string>
    //     {
    //         {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
    //         {"DecisionType", decisionType},
    //         {"DecisionTime", reactionTime.ToString("F3")},
    //         {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    //     });
    // }

public void LogDecisionMade(int trialNumber, string decisionType, float decisionTime)
{
    Dictionary<string, string> parameters = new Dictionary<string, string>
    {
        { "TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString() },
        { "BlockNumber", ExperimentManager.Instance.GetCurrentBlockNumber().ToString() },
        { "DecisionType", decisionType },
        { "DecisionTime", decisionTime.ToString("F3") }
    };
    
    LogEvent("Decision", parameters);
}

    public void LogWorkDecision(int trialNumber, float reactionTime = -1)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        if (reactionTime < 0)
        {
            reactionTime = Time.time - state.decisionStartTime;
        }

        state.decisionType = "Work";
        state.decisionTime = reactionTime;

        LogEvent("Decision", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"BlockNumber", ExperimentManager.Instance.GetCurrentBlockNumber().ToString()},  // Added BlockNumber
        {"DecisionType", "Work"},
        {"DecisionTime", reactionTime.ToString("F3")},
        {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    public void LogSkipDecision(int trialNumber, float reactionTime = -1)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        if (reactionTime < 0)
        {
            reactionTime = Time.time - state.decisionStartTime;
        }

        state.decisionType = "Skip";
        state.decisionTime = reactionTime;

        LogEvent("Decision", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"BlockNumber", ExperimentManager.Instance.GetCurrentBlockNumber().ToString()},  // Added BlockNumber
        {"DecisionType", "Skip"},
        {"DecisionTime", reactionTime.ToString("F3")},
        {"PenaltyDuration", "3.0"},
        {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    public void LogDecisionOutcome(int trialNumber, string decisionType)
    {
        LogEvent("DecisionOutcome", new Dictionary<string, string> {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"DecisionType", decisionType},
        {"Timestamp", System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    public void LogNoDecision(int trialNumber)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        float timeElapsed = Time.time - state.decisionStartTime;

        state.decisionType = "NoDecision";
        state.decisionTime = timeElapsed;

        // Create parameters with all required fields
        var parameters = new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"BlockNumber", ExperimentManager.Instance.GetCurrentBlockNumber().ToString()},  // Add BlockNumber
            {"DecisionType", "NoDecision"},
            {"DecisionTime", timeElapsed.ToString("F3")},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        };

        // Log the decision event with complete parameters
        LogEvent("Decision", parameters);

        // Then log the penalty
        LogEvent("PenaltyStart", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"PenaltyType", "NoDecision"},
            {"Duration", "5.0"}
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

    public void LogMovementOutcome(int trialNumber, bool rewardCollected, int buttonPresses)
    {
        if (!trialStates.ContainsKey(trialNumber)) return;

        var state = trialStates[trialNumber];
        float movementTime = Time.time - state.movementStartTime;

        state.rewardCollected = rewardCollected;
        state.buttonPresses = buttonPresses;
        state.movementTime = movementTime;

        string outcome = rewardCollected ? "Hit" : "Miss";

        LogEvent("MovementOutcome", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"Outcome", outcome},
            {"RewardCollected", rewardCollected.ToString()},
            {"MovementTime", movementTime.ToString("F3")},
            {"ButtonPresses", buttonPresses.ToString()},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
    }

    public void LogCollisionTime(int trialNumber)
    {
        LogEvent("CollisionTime", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"CollisionTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
    });
    }

    #endregion

    public void LogScoreUpdate(int trialNumber, bool isPractice, int points, string outcome)
    {
        LogEvent("ScoreUpdate", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"IsPractice", isPractice.ToString()},
            {"Points", points.ToString()},
            {"Outcome", outcome}
        });
    }

    public void LogScoreReset(int trialNumber, bool isPractice, int oldScore, int newScore, string reason)
    {
        LogEvent("ScoreReset", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"IsPractice", isPractice.ToString()},
            {"OldScore", oldScore.ToString()},
            {"NewScore", newScore.ToString()},
            {"ResetReason", reason}
        });
    }

    public void LogSkipPenaltyStart(int trialNumber, string penaltyType, float duration)
    {
        LogEvent("PenaltyStart", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"PenaltyType", penaltyType},
            {"Duration", duration.ToString()}
        });
    }

    public void LogSkipPenaltyEnd(int trialNumber, string penaltyType)
    {
        LogEvent("PenaltyEnd", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"PenaltyType", penaltyType}
        });
    }

    public void LogPenaltyStart(int trialNumber, string penaltyType, float duration)
    {
        LogEvent("PenaltyStart", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"PenaltyType", penaltyType},
            {"Duration", duration.ToString()}
        });
    }

    public void LogPenaltyEnd(int trialNumber, string penaltyType)
    {
        LogEvent("PenaltyEnd", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"PenaltyType", penaltyType}
        });
    }

    public void LogPenaltyApplied(int trialNumber, string decisionType, float penaltyDuration)
    {
        LogEvent("Penalty", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"DecisionType", decisionType},
            {"PenaltyDuration", penaltyDuration.ToString("F3")},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
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

        // Determine the outcome type based on decision type and reward collection
        string outcomeType = DetermineOutcomeType(state.decisionType, state.rewardCollected);

        LogEvent("TrialOutcome", new Dictionary<string, string>
    {
        {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
        {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
        {"DecisionType", state.decisionType},
        {"DecisionTime", state.decisionTime.ToString("F3")},
        {"RewardCollected", state.rewardCollected.ToString()},
        {"MovementTime", state.movementTime.ToString("F3")},
        {"ButtonPresses", state.buttonPresses.ToString()},
        {"EffortLevel", state.effortLevel.ToString()},
        {"RequiredPresses", state.requiredPresses.ToString()},
        {"OutcomeType", outcomeType}  // Added required OutcomeType parameter
    });

        // Cleanup trial state after logging
        trialStates.Remove(trialNumber);
    }

    // public void LogTrialOutcome(int trialNumber, int blockNumber, bool rewardCollected, float completionTime, int effortLevel)
    // {
    //     string decisionType = decisionTypes.GetValueOrDefault(trialNumber, "Unknown");

    //     // Update success/skip counts
    //     if (decisionType == "Skip")
    //     {
    //         skipCountByEffortLevel[effortLevel] = skipCountByEffortLevel.GetValueOrDefault(effortLevel, 0) + 1;
    //     }
    //     else if (rewardCollected)
    //     {
    //         successCountByEffortLevel[effortLevel] = successCountByEffortLevel.GetValueOrDefault(effortLevel, 0) + 1;
    //     }

    //     LogEvent("TrialOutcome", new Dictionary<string, string>
    //     {
    //         {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
    //         {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
    //         {"DecisionType", decisionType},
    //         {"RewardCollected", rewardCollected.ToString()},
    //         {"CompletionTime", completionTime.ToString("F3")},
    //         {"EffortLevel", effortLevel.ToString()},
    //         {"SuccessRate", GetSuccessRate(effortLevel).ToString("F3")},
    //         {"Outcome", rewardCollected ? "Hit" : decisionType == "Skip" ? "Skip" : "Miss"}
    //     });

    //     // Cleanup decision type after logging
    //     decisionTypes.Remove(trialNumber);
    // }

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
        // First, validate required parameters based on event type
        ValidateRequiredParameters(eventType, parameters);

        // Create a dictionary with default values for all fields
        var logData = new Dictionary<string, string>
    {
        {"BlockNumber", "-"},
        {"TrialNumber", "-"},
        {"EffortLevel", "-"},
        {"RequiredPresses", "-"},
        {"DecisionType", "-"},
        {"DecisionTime", "-"},
        {"OutcomeType", "-"},
        {"RewardCollected", "-"},
        {"MovementTime", "-"},
        {"ButtonPresses", "-"},
        {"TotalScore", "-"},
        {"CheckQuestionNumber", "-"},
        {"CheckQuestionResponse", "-"},
        {"CheckQuestionCorrect", "-"}
    };

        // Override defaults with actual values if provided
        foreach (var param in parameters)
        {
            logData[param.Key] = param.Value;
        }

        // Add context-specific values
        if (eventType == "Decision")
        {
            logData["TotalScore"] = ScoreManager.Instance?.GetTotalScore().ToString() ?? "-";
        }

        StringBuilder logEntry = new StringBuilder();
        logEntry.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},");
        logEntry.Append($"{eventType},");
        logEntry.Append($"{participantID},");
        logEntry.Append($"{participantAge},");
        logEntry.Append($"{participantGender},");

        // Add all fields in specific order
        foreach (var field in logData)
        {
            logEntry.Append($"{field.Value},");
        }

        // Remove trailing comma
        logEntry.Length--;

        lock (logLock)
        {
            try
            {
                File.AppendAllText(logFilePath, logEntry.ToString() + "\n");
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

    // Update LogEvent validation
    private List<string> GetRequiredParameters(string eventType)
    {
        switch (eventType)
        {
            case "Decision":
                return new List<string> { "TrialNumber", "BlockNumber", "DecisionType", "DecisionTime" };
            case "MovementOutcome":
                return new List<string> { "TrialNumber", "RewardCollected", "MovementTime", "ButtonPresses" };
            case "TrialOutcome":
                return new List<string> { "TrialNumber", "BlockNumber", "DecisionType", "OutcomeType" };
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
        LogEvent("CalibrationPhase", new Dictionary<string, string>
    {
        {"Tag", $"CALIB_PHASE_{phaseNumber}"},
        {"PhaseNumber", phaseNumber.ToString()},
        {"TotalPresses", totalPresses.ToString()},
        {"Duration", calibrationTime.ToString("F2")},
        {"PressesPerSecond", (totalPresses / calibrationTime).ToString("F2")},
        {"Category", "PhaseMetrics"}
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
}