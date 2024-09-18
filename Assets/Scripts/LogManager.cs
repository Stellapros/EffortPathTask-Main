using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System;
using System.Text;


/// <summary>
/// How to use logging methods 
/// </summary>
/// logManager.LogExperimentStart();
/// logManager.LogTrialInfo(trialNumber, blockIndex, blockOrder, effortLevel, decision, reactionTime);
/// logManager.LogTrialOutcome(trialNumber, rewardCollected, completionTime);
/// logManager.LogBlockStart(blockIndex, blockOrder);
/// logManager.LogBlockEnd(blockIndex, blockOrder);
/// logManager.LogExperimentEnd();

public class LogManager : MonoBehaviour
{
    public static LogManager instance = null; //Static instance of LogManager which allows it to be accessed by any other script.
    private string filePath;
    private List<string[]> dataRows = new List<string[]>();
    [SerializeField] private bool m_ShowDebugLogManager;
    private ExperimentManager experimentManager;
    public static class LogManagerHelper
    {
        public static void Log(string message)
        {
            if (LogManager.instance != null)
            {
                LogManager.instance.WriteTimeStampedEntry(message);
            }
            else
            {
                Debug.LogWarning($"LogManager instance is null. Cannot log message: {message}");
            }
        }
    }

    //Awake is always called before any Start functions
    void Awake()
    {
        Debug.Log("LogManager Awake called");

        if (m_ShowDebugLogManager)
        {
            print("LogManager::Awake");
        }
        if (instance == null)
        {
            //if not, set instance to this
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeCSVFile();
        }

        //If instance already exists and it's not this:
        else if (instance != this)
        {
            //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);

        //string
        //string strDir = Path.Combine(Application.streamingAssetsPath);
        string strDir = Path.Combine(Application.dataPath, "_ExpData");
        //string strDir = Application.persistentDataPath;
        filePath = Path.Combine(strDir, System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_data.txt");

        /* opening the writer here causes obscure problems (file access permission) errors when Unity loses focus.
            sadly, we therefore have to re-open the file every time we want to write 
        writer = new StreamWriter(filePath, true);
        */

    }
    public void LogPlayerMovement(Vector2 initialPosition, Vector2 finalPosition, float distanceMoved)
    {
        string movementData = $"Initial Position: {initialPosition}, Final Position: {finalPosition}, Distance Moved: {distanceMoved}";
        Debug.Log($"Player Movement: {movementData}");

        // Add this data to your experiment log or database
        // For example:
        // experimentLog.Add(new ExperimentLogEntry(currentBlockIndex, currentTrialIndex, "PlayerMovement", movementData));
    }

    private void InitializeCSVFile()
    {
        string strDir = Path.Combine(Application.dataPath, "_ExpData");
        if (!Directory.Exists(strDir))
        {
            Directory.CreateDirectory(strDir);
        }
        filePath = Path.Combine(strDir, $"ExperimentData_{DateTime.Now:yyyyMMdd-HHmmss}.csv");

        string[] header = new string[]
        {
            "Timestamp", "EventType", "TrialNumber", "BlockIndex", "BlockOrder",
            "EffortLevel", "Decision", "ReactionTime", "Outcome", "CompletionTime"
        };
        dataRows.Add(header);
        Debug.Log($"CSV file initialized at: {filePath}");
    }

    public void WriteTimeStampedEntry(string strMessage)
    {
        Debug.Log($"[{System.DateTime.Now}] {strMessage}");

        if (m_ShowDebugLogManager)
        {
            print("LogManager::writeEntry");
        }
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine(Time.realtimeSinceStartup + ";" + strMessage);
        }
    }

    public void WriteEntry(string strMessage)
    {
        if (m_ShowDebugLogManager)
        {
            print("LogManager::writeEntry");
        }
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine(strMessage);
        }
    }

