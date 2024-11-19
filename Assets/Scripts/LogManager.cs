using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;



public class LogManager : MonoBehaviour
{
    public static LogManager Instance { get; private set; }

     public static LogManager instance = null; //Static instance of LogManager which allows it to be accessed by any other script.

private string filePath;
    private List<string> dataColumns;
    private Dictionary<int, TrialData> trialDataDict = new Dictionary<int, TrialData>();
    [SerializeField] private bool m_ShowDebugLogManager;

    // Participant info
    private string participantID;
    private int participantAge;
    private string participantGender;

    private class TrialData
    {
        public int BlockNumber;
        public int TrialNumber;
        public float BlockDistance;
        public DateTime TrialStart;
        public DateTime DecisionPhaseStart;
        public DateTime? GridWorldPhaseStart;
        public DateTime? DecisionMade;
        public DateTime? CollisionTime;
        public DateTime? TrialEnd;
        public string Decision;
        public string Outcome;
        public int EffortLevel;
        public int PressesRequired;
        public bool IsPractice;

        public bool IsComplete()
        {
            return TrialEnd.HasValue && !string.IsNullOrEmpty(Decision) && !string.IsNullOrEmpty(Outcome);
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadParticipantInfo();
            InitializeCSVFile();
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void LoadParticipantInfo()
    {
        participantID = PlayerPrefs.GetString("ParticipantID", "Unknown");
        participantAge = PlayerPrefs.GetInt("ParticipantAge", 0);
        participantGender = PlayerPrefs.GetString("ParticipantGender", "Unknown");
    }

    private void InitializeCSVFile()
    {
        string strDir = Path.Combine(Application.dataPath, "_ExpData");
        Directory.CreateDirectory(strDir);
        filePath = Path.Combine(strDir, $"ExperimentData_{participantID}_{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        dataColumns = new List<string>
        {
            "ParticipantID", "ParticipantAge", "ParticipantGender", "TrialType",
            "TrialNumber", "BlockNumber", "BlockDistance", "TrialStartTime", "DecisionPhaseStartTime",
            "GridWorldPhaseStartTime", "DecisionMadeTime", "CollisionTime", "TrialEndTime",
            "DecisionReactionTime", "RewardCollectionTime", "Decision", "Outcome",
            "EffortLevel", "PressesRequired"
        };

        WriteCSVLine(dataColumns);
        if (m_ShowDebugLogManager) Debug.Log($"CSV file initialized at: {filePath}");
    }

    public void LogTrialStart(int trialNumber, int blockNumber, float blockDistance, int effortLevel, int pressesRequired, bool isPractice)
    {
        var trialData = new TrialData
        {
            TrialNumber = trialNumber,
            BlockNumber = blockNumber,
            BlockDistance = blockDistance,
            TrialStart = DateTime.Now,
            EffortLevel = effortLevel,
            PressesRequired = pressesRequired,
            IsPractice = isPractice
        };

        trialDataDict[trialNumber] = trialData;

        Debug.Log($"{(isPractice ? "Practice" : "Formal")} Trial {trialNumber} started. Effort Level: {effortLevel}, Presses Required: {pressesRequired}");
    }
    // public void LogTrialStart(int trialNumber, int blockNumber, float blockDistance, int effortLevel, int pressesRequired)
    // {
    //     if (!trialDataDict.ContainsKey(trialNumber))
    //     {
    //         trialDataDict[trialNumber] = new TrialData
    //         {
    //             TrialNumber = trialNumber + 1, // Add 1 to convert from 0-based to 1-based index for logging
    //             BlockNumber = blockNumber + 1, // Add 1 to convert from 0-based to 1-based index for logging
    //             BlockDistance = blockDistance,
    //             TrialStart = DateTime.Now,
    //             EffortLevel = effortLevel,
    //             PressesRequired = pressesRequired
    //         };
    //         Debug.Log($"New trial data created for Trial {trialNumber + 1}");
    //     }
    //     else
    //     {
    //         var trial = trialDataDict[trialNumber];
    //         trial.BlockNumber = blockNumber + 1; // Add 1 to convert from 0-based to 1-based index for logging
    //         trial.BlockDistance = blockDistance;
    //         trial.TrialStart = DateTime.Now;
    //         trial.EffortLevel = effortLevel;
    //         trial.PressesRequired = pressesRequired;
    //         Debug.Log($"Existing trial data updated for Trial {trialNumber + 1}");
    //     }
    //     Debug.Log($"Trial {trialNumber + 1} in Block {blockNumber + 1} started. Effort Level: {effortLevel}, Presses Required: {pressesRequired}");
    // }
    public void LogCheckQuestionResponse(int trialNumber, int checkQuestionNumber, string leftFruitName, string rightFruitName, string choice, bool isCorrect)
    {
        // Append check question response data to the CSV file
        var rowData = new string[]
        {
            participantID,
            participantAge.ToString(),
            participantGender,
            "CheckQuestion",
            trialNumber.ToString(),
            checkQuestionNumber.ToString(),
            leftFruitName,
            rightFruitName,
            choice,
            isCorrect.ToString()
        };

        WriteCSVLine(rowData);
    }

    public void LogScoreUpdate(int trialNumber, bool isPractice, int points, string outcome)
    {
        if (isPractice)
        {
            LogDecisionMade(trialNumber, $"Practice Score +{points}");
            LogTrialEnd(trialNumber, outcome);
        }
        else
        {
            LogDecisionMade(trialNumber, $"Score +{points}");
            LogTrialEnd(trialNumber, outcome);
        }
    }

    public void LogScoreReset(int trialNumber, bool isPractice, int oldScore, int newScore, string decision)
    {
        if (isPractice)
        {
            LogDecisionMade(trialNumber, decision);
            LogTrialEnd(trialNumber, $"Practice Score Reset from {oldScore} to {newScore}");
        }
        else
        {
            LogDecisionMade(trialNumber, decision);
            LogTrialEnd(trialNumber, $"Total Score Reset from {oldScore} to {newScore}");
        }
    }


    public void LogDecisionPhaseStart(int trialNumber)
    {
        if (trialDataDict.TryGetValue(trialNumber, out var trial))
        {
            trial.DecisionPhaseStart = DateTime.Now;
        }
    }
    public void LogGridWorldPhaseStart(int trialNumber)
    {
        if (trialDataDict.TryGetValue(trialNumber, out var trial))
        {
            trial.GridWorldPhaseStart = DateTime.Now;
        }
    }
    public void LogDecisionMade(int trialNumber, string decision)
    {
        if (trialDataDict.TryGetValue(trialNumber, out var trial))
        {
            trial.DecisionMade = DateTime.Now;
            trial.Decision = decision;
        }
    }
    public void LogCollisionTime(int trialNumber)
    {
        if (trialDataDict.TryGetValue(trialNumber, out var trial))
        {
            trial.CollisionTime = DateTime.Now;
        }
    }

    // public void LogTrialEnd(int trialNumber, string outcome)
    // {
    //     EnsureTrialDataExists(trialNumber);
    //     var trial = trialDataDict[trialNumber];
    //     trial.TrialEnd = DateTime.Now;
    //     trial.Outcome = outcome;
    //     if (m_ShowDebugLogManager) Debug.Log($"Trial {trialNumber} ended at {trial.TrialEnd} with outcome: {outcome}");

    //     if (trial.IsComplete())
    //     {
    //         WriteTrialData(trial);
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"Trial {trialNumber} data is incomplete and will not be logged.");
    //     }
    // }

    public void LogInfoMessage(string title, string message)
    {
        LogManager.Instance.LogScoreUpdate(0, false, 0, $"{title}: {message}");
    }

    public void LogMessage(string title, string message)
    {
        // Implement the logic to log the message, e.g., write to a file or the console
        Debug.Log($"{title}: {message}");
    }

    public void LogWarning(string title, string message)
    {
        // Implement the logic to log the warning, e.g., write to the console or a log file
        Debug.LogWarning($"{title}: {message}");
    }

    public void LogTrialEnd(int trialNumber, string outcome)
    {
        if (trialDataDict.TryGetValue(trialNumber, out var trial))
        {
            trial.TrialEnd = DateTime.Now;
            trial.Outcome = outcome;

            WriteTrialData(trial);

            trialDataDict.Remove(trialNumber);
            Debug.Log($"{(trial.IsPractice ? "Practice" : "Formal")} Trial {trialNumber} data logged and removed from memory. Outcome: {outcome}");
        }
        else
        {
            Debug.LogWarning($"Trial {trialNumber} data not found when ending trial.");
        }
    }
    private void WriteTrialData(TrialData trial)
    {
        TimeSpan? decisionReactionTime = trial.DecisionMade - trial.DecisionPhaseStart;
        TimeSpan? rewardCollectionTime = trial.CollisionTime - trial.GridWorldPhaseStart;

        var rowData = new string[]
        {
            participantID,
            participantAge.ToString(),
            participantGender,
            trial.IsPractice ? "Practice" : "Formal",
            trial.TrialNumber.ToString(),
            trial.BlockNumber.ToString(),
            trial.BlockDistance.ToString("F2"),
            FormatDateTime(trial.TrialStart),
            FormatDateTime(trial.DecisionPhaseStart),
            FormatDateTime(trial.GridWorldPhaseStart),
            FormatDateTime(trial.DecisionMade),
            FormatDateTime(trial.CollisionTime),
            FormatDateTime(trial.TrialEnd),
            FormatTimeSpan(decisionReactionTime),
            FormatTimeSpan(rewardCollectionTime),
            trial.Decision ?? "N/A",
            trial.Outcome ?? "N/A",
            trial.EffortLevel.ToString(),
            trial.PressesRequired.ToString()
        };

        WriteCSVLine(rowData);
    }

    // Update the WritePartialTrialData method to include participant info
    private void WritePartialTrialData(TrialData trial)
    {
        var rowData = new string[]
        {
            participantID,
            participantAge.ToString(),
            participantGender,
            trial.TrialNumber.ToString(),
            trial.BlockNumber.ToString(),
            trial.BlockDistance.ToString("F2"),
            FormatDateTime(trial.TrialStart),
            "N/A", // DecisionPhaseStartTime
            "N/A", // GridWorldPhaseStartTime
            "N/A", // DecisionMadeTime
            "N/A", // CollisionTime
            "N/A", // TrialEndTime
            "N/A", // DecisionReactionTime
            "N/A", // RewardCollectionTime
            "N/A", // Decision
            "N/A", // Outcome
            trial.EffortLevel.ToString(),
            trial.PressesRequired.ToString()
        };

        WriteCSVLine(rowData);
    }

    private string FormatDateTime(DateTime? dateTime)
    {
        return dateTime?.ToString("yyyy-MM-dd HH:mm:ss.fff") ?? "N/A";
    }

    private string FormatTimeSpan(TimeSpan? timeSpan)
    {
        return timeSpan?.TotalSeconds.ToString("F3") ?? "N/A";
    }

    private void WriteCSVLine(IEnumerable<string> data)
    {
        string line = string.Join(",", data.Select(field => field.Contains(",") ? $"\"{field}\"" : field));
        File.AppendAllText(filePath, line + Environment.NewLine);
    }

    public void LogBlockStart(int blockNumber)
    {
        Debug.Log($"Block {blockNumber} started at {DateTime.Now}");
    }

    public void LogBlockEnd(int blockNumber)
    {
        Debug.Log($"Block {blockNumber} ended at {DateTime.Now}");
    }

    public void LogExperimentStart(bool isPractice)
    {
        Debug.Log($"{(isPractice ? "Practice" : "Formal")} experiment started.");
    }

    public void LogExperimentEnd()
    {
        Debug.Log($"Experiment ended at {DateTime.Now}. Total trials completed: {trialDataDict.Count}");
        DumpUnloggedTrials();
    }

    // public void LogPracticeTrialStart(int trialNumber, int effortLevel, int pressesRequired)
    // {
    //     practiceTrialDataDict[trialNumber] = new TrialData
    //     {
    //         TrialNumber = trialNumber,
    //         BlockNumber = 0,
    //         BlockDistance = 3,
    //         TrialStart = DateTime.Now,
    //         EffortLevel = effortLevel,
    //         PressesRequired = pressesRequired
    //     };
    //     Debug.Log($"Practice Trial {trialNumber} started. Effort Level: {effortLevel}, Presses Required: {pressesRequired}");
    // }

    // public void LogPracticeDecisionMade(int trialNumber, string decision, int effortLevel, int pressesRequired)
    // {
    //     if (practiceTrialDataDict.TryGetValue(trialNumber, out var trial))
    //     {
    //         trial.DecisionMade = DateTime.Now;
    //         trial.Decision = decision;
    //         trial.EffortLevel = effortLevel;
    //         trial.PressesRequired = pressesRequired;
    //     }
    // }
    // public void LogPracticeTrialEnd(int trialNumber, string outcome)
    // {
    //     if (practiceTrialDataDict.TryGetValue(trialNumber, out var trial))
    //     {
    //         trial.TrialEnd = DateTime.Now;
    //         trial.Outcome = outcome;
    //         WritePracticeTrialData(trial);
    //         practiceTrialDataDict.Remove(trialNumber);
    //     }
    // }

    // public void LogFormalTrialStart(int trialNumber, int blockNumber, float blockDistance, int effortLevel, int pressesRequired)
    // {
    //     formalTrialDataDict[trialNumber] = new TrialData
    //     {
    //         TrialNumber = trialNumber,
    //         BlockNumber = blockNumber,
    //         BlockDistance = blockDistance,
    //         TrialStart = DateTime.Now,
    //         EffortLevel = effortLevel,
    //         PressesRequired = pressesRequired
    //     };
    //     Debug.Log($"Formal Trial {trialNumber} in Block {blockNumber} started. Effort Level: {effortLevel}, Presses Required: {pressesRequired}");
    // }

    // Add a method to dump all trial data for debugging purposes
    public void DumpUnloggedTrials()
    {
        foreach (var trial in trialDataDict.Values)
        {
            WriteTrialData(trial);
        }
        trialDataDict.Clear();
    }
}