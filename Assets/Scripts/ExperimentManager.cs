using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class ExperimentManager : MonoBehaviour, IExperimentManager
{
    // Define the Trial class
    public class Trial
    {
        /// <summary>
        /// Create class Trial that holds the effort level for each trial
        /// TO DO: Add other trial variables here: Player reset position, Target reset position, Reward value 
        /// </summary>
        /// varEV is the variable that holds the effort level (1,2,3) for each trial
        public float EffortLevel { get; }
        public Vector2 PlayerPosition { get; }
        public Vector2 RewardPosition { get; } // Add this

        public Trial(float effortLevel, Vector2 playerPosition, Vector2 rewardPosition)
        {
            EffortLevel = effortLevel;
            PlayerPosition = playerPosition;
            RewardPosition = rewardPosition;
        }
    }

    private List<Trial> trials;
    private Dictionary<float, Sprite> effortToSpriteMap;
    private int currentTrialIndex = 0;
    private float startTime;
    private bool isTrialRunning = false;

    [SerializeField] private PlayerSpawner playerSpawner;
    [SerializeField] private RewardSpawner rewardSpawner;
    [SerializeField] private List<Sprite> effortSprites;

    private void Awake()
    {
        // Ensure this object persists across scene loads
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeTrials();
        ShuffleEffortSprites();
        // LoadDecisionScene();
    }

    // TRIAL GENERATOR
    // Initialize trials with random positions
    private void InitializeTrials()
    {
        if (playerSpawner == null)
        {
            Debug.LogError("PlayerSpawner is not set in ExperimentManager!");
            return;
        }

        trials = new List<Trial>();
        for (int i = 0; i < 12; i++)
        {
            Vector2 playerSpawnPosition = playerSpawner.GetRandomSpawnPosition();
            Vector2 rewardPosition = new Vector2(Random.Range(-8f, 8f), Random.Range(-4f, 4f)); // Generate random reward position
            trials.Add(new Trial(i % 3 + 1, playerSpawnPosition, rewardPosition)); // Pass the reward position
        }
        trials.Shuffle();
    }

    // New method to shuffle and assign effort sprites
    private void ShuffleEffortSprites()
    {
        if (effortSprites.Count != 3)
        {
            Debug.LogError("Please assign exactly 3 effort sprites in the inspector!");
            return;
        }

        effortSprites = effortSprites.OrderBy(x => Random.value).ToList();
        effortToSpriteMap = new Dictionary<float, Sprite>
        {
            { 1, effortSprites[0] },
            { 2, effortSprites[1] },
            { 3, effortSprites[2] }
        };

        Debug.Log("Effort sprites shuffled and assigned.");
    }

    // Start a new trial
    public void StartTrial()
    {
        isTrialRunning = true;
        startTime = Time.time;

        // Add a callback for when the scene is loaded
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Load the GridWorld scene instead of ExperimentScene
        SceneManager.LoadScene("GridWorld");
    }

    // Skip the current trial
    public void SkipTrial()
    {
        EndTrial(false);
    }

    // End the current trial
    public void EndTrial(bool completed)
    {
        isTrialRunning = false;
        LogManager.instance.WriteTimeStampedEntry($"{currentTrialIndex};{trials[currentTrialIndex].EffortLevel};{(completed ? "Y" : "N")}");
        currentTrialIndex++;
        OnTrialEnded?.Invoke(completed);
        LoadDecisionScene();
    }

    public event System.Action<bool> OnTrialEnded;

    // Get the sprite for the current trial's effort level
    public Sprite GetCurrentTrialSprite()
    {
        return effortToSpriteMap[trials[currentTrialIndex].EffortLevel];
    }

    // Get the effort value for the current trial
    public float GetCurrentTrialEV()
    {
        return trials[currentTrialIndex].EffortLevel;
    }

    // Get the player position for the current trial
    public Vector2 GetCurrentTrialPlayerPosition()
    {
        return trials[currentTrialIndex].PlayerPosition;
    }

    // Get the reward position for the current trial
    public Vector2 GetCurrentTrialRewardPosition()
    {
        return trials[currentTrialIndex].RewardPosition;
    }

    // Get the reward value for the current trial
    public float GetCurrentTrialRewardValue()
    {
        return 10f; // Assuming 10 points per reward
    }

    // Check if the trial time is up
    public bool IsTrialTimeUp()
    {
        return Time.time - startTime > 5f;
    }

    // Load the decision scene
    public void LoadDecisionScene()
    {
        if (currentTrialIndex >= trials.Count)
        {
            Debug.Log("Experiment completed!");
            // TODO: Load end experiment scene or do something else
            return;
        }

        SceneManager.LoadScene("DecisionPhase");
    }

    // Handle scene loading
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "GridWorld")
        {
            GameController gameController = FindObjectOfType<GameController>();
            if (gameController != null)
            {
                SpawnPlayerAndReward();
                gameController.StartTrial();
            }
            else
            {
                Debug.LogError("GameController not found in GridWorld scene!");
            }
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Spawn player and reward for the current trial
    private void SpawnPlayerAndReward()
    {
        if (playerSpawner == null || rewardSpawner == null)
        {
            Debug.LogError("PlayerSpawner or RewardSpawner is not set in ExperimentManager!");
            return;
        }

        Vector2 playerPosition = GetCurrentTrialPlayerPosition();
        Vector2 rewardPosition = GetCurrentTrialRewardPosition();

        playerSpawner.SpawnPlayer(playerPosition);
        rewardSpawner.SpawnReward(currentTrialIndex, 0, (int)GetCurrentTrialEV());

        Debug.Log($"Player spawned at: {playerPosition}, Reward spawned at: {rewardPosition}");
    }
}