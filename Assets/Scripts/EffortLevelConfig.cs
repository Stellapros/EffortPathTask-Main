using UnityEngine;
using System;

[CreateAssetMenu(fileName = "EffortLevelConfig", menuName = "Experiment/Effort Level Config", order = 1)]
public class EffortLevelConfig : ScriptableObject
{
    [Serializable]
    public class EffortLevel
    {
        public int level;
        public int pressesRequired;
        public Sprite sprite;
    }

    public EffortLevel[] effortLevels;

    public int GetPressesRequired(int level)
    {
        foreach (var effortLevel in effortLevels)
        {
            if (effortLevel.level == level)
            {
                return effortLevel.pressesRequired;
            }
        }
        Debug.LogWarning($"No configuration found for effort level {level}. Returning default value of 2.");
        return 2;
    }

    public Sprite GetSprite(int level)
    {
        foreach (var effortLevel in effortLevels)
        {
            if (effortLevel.level == level)
            {
                return effortLevel.sprite;
            }
        }
        Debug.LogWarning($"No sprite found for effort level {level}. Returning null.");
        return null;
    }
}