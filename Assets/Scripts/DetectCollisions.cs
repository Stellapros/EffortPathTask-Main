using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles collision detection for the player, particularly with rewards.
/// </summary>
public class DetectCollisions : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private GridWorldManager gridWorldManager;
    private PlayerController playerController;
    private RewardSpawner rewardSpawner;
    private bool hasCollectedReward = false;

    [Header("Celebration")]
    [SerializeField] private Text celebratoryMessage;
    [SerializeField] private float celebrationDuration = 2f;
    [SerializeField] private ParticleSystem celebrationParticles;
    private Coroutine celebrationCoroutine;

    private void Start()
    {
        // Find necessary components if not assigned
        if (gameController == null)
            gameController = FindAnyObjectByType<GameController>();

        if (gridWorldManager == null)
            gridWorldManager = FindAnyObjectByType<GridWorldManager>();

        rewardSpawner = FindAnyObjectByType<RewardSpawner>(); // Find RewardSpawner
        if (rewardSpawner == null)
            Debug.LogWarning("RewardSpawner not found in the scene!");

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogError("PlayerController not found on this GameObject!");

        // Log warnings for missing components
        if (gameController == null)
            Debug.LogWarning("GameController not found in the scene!");
        if (gridWorldManager == null)
            Debug.LogWarning("GridWorldManager not found in the scene!");

        // Make sure celebratory message is empty at start
        if (celebratoryMessage != null)
            celebratoryMessage.text = "";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!hasCollectedReward && other.CompareTag("Reward") && other.TryGetComponent<Reward>(out var reward))
        {
            HandleRewardCollision(reward, other.gameObject);
        }
    }

    /// <summary>
    /// Handles the collision with a reward object.
    /// </summary>
    /// <param name="reward">The Reward component of the collided object.</param>
    /// <param name="rewardObject">The GameObject of the reward.</param>
    private void HandleRewardCollision(Reward reward, GameObject rewardObject)
    {
        hasCollectedReward = true;
        gameController.RewardCollected(true);

        // Start celebration
        ShowCelebration();

        // Log the reward collection
        if (LogManager.Instance != null)
        {
            LogManager.Instance.LogEvent("RewardCollected", new Dictionary<string, string>
            {
                {"RewardCollected", "true"},
                {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}
            });
        }

        // Use RewardSpawner to clear the reward properly
        if (rewardSpawner != null)
        {
            rewardSpawner.ClearReward();
        }
        else
        {
            Debug.LogWarning("RewardSpawner not found, destroying reward directly");
            Destroy(rewardObject);
        }
    }

    /// <summary>
    /// Shows celebration message for a limited time
    /// </summary>
    private void ShowCelebration()
    {
        if (celebratoryMessage != null)
        {
            // Stop any existing celebration
            if (celebrationCoroutine != null)
                StopCoroutine(celebrationCoroutine);

            // Start new celebration
            celebrationCoroutine = StartCoroutine(CelebrationRoutine());
        }

        // Play particle effects if assigned
        if (celebrationParticles != null)
        {
            celebrationParticles.Play();
        }

        // Play audio if you have an audio source
        if (TryGetComponent<AudioSource>(out var audioSource) && audioSource.clip != null)
        {
            audioSource.Play();
        }
    }

    private IEnumerator CelebrationRoutine()
    {
        celebratoryMessage.fontSize = 36;
        celebratoryMessage.color = Color.cyan;
        celebratoryMessage.text = "Well done!";

        // Optional: add animation or visual effects here

        yield return new WaitForSeconds(celebrationDuration);

        celebratoryMessage.text = "";
        celebrationCoroutine = null;
    }
}



