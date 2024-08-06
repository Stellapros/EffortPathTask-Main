using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class MainExperimentManager : MonoBehaviour
{
    public PlayerController playerPrefab;
    public GameObject rewardPrefab;
    public Text instructionsText;
    public Text trialCounterText;
    public Text conditionText;

    private PlayerController player;
    private GameObject reward;
    private int currentTrial = 0;
    private int currentCondition = 0;
    private float[] speedLevels = { 0.3f, 0.6f, 0.9f };
    private string[] conditionNames = { "Low", "Medium", "High" };
    private float trialStartTime;
    private Vector3 startPosition;
    private Vector3 totalMovement;

    private void Start()
    {
        GameManager.Instance.mainPhaseStartTime = Time.time;
        SpawnPlayerAndReward();
        SetupTrial();
    }

    private void SpawnPlayerAndReward()
    {
        player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        reward = Instantiate(rewardPrefab, new Vector3(GameManager.Instance.calibrationDistance, 0, 0), Quaternion.identity);
    }

    private void SetupTrial()
    {
        if (currentTrial < 20)
        {
            player.SetSpeedLevel(speedLevels[currentCondition]);
            player.transform.position = Vector3.zero;
            startPosition = player.transform.position;
            totalMovement = Vector3.zero;

            instructionsText.text = "Use arrow keys to move towards the reward.";
            trialCounterText.text = $"Trial: {currentTrial + 1}/20";
            conditionText.text = $"Condition: {conditionNames[currentCondition]} Effort";

            trialStartTime = Time.time;
        }
        else if (currentCondition < 2)
        {
            currentCondition++;
            currentTrial = 0;
            SetupTrial();
        }
        else
        {
            EndExperiment();
        }
    }

    private void Update()
    {
        if (player.ReachedTarget(reward.transform.position))
        {
            EndTrial();
        }
        else
        {
            totalMovement += player.transform.position - startPosition;
            startPosition = player.transform.position;
        }
    }

    private void EndTrial()
    {
        float trialDuration = Time.time - trialStartTime;
        RecordTrialData(trialDuration);
        currentTrial++;
        SetupTrial();
    }

    private void RecordTrialData(float trialDuration)
    {
        string data = $"{PlayerPrefs.GetString("ID")},{currentCondition},{currentTrial},{totalMovement.magnitude},{trialDuration:F2}";
        
        string filePath = Path.Combine(Application.persistentDataPath, "experiment_data.csv");
        File.AppendAllText(filePath, data + "\n");
    }

    private void EndExperiment()
    {
        GameManager.Instance.experimentEndTime = Time.time;
        GameManager.Instance.LoadNextScene();
    }
}