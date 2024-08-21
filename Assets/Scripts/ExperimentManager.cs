using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;


// Define the Trial class
public class Trial
{
    /// <summary>
    /// Create class Trial that holds the effort level for each trial
    /// TO DO: Add other trial variables here: Player reset position, Target reset position, Reward value 
    /// </summary>
    /// varEV is the variable that holds the effort level (1,2,3) for each trial
    public float varEV;
    public Vector2 varPosPlayer;

    public Trial(float varEV_, Vector2 varPosPlayer_)
    {
        varEV = varEV_;
        varPosPlayer = varPosPlayer_;
    }
}

public interface IExperimentManager
{
    Vector2 GetCurrentTrialPlayerPosition();
    void EndTrial(bool completed);
    void LoadDecisionScene();
}

// Define the IListExtensions class
public static class IListExtensions
{
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}


public class ExperimentManager : MonoBehaviour, IExperimentManager
{
    // Original variables
    private float startTime;
    private int trialNum = 0;
    public bool trialRunning = false;
    public int totalKeyPresses;
    public int pressesRequired;
    private float trialDuration = 5f;
    private float lowEffort = 0.3f;
    private float mediumEffort = 0.5f;
    private float highEffort = 0.9f;

    List<Trial> lstEVs;

    // New variables for sprite management
    public List<Sprite> effortSprites = new List<Sprite>();
    private Dictionary<float, Sprite> effortToSpriteMap = new Dictionary<float, Sprite>();
    [SerializeField] private PlayerSpawner playerSpawner;


    private void Awake()
    {
        // Ensure this object persists across scene loads
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        InitializeTrials();
        ShuffleEffortSprites();
        LoadDecisionScene();
    }

    public void StartExperiment()
    {
        trialNum = 0;
        LoadDecisionScene();
    }


    public void StartExperimentScene()
    {
        SceneManager.LoadScene("ExperimentScene");
    }

    public void EndExperiment()
    {
        Debug.Log("Experiment completed!");
        // 加载结束实验场景或执行其他逻辑
        SceneManager.LoadScene("DecisionScene");
    }

    // TRIAL GENERATOR
    void InitializeTrials()
    {
        if (playerSpawner == null)
        {
            Debug.LogError("PlayerSpawner is not set in ExperimentManager!");
            return;
        }

        lstEVs = new List<Trial>();
        for (int i2 = 0; i2 < 4; i2++)
        {
            for (int ii = 0; ii < 3; ii++)
            {
                // This method creates a list of trials, each with a random spawn position for the player
                Vector2 spawnPosition = playerSpawner.GetRandomSpawnPosition();
                lstEVs.Add(new Trial(ii + 1, spawnPosition));
            }
        }
        lstEVs.Shuffle();
    }

    // New method to shuffle and assign effort sprites
    void ShuffleEffortSprites()
    {
        if (effortSprites.Count != 3)
        {
            Debug.LogError("Please assign exactly 3 effort sprites in the inspector!");
            return;
        }

        effortSprites = effortSprites.OrderBy(x => Random.value).ToList();
        effortToSpriteMap[1] = effortSprites[0];
        effortToSpriteMap[2] = effortSprites[1];
        effortToSpriteMap[3] = effortSprites[2];

        Debug.Log("Effort sprites shuffled and assigned.");
    }

    // Method to start a trial (now loads ExperimentScene)
    public void StartTrial()
    {
        trialRunning = true;
        startTime = Time.time;
        SceneManager.LoadScene("ExperimentScene");
    }

    // Method to skip a trial
    public void SkipTrial()
    {
        EndTrial(false);
    }

    // Method to handle trial ending
    public event System.Action<bool> OnTrialEnded;
    public void EndTrial(bool completed)
    {
        trialRunning = false;

        string choice = completed ? "Y" : "N";
        //Add to the data file, the current trial number, its effort level, participants choice (Y/N)
        LogManager.instance.WriteTimeStampedEntry($"{trialNum};{lstEVs[trialNum].varEV};{choice}");

        //Increase the trial count
        trialNum++;

        // Notify subscribers about the trial ending
        OnTrialEnded?.Invoke(completed);

        LoadDecisionScene();
    }


    // New method to load the Decision scene
    public void LoadDecisionScene()
    {
        if (trialNum >= lstEVs.Count)
        {
            Debug.Log("Experiment completed!");
            // TODO: Load end experiment scene or do something else
            return;
        }

        SceneManager.LoadScene("DecisionScene");
    }

    // Method to get the current trial's effort sprite
    public Sprite GetCurrentTrialSprite()
    {
        return effortToSpriteMap[lstEVs[trialNum].varEV];
    }

    // Method to get the current trial's effort value
    public float GetCurrentTrialEV()
    {
        return lstEVs[trialNum].varEV;
    }

    // Method to get the current trial's player position
    public Vector2 GetCurrentTrialPlayerPosition()
    {
        return lstEVs[trialNum].varPosPlayer;
    }


    // Method to check if trial time is up
    public bool IsTrialTimeUp()
    {
        return Time.time - startTime > trialDuration;
    }

    // Original method to calculate required presses (kept for reference)
    int CalculatePressesRequired(float effortLevel)
    {
        if (effortLevel == 1) return (int)(lowEffort * totalKeyPresses);
        if (effortLevel == 2) return (int)(mediumEffort * totalKeyPresses);
        if (effortLevel == 3) return (int)(highEffort * totalKeyPresses);
        return 0;
    }

}
