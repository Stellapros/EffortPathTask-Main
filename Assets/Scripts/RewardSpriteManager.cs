using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class RewardSpriteManager : MonoBehaviour
{
    public static RewardSpriteManager Instance { get; private set; }
    [System.Serializable]
        public struct RewardMapping
    {
        [Tooltip("The sprite shown in the decision UI")]
        public Sprite effortSprite;
        
        [Tooltip("The prefab to spawn in the grid world")]
        public GameObject rewardPrefab;
        
        [Tooltip("The effort level (1, 2, 3, etc.)")]
        public int effortLevel;
        
        [Tooltip("Description for this reward type")]
        public string description;
    }

    [Header("Reward Configuration")]
    [Tooltip("Map each effort level to its sprite and prefab")]
    [SerializeField] private RewardMapping[] rewardMappings;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        
        ValidateMappings();
    }

    private void ValidateMappings()
    {
        if (rewardMappings == null || rewardMappings.Length == 0)
        {
            Debug.LogError("No reward mappings configured in RewardSpriteManager!");
            return;
        }
        
        foreach (var mapping in rewardMappings)
        {
            if (mapping.effortSprite == null)
            {
                Debug.LogError($"Missing sprite for effort level {mapping.effortLevel}!");
            }
            if (mapping.rewardPrefab == null)
            {
                Debug.LogError($"Missing prefab for effort level {mapping.effortLevel}!");
            }
        }

        // Check for duplicate effort levels
        for (int i = 0; i < rewardMappings.Length; i++)
        {
            for (int j = i + 1; j < rewardMappings.Length; j++)
            {
                if (rewardMappings[i].effortLevel == rewardMappings[j].effortLevel)
                {
                    Debug.LogError($"Duplicate effort level found: {rewardMappings[i].effortLevel}");
                }
            }
        }

        // Log successful configuration
        Debug.Log($"RewardSpriteManager initialized with {rewardMappings.Length} mappings:");
        foreach (var mapping in rewardMappings)
        {
            Debug.Log($"Effort Level {mapping.effortLevel}: Sprite={mapping.effortSprite?.name}, Prefab={mapping.rewardPrefab?.name}");
        }
    }

        public GameObject GetRewardPrefabForSprite(Sprite effortSprite)
    {
        foreach (var mapping in rewardMappings)
        {
            if (mapping.effortSprite == effortSprite)
            {
                return mapping.rewardPrefab;
            }
        }
        Debug.LogError($"No reward prefab found for sprite: {effortSprite.name}");
        return null;
    }

    public GameObject GetRewardPrefabForEffortLevel(int effortLevel)
    {
        foreach (var mapping in rewardMappings)
        {
            if (mapping.effortLevel == effortLevel)
            {
                if (mapping.rewardPrefab == null)
                {
                    Debug.LogError($"Reward prefab is null for effort level: {effortLevel}");
                    return null;
                }
                Debug.Log($"Found reward prefab {mapping.rewardPrefab.name} for effort level {effortLevel}");
                return mapping.rewardPrefab;
            }
        }
        Debug.LogError($"No reward prefab found for effort level: {effortLevel}");
        return null;
    }

    public Sprite GetSpriteForEffortLevel(int effortLevel)
    {
        foreach (var mapping in rewardMappings)
        {
            if (mapping.effortLevel == effortLevel)
            {
                if (mapping.effortSprite == null)
                {
                    Debug.LogError($"Sprite is null for effort level: {effortLevel}");
                    return null;
                }
                return mapping.effortSprite;
            }
        }
        Debug.LogError($"No sprite found for effort level: {effortLevel}");
        return null;
    }

    #region Debug Methods
    public void LogCurrentMappings()
    {
        Debug.Log("Current Reward Mappings:");
        foreach (var mapping in rewardMappings)
        {
            Debug.Log($"Level {mapping.effortLevel}: " +
                     $"Sprite={mapping.effortSprite?.name ?? "NULL"}, " +
                     $"Prefab={mapping.rewardPrefab?.name ?? "NULL"}, " +
                     $"Description={mapping.description}");
        }
    }
    #endregion
}