using UnityEngine;
using System.Collections;

public class GameController : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private SpawnManager spawnManager;
    [SerializeField] private CountdownTimer countdownTimer;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private float maxTrialDuration = 30f;
    [SerializeField] private float restDuration = 3f;
    [SerializeField] private int[] pressesPerStep = { 3, 2, 1 };
    [SerializeField] private int trialsPerBlock = 20;

    private int currentBlockIndex = 0;
    private int currentTrialInBlock = 0;
    private bool rewardCollected = false;

    private void Start()
    {
        countdownTimer.OnTimerExpired += OnTrialEnd;
        StartCoroutine(RunExperiment());
    }

    private IEnumerator RunExperiment()
    {
        while (currentBlockIndex < pressesPerStep.Length)
        {
            for (currentTrialInBlock = 0; currentTrialInBlock < trialsPerBlock; currentTrialInBlock++)
            {
                yield return StartCoroutine(RunTrial());
                yield return new WaitForSeconds(restDuration);
                currentTrialInBlock++; // 在试验结束后递增计数器
            }
            currentBlockIndex++;
        }
    }

    private IEnumerator RunTrial()
    {
        int currentPressesRequired = pressesPerStep[currentBlockIndex];
        spawnManager.SpawnReward(currentBlockIndex, currentTrialInBlock, currentPressesRequired);
        playerMovement.SetPressesPerStep(currentPressesRequired);
        playerMovement.EnableMovement();
        countdownTimer.ResetTimer();
        countdownTimer.StartTimer();
        rewardCollected = false;

        yield return new WaitUntil(() => rewardCollected || countdownTimer.TimeLeft <= 0);

        EndTrial();
    }

    private void EndTrial()
    {
        playerMovement.DisableMovement();
        spawnManager.ClearReward();
        countdownTimer.ResetTimer();
    }

    private void OnTrialEnd() => EndTrial();

    public void RewardCollected() => rewardCollected = true;
}