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

    //Awake is always called before any Start functions
    void Awake()
    {
        if (m_ShowDebugLogManager)
        {
            print("LogManager::Awake");
        }
        if (instance == null)
        {
            //if not, set instance to this
            instance = this;
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



    public void WriteTimeStampedEntry(string strMessage)
    {
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