    public void WriteEntry(List<string> data)
    {
        if (m_ShowDebugLogManager)
        {
            print("LogManager::writeEntry");
        }
        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            data.ForEach(delegate (string s)
            {
                sw.WriteLine(s);
            });
        }
    }


    public void WriteCSV(string[] header, IList<string[]> data)
    {
        if (m_ShowDebugLogManager)
        {
            print("LogManager::WriteCSV");
        }

        using (StreamWriter sw = new StreamWriter(filePath, true))
        {
            sw.WriteLine(string.Join(",", header));
            for (int i = 0; i < data.Count; i++)
            {
                sw.WriteLine(string.Join(",", data[i]));
            }
        }
    }

    //     public void LogTrialInfo(int trialNumber, int blockIndex, int blockOrder, int effortLevel, bool decision, float reactionTime)
    // {
    //     string logEntry = $"Trial:{trialNumber},BlockIndex:{blockIndex},BlockOrder:{blockOrder},EffortLevel:{effortLevel},Decision:{(decision ? "Work" : "Skip")},ReactionTime:{reactionTime:F2}";
    //     WriteTimeStampedEntry(logEntry);
    // // }
    // public void LogTrialInfo(int trialNumber, int blockIndex, int blockOrder, int effortLevel, bool decision, float reactionTime)
    // {
    //     LogEvent("TrialDecision", trialNumber, blockIndex, blockOrder, effortLevel, decision ? "Work" : "Skip", reactionTime);
    //     Debug.Log($"Logged trial info: Trial {trialNumber}, Block {blockIndex}, Decision {(decision ? "Work" : "Skip")}");
    // }
    public void LogTrialInfo(int trialNumber, int blockIndex, int blockOrder, int effortLevel, bool decision, float reactionTime)
    {
        LogEvent("TrialDecision", trialNumber, blockIndex, blockOrder, effortLevel, decision ? "Work" : "Skip", reactionTime);
        Debug.Log($"Logged trial info: Trial {trialNumber}, Block {blockIndex}, BlockOrder {blockOrder}, EffortLevel {effortLevel}, Decision {(decision ? "Work" : "Skip")}, ReactionTime {reactionTime:F2}");
    }


    public void LogTrialOutcome(int trialNumber, bool rewardCollected, float completionTime)
    {
        LogEvent("TrialOutcome", trialNumber, outcome: rewardCollected ? "Completed" : "TimedOut", completionTime: completionTime);
    }

    public void LogBlockStart(int blockIndex, int blockOrder)
    {
        LogEvent("BlockStart", blockIndex: blockIndex, blockOrder: blockOrder);
    }

    public void LogBlockEnd(int blockIndex, int blockOrder)
    {
        LogEvent("BlockEnd", blockIndex: blockIndex, blockOrder: blockOrder);
    }

    public void LogExperimentStart()
    {
        LogEvent("ExperimentStart");
    }

    public void LogExperimentEnd()
    {
        LogEvent("ExperimentEnd");
        WriteCSVFile();
    }

    // private void LogEvent(string eventType, int trialNumber = 0, int blockIndex = 0, int blockOrder = 0,
    //                       int effortLevel = 0, string decision = "", float reactionTime = 0f,
    //                       string outcome = "", float completionTime = 0f)
    // {
    //     string[] rowData = new string[]
    //     {
    //         DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
    //         eventType,
    //         trialNumber > 0 ? trialNumber.ToString() : "",
    //         blockIndex > 0 ? blockIndex.ToString() : "",
    //         blockOrder > 0 ? blockOrder.ToString() : "",
    //         effortLevel > 0 ? effortLevel.ToString() : "",
    //         decision,
    //         reactionTime > 0 ? reactionTime.ToString("F3") : "",
    //         outcome,
    //         completionTime > 0 ? completionTime.ToString("F3") : ""
    //     };
    //     dataRows.Add(rowData);

    //     if (m_ShowDebugLogManager)
    //     {
    //         Debug.Log($"Logged event: {string.Join(", ", rowData)}");
    //     }
    // }

    public void LogBlockStructure(List<(int trialNumber, int blockIndex, int blockOrder, int effortLevel)> trialInfo)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Block Structure:");
        foreach (var trial in trialInfo)
        {
            sb.AppendLine($"Trial {trial.trialNumber}: BlockIndex {trial.blockIndex}, BlockOrder {trial.blockOrder}, EffortLevel {trial.effortLevel}");
        }
        Debug.Log(sb.ToString());
        WriteEntry(sb.ToString());
    }
    private void LogEvent(string eventType, int trialNumber = 0, int blockIndex = 0, int blockOrder = 0,
                          int effortLevel = 0, string decision = "", float reactionTime = 0f,
                          string outcome = "", float completionTime = 0f)
    {
        string[] rowData = new string[]
        {
        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
        eventType,
        trialNumber.ToString(),  // Always log the trial number
        blockIndex.ToString(),   // Always log the block index
        blockOrder.ToString(),   // Always log the block order
        effortLevel.ToString(),  // Always log the effort level
        decision,
        reactionTime > 0 ? reactionTime.ToString("F3") : "",
        outcome,
        completionTime > 0 ? completionTime.ToString("F3") : ""
        };
        dataRows.Add(rowData);

        Debug.Log($"Logged event: {string.Join(", ", rowData)}");
    }


    private void WriteCSVFile()
    {
        StringBuilder csv = new StringBuilder();
        foreach (string[] row in dataRows)
        {
            csv.AppendLine(string.Join(",", row));
        }
        File.WriteAllText(filePath, csv.ToString());
        Debug.Log($"CSV file written to: {filePath}");
    }

    private void OnApplicationQuit()
    {
        WriteCSVFile();
    }
}
