using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDebugger : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("Scene Debugger - Scene Loaded");
        Debug.Log($"Active Scene: {SceneManager.GetActiveScene().name}");

        // Find and log key managers
        var playerController = FindAnyObjectByType<PlayerController>();
        var practiceManager = FindAnyObjectByType<PracticeManager>();
        var experimentManager = FindAnyObjectByType<ExperimentManager>();

        Debug.Log($"PlayerController found: {playerController != null}");
        Debug.Log($"PracticeManager found: {practiceManager != null}");
        Debug.Log($"ExperimentManager found: {experimentManager != null}");

        // Log PlayerPrefs
        Debug.Log($"Is Practice Trial: {PlayerPrefs.GetInt("IsPracticeTrial", 0)}");
        Debug.Log($"Current Practice Trial Index: {PlayerPrefs.GetInt("CurrentPracticeTrialIndex", -1)}");
        Debug.Log($"Current Practice Effort Level: {PlayerPrefs.GetInt("CurrentPracticeEffortLevel", -1)}");
    }
}