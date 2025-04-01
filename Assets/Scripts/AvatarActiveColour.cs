using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarActiveColour : MonoBehaviour
{
    private SpriteRenderer avatarRenderer;
    private PlayerController playerController;

    [SerializeField] private Color targetColor = Color.white;

    // Awake runs before Start
    void Awake()
    {
        Debug.Log($"AvatarActiveColour Awake on GameObject: {gameObject.name}");

        // Try to get the renderer in Awake
        avatarRenderer = GetComponent<SpriteRenderer>();
        if (avatarRenderer == null)
        {
            Debug.LogError($"No SpriteRenderer found on {gameObject.name} in Awake");

            // Try looking for it in children
            avatarRenderer = GetComponentInChildren<SpriteRenderer>();
            if (avatarRenderer != null)
                Debug.Log($"Found SpriteRenderer in child: {avatarRenderer.gameObject.name}");
        }
        else
        {
            Debug.Log($"Found SpriteRenderer on {gameObject.name} in Awake. Initial color: {avatarRenderer.color}");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log($"AvatarActiveColour Start on GameObject: {gameObject.name}");

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogWarning($"No PlayerController found on {gameObject.name}");

        // Check for SpriteRenderer again in Start if not found in Awake
        if (avatarRenderer == null)
        {
            avatarRenderer = GetComponent<SpriteRenderer>();
            if (avatarRenderer == null)
            {
                Debug.LogError($"Still no SpriteRenderer found on {gameObject.name} in Start");
                return; // Exit early since we can't change color without a renderer
            }
        }

        // Log color before change
        Debug.Log($"Avatar color before change: {avatarRenderer.color}");

        // Set the color and log after change
        avatarRenderer.color = targetColor;
        Debug.Log($"Avatar color after change: {avatarRenderer.color}");

        // Schedule a check to see if the color sticks
        StartCoroutine(CheckColorAfterDelay());
    }

    private IEnumerator CheckColorAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        if (avatarRenderer != null)
        {
            Debug.Log($"Avatar color after 0.5 seconds: {avatarRenderer.color}");

            // Force the color again
            avatarRenderer.color = targetColor;
            Debug.Log($"Avatar color set again to: {targetColor}");
        }
    }

    // Update is called every frame
    void Update()
    {
        // Check if color is being reset somewhere
        if (avatarRenderer != null && avatarRenderer.color != targetColor)
        {
            Debug.Log($"Color changed from target! Current: {avatarRenderer.color}, Target: {targetColor}");
            avatarRenderer.color = targetColor; // Reset it to our target
        }
    }
}