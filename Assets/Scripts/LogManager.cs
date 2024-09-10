using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class LogManager : MonoBehaviour
{
    public static LogManager instance = null; //Static instance of LogManager which allows it to be accessed by any other script.
    private string filePath;
    [SerializeField] private bool m_ShowDebugLogManager;
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
            InitializeLogFile();
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

    private void InitializeLogFile()
    {
        string strDir = Path.Combine(Application.dataPath, "_ExpData");
        filePath = Path.Combine(strDir, System.DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_data.txt");
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
}
