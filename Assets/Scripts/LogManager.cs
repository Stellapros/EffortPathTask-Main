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
    #endregion

    #region Additional Fields for Behavioral Analysis
    private const float BLOCK_DURATION = 120f; // 2 minutes in seconds
    private List<Vector2> rewardPositions = new List<Vector2>();
    private Dictionary<int, List<float>> reactionTimesByBlock = new Dictionary<int, List<float>>();
    private Dictionary<int, List<float>> movementTimesByEffortLevel = new Dictionary<int, List<float>>();
    private Dictionary<int, int> skipCountByEffortLevel = new Dictionary<int, int>();
    private Dictionary<int, int> successCountByEffortLevel = new Dictionary<int, int>();
    #endregion

    private Dictionary<int, int> trialsCompletedPerBlock = new Dictionary<int, int>();
    private Dictionary<int, Vector2> playerPositions = new Dictionary<int, Vector2>();
    private Dictionary<string, object> blockMetrics = new Dictionary<string, object>();


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
        participantGender = PlayerPrefs.GetString("ParticipantGender", "Unknown");
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

    public void LogCheckQuestionResponse(int trialNumber, int checkQuestionNumber, string leftFruitName,
                                       string rightFruitName, string choice, bool isCorrect)
    {
        LogEvent("CheckQuestion", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"CheckQuestionNumber", checkQuestionNumber.ToString()},
            {"LeftOption", leftFruitName},
            {"RightOption", rightFruitName},
            {"Choice", choice},
            {"IsCorrect", isCorrect.ToString()}
        });
    }

    public void LogDecisionPhaseStart(int trialNumber)
    {
        LogEvent("DecisionPhaseStart", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"StartTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
        });
    }

    public void LogDecisionMade(int trialNumber, string decisionType, float reactionTime = -1)
    {
        LogEvent("Decision", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"DecisionType", decisionType},
            {"ReactionTime", reactionTime.ToString("F3")},
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
        LogEvent("NoDecision", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"TimeoutDuration", "5.0"},
            {"PenaltyApplied", "true"},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
    }

    public void LogSkipDecision(int trialNumber, string decisionType, float reactionTime = -1)
    {
        LogEvent("Skip", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"DecisionType", decisionType},
            {"PenaltyDuration", "3.0"},
            {"ReactionTime", reactionTime.ToString("F3")},
            {"PenaltyApplied", "true"},
            {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
        });
    }

    public void LogGridWorldPhaseStart(int trialNumber)
    {
        LogEvent("GridWorldStart", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"StartTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
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
    
    public void LogMovementOutcome(int trialNumber, bool rewardCollected, float movementDuration, int buttonPresses)
    {
        LogEvent("MovementOutcome", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"RewardCollected", rewardCollected.ToString()},
            {"MovementDuration", movementDuration.ToString("F3")},
            {"ButtonPresses", buttonPresses.ToString()}
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

    public void LogTrialOutcome(int trialNumber, int blockNumber, string decisionType, bool rewardCollected, float completionTime, int effortLevel)
    {
        // Update success/skip counts
        if (decisionType == "Skip")
        {
            skipCountByEffortLevel[effortLevel] = skipCountByEffortLevel.GetValueOrDefault(effortLevel, 0) + 1;
        }
        else if (rewardCollected)
        {
            successCountByEffortLevel[effortLevel] = successCountByEffortLevel.GetValueOrDefault(effortLevel, 0) + 1;
        }

        LogEvent("TrialOutcome", new Dictionary<string, string>
        {
            {"TrialNumber", AdjustToOneBasedIndex(trialNumber).ToString()},
            {"BlockNumber", AdjustToOneBasedIndex(blockNumber).ToString()},
            {"DecisionType", decisionType},
            {"RewardCollected", rewardCollected.ToString()},
            {"CompletionTime", completionTime.ToString("F3")},
            {"EffortLevel", effortLevel.ToString()},
            {"SuccessRate", GetSuccessRate(effortLevel).ToString("F3")}

        });
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
    private void LogEvent(string eventType, Dictionary<string, string> parameters)
    {
        StringBuilder logEntry = new StringBuilder();
        logEntry.Append($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff},");
        logEntry.Append($"{eventType},");
        logEntry.Append($"{participantID},");
        logEntry.Append($"{participantAge},");
        logEntry.Append($"{participantGender},");

        // Add standard fields (use "-" for missing values)
        string[] standardFields = new string[]
        {
            parameters.GetValueOrDefault("BlockNumber", "-"),
            parameters.GetValueOrDefault("TrialNumber", "-"),
            parameters.GetValueOrDefault("EffortLevel", "-"),
            parameters.GetValueOrDefault("RequiredPresses", "-"),
            parameters.GetValueOrDefault("DecisionType", "-"),
            parameters.GetValueOrDefault("ReactionTime", "-"),
            parameters.GetValueOrDefault("OutcomeType", "-"),
            parameters.GetValueOrDefault("RewardCollected", "-"),
            parameters.GetValueOrDefault("MovementDuration", "-"),
            parameters.GetValueOrDefault("ButtonPresses", "-"),
            parameters.GetValueOrDefault("TotalScore", "-"),
            parameters.GetValueOrDefault("CheckQuestionNumber", "-"),
            parameters.GetValueOrDefault("Choice", "-"),
            parameters.GetValueOrDefault("IsCorrect", "-")
        };

        logEntry.Append(string.Join(",", standardFields));

        // Add any additional parameters as extra info
        string additionalInfo = string.Join(";", parameters
            .Where(p => !standardFields.Contains(p.Value))
            .Select(p => $"{p.Key}={p.Value}"));
        logEntry.Append($",{additionalInfo}");

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

    // // For work decisions
    // logManager.LogDecisionMade(currentTrialIndex, "Work", reactionTime);

    // // For skip decisions
    // logManager.LogSkipDecision(currentTrialIndex, reactionTime);
    // logManager.LogPenaltyApplied(currentTrialIndex, "Skip", 3.0f);

    // // For no decisions
    // logManager.LogNoDecision(currentTrialIndex);
    // logManager.LogPenaltyApplied(currentTrialIndex, "NoDecision", 5.0f);

    // // At the end of each trial
    // logManager.LogTrialOutcome(currentTrialIndex, decisionType, rewardCollected, completionTime);
    // logManager.LogTrialProgression(currentTrialIndex, currentBlockNumber, progressionType);



    // // Your internal code can use 0-based indexing
    // int currentTrialIndex = 0;  // First trial
    // int currentBlockIndex = 0;  // First block

    // // The logging will automatically convert to 1-based indexing
    // logManager.LogTrialStart(currentTrialIndex, currentBlockIndex, effortLevel, requiredPresses, isPractice);
    // // This will log as Trial 1, Block 1

    // // For the last trial in first block
    // currentTrialIndex = 8;  // (TRIALS_PER_BLOCK - 1)
    // logManager.LogTrialEnd(currentTrialIndex, rewardCollected, trialDuration, reactionTime);
    // // This will log as Trial 9, Block 1
}