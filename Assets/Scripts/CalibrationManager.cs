using UnityEngine;
using UnityEngine.UI;

public class CalibrationManager : MonoBehaviour
{
    public float calibrationTime = 5f;
    public Text timerText;
    public Text instructionsText;
    public PlayerController playerPrefab;
    public GameObject rewardPrefab;
    public float rewardDistance = 10f;

    private PlayerController player;
    private GameObject reward;
    private float timer;
    private Vector3 totalMovement;

    private void Start()
    {
        SpawnPlayerAndReward();
        timer = calibrationTime;
        instructionsText.text = "Use arrow keys to move towards the reward as fast as possible within 5 seconds.";
    }

    private void SpawnPlayerAndReward()
    {
        player = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
        reward = Instantiate(rewardPrefab, new Vector3(rewardDistance, 0, 0), Quaternion.identity);
    }

    private void Update()
    {
        timer -= Time.deltaTime;
        timerText.text = $"Time: {timer:F2}";

        totalMovement += new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0) * Time.deltaTime;

        if (timer <= 0 || player.ReachedTarget(reward.transform.position))
        {
            EndCalibration();
        }
    }

    private void EndCalibration()
    {
        GameManager.Instance.calibrationDistance = totalMovement.magnitude;
        GameManager.Instance.LoadNextScene();
    }

    
}