using UnityEngine;
using UnityEngine.UI;

public class PracticeManager : MonoBehaviour
{
    public PlayerController playerPrefab;
    public GameObject rewardPrefab;
    public Text instructionsText;
    public Text trialCounterText;

    private PlayerController player;
    private GameObject reward;
    private int currentTrial = 0;
    private float[] speedLevels = { 0.3f, 0.6f, 0.9f };

    // private void Start()
    // {
    //     SpawnPlayerAndReward();
    //     SetupTrial();
    // }

    // private void SpawnPlayerAndReward()
    // {
    //     player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
    //     reward = Instantiate(rewardPrefab, new Vector3(GameManager.Instance.calibrationDistance, 0, 0), Quaternion.identity);
    // }

    // private void SetupTrial()
    // {
    //     if (currentTrial < 3)
    //     {
    //         player.SetSpeedLevel(speedLevels[currentTrial]);
    //         player.transform.position = Vector3.zero;

    //         instructionsText.text = $"Practice Trial {currentTrial + 1}: Use arrow keys to move towards the reward.";
    //         trialCounterText.text = $"Trial: {currentTrial + 1}/3";
    //     }
    //     else
    //     {
    //         GameManager.Instance.LoadNextScene();
    //     }
    // }

    // private void Update()
    // {
    //     if (player.ReachedTarget(reward.transform.position))
    //     {
    //         currentTrial++;
    //         SetupTrial();
    //     }
    // }
}