using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private RewardSpawner rewardSpawner;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private int rewardValue = 1; // Add this field to specify the reward value
    [SerializeField] private float maxTrialDuration = 10f;
    [SerializeField] private float restDuration = 3f;
    [SerializeField] private int[] pressesPerStep = { 3, 2, 1 };
    [SerializeField] private int trialsPerBlock = 20;

    private GameObject currentPlayer;
    private GameObject currentReward;



    private int currentBlockIndex = 0;
    private int currentTrialInBlock = 0;
    private bool rewardCollected = false;

    // Reference to the ExperimentManager



    private readonly IExperimentManager _experimentManager;

    public GameController(IExperimentManager experimentManager)
    {
        _experimentManager = experimentManager;
    }


    private void Start()
    {

        // Find the ExperimentManager in the scene
        // experimentManager = FindObjectOfType<ExperimentManager>();
        // experimentManager = (IExperimentManager)experimentManagerObject;

        if (_experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found!");
            return;
        }

        _experimentManager.OnTrialEnded += OnTrialEnd;
        StartCoroutine(RunExperiment());

        if (playerSpawner != null)
        {
            Vector2 initialPosition = Vector2.zero;

            GameObject player = playerSpawner.SpawnPlayer(initialPosition);
            if (player != null)
            {
                Debug.Log("Player successfully spawned!");
            }
            else
            {
                Debug.LogError("Failed to spawn player.");
            }
        }
        else
        {
            Debug.LogError("PlayerSpawner not assigned to GameManager!");
        }
    }




    private IEnumerator RunExperiment()
    {
        while (currentBlockIndex < pressesPerStep.Length)
        {
            for (currentTrialInBlock = 0; currentTrialInBlock < trialsPerBlock; currentTrialInBlock++)
            {
                yield return StartCoroutine(RunTrial());
                yield return new WaitForSeconds(restDuration);
                currentTrialInBlock++; // Increment counter after trial ends
            }
            currentBlockIndex++;
        }

        // Experiment completed, return to decision scene
        _experimentManager.EndTrial(true);
        _experimentManager.LoadDecisionScene();

    }

    private IEnumerator RunTrial()
    {
        // Spawn player
        Vector2 playerSpawnPosition = _experimentManager.GetCurrentTrialPlayerPosition();
        currentPlayer = playerSpawner.SpawnPlayer(playerSpawnPosition);
        if (currentPlayer != null)
        {
            playerController = currentPlayer.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("PlayerController component not found on spawned player!");
                yield break;
            }
        }
        else
        {
            Debug.LogError("Failed to spawn player!");
            yield break;
        }

        // Spawn reward
        int currentPressesRequired = pressesPerStep[currentBlockIndex];
        GameObject currentReward = rewardSpawner.SpawnReward(currentBlockIndex, currentTrialInBlock, currentPressesRequired);

        // Set up player movement
        playerController.SetPressesPerStep(currentPressesRequired);
        playerController.EnableMovement();
        playerController.StartTrial();

        countdownTimer.ResetTimer();
        countdownTimer.StartTimer();
        rewardCollected = false;

        // Wait for trial to end
        yield return new WaitUntil(() => rewardCollected || countdownTimer.TimeLeft <= 0);

        EndTrial();
    }

    private void OnRewardCollected()
    {
        rewardCollected = true;
        scoreManager.IncreaseScore(rewardValue);
    }

    private void EndTrial()
    {
        if (playerController != null)
        {
            playerController.DisableMovement();
            playerController.EndTrial();
            playerController.OnRewardCollected -= OnRewardCollected;
        }

        // Clear player
        if (currentPlayer != null)
        {
            playerSpawner.DespawnPlayer(currentPlayer);
            currentPlayer = null;
            playerController = null;
        }

        // Clear reward
        if (currentReward != null)
        {
            rewardSpawner.ClearReward();
            currentReward = null;
        }

        countdownTimer.ResetTimer();

        // Log trial data
        string trialOutcome = rewardCollected ? "Reward Collected" : "Time Out";
        Debug.Log($"Trial ended - Block:{currentBlockIndex};Trial:{currentTrialInBlock};Outcome:{trialOutcome}");

        // Inform experiment manager
        _experimentManager.EndTrial(rewardCollected);
    }


    private void OnTrialEnd(bool rewardCollected)
    {
        EndTrial();
    }


    public void RewardCollected()
    {
        rewardCollected = true;
        scoreManager.IncreaseScore(rewardValue); // Pass the reward value to IncreaseScore
    }

    // New method to initialize the game state
    public void InitializeGameState()
    {
        // Reset all relevant game state variables
        currentBlockIndex = 0;
        currentTrialInBlock = 0;
        rewardCollected = false;
        scoreManager.ResetScore(); // Assuming you have a method to reset the score

        // Initialize player position
        Vector2 playerPosition = _experimentManager.GetCurrentTrialPlayerPosition();
        playerController.ResetPosition(playerPosition);
    }

    public void LoadDecisionScene()
    {
        SceneManager.LoadScene("DecisionScene");
    }
}