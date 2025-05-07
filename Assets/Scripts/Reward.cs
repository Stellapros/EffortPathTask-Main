using UnityEngine;

/// <summary>
/// Represents a reward in the GridWorld scene.
/// </summary>
public class Reward : MonoBehaviour
{
    /// <summary>
    /// The value of the reward, which is added to the player's score when collected.
    /// </summary>

    [SerializeField] private int scoreValue;
    [SerializeField] private int pressesRequired;
    [SerializeField] private AudioClip rewardAppearSound;

    private int blockIndex;
    private int trialIndex;
    private AudioSource audioSource;
    private Renderer rewardRenderer;

    private void Awake()
    {
        // Get or add an AudioSource component
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Get the renderer
        rewardRenderer = GetComponent<Renderer>();
    }

    private void OnEnable()
    {
        // Ensure the AudioSource is enabled
        if (audioSource != null)
        {
            audioSource.enabled = true;
        }

        // Start with the renderer disabled
        if (rewardRenderer != null)
        {
            rewardRenderer.enabled = false;
        }

        // Show the reward and play sound on the next frame
        StartCoroutine(ShowWithSound());
    }

    private System.Collections.IEnumerator ShowWithSound()
    {
        // Wait for the end of the frame
        yield return new WaitForEndOfFrame();

        // Enable the renderer and play sound simultaneously
        if (rewardRenderer != null)
        {
            rewardRenderer.enabled = true;
        }

        PlayRewardAppearSound();
    }

    public void SetRewardParameters(int block, int trial, int pressesRequired, int value)
    {
        blockIndex = block;
        trialIndex = trial;
        this.pressesRequired = pressesRequired;
        scoreValue = value;
        // Debug.Log($"Reward parameters set: Block {block}, Trial {trial}, Presses required {pressesRequired}, Value {value}");
    }

    private void PlayRewardAppearSound()
    {
        if (rewardAppearSound != null)
        {
            audioSource.enabled = true;
            audioSource.volume = 0.008f; // Reduced volume for reward sound
            audioSource.PlayOneShot(rewardAppearSound);
        }
    }
}