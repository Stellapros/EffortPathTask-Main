using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public int calibrationButtonPresses;
    public float calibrationDistance;
    public float experimentStartTime;
    public float mainPhaseStartTime;
    public float experimentEndTime;

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
    }

    public void StartExperiment()
    {
        experimentStartTime = Time.time;
        SceneManager.LoadScene("WelcomeScene");
    }

    public void LoadNextScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex + 1);
    }

    public void EndExperiment()
    {
        experimentEndTime = Time.time;
        // Save data or perform any cleanup
    }
}