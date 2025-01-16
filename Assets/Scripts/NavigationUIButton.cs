using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneNavigationButton : MonoBehaviour
{
    public ExperimentNavigationController.SceneType targetScene;
    private Button buttonComponent;

    void Awake()
    {
        // Ensure button component exists
        buttonComponent = GetComponent<Button>();
        if (buttonComponent == null)
        {
            buttonComponent = gameObject.AddComponent<Button>();
        }

        // Add click listener
        buttonComponent.onClick.AddListener(NavigateToScene);

        // Debug logging
        Debug.Log($"SceneNavigationButton initialized for scene: {targetScene}");
    }

    void NavigateToScene()
    {
        Debug.Log($"Attempting to navigate to scene: {targetScene}");

        // Check for ExperimentNavigationController
        if (ExperimentNavigationController.Instance != null)
        {
            try
            {
                ExperimentNavigationController.Instance.NavigateToScene(targetScene);
                Debug.Log($"Navigation successful to {targetScene}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Navigation error: {e.Message}");
                FallbackSceneLoad(targetScene);
            }
        }
        else
        {
            Debug.LogWarning("ExperimentNavigationController not found. Using fallback navigation.");
            FallbackSceneLoad(targetScene);
        }
    }

    void FallbackSceneLoad(ExperimentNavigationController.SceneType sceneType)
    {
        string sceneToLoad = sceneType switch
        {
            ExperimentNavigationController.SceneType.Instruction => "TourGame",
            ExperimentNavigationController.SceneType.Practice => "GetReadyPractice",
            ExperimentNavigationController.SceneType.MainGame => "GetReadyFormal",
            ExperimentNavigationController.SceneType.Quit => "EndExperiment",
            _ => throw new System.ArgumentException("Invalid scene type")
        };

        try
        {
            SceneManager.LoadScene(sceneToLoad);
            Debug.Log($"Fallback scene load successful: {sceneToLoad}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Fallback scene load failed: {e.Message}");
        }
    }
}