using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor.Build.Content;
using UnityEngine;

public class Reward : MonoBehaviour
{
    public int scoreValue = 10;
    private int blockIndex;
    private int trialIndex;
    private int pressesRequired;

    public void SetRewardParameters(int block, int trial, int presses)
    {
        blockIndex = block;
        trialIndex = trial;
        pressesRequired = presses;
        // You can use these parameters to adjust the reward if needed
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ScoreManager.Instance.AddScore(scoreValue);
            Destroy(gameObject);
        }
    }
}