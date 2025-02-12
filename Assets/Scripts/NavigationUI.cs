using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ExperimentNavigationController : MonoBehaviour
{
    public static ExperimentNavigationController Instance { get; private set; }

    // Current active scene type
    public SceneType CurrentScene { get; private set; }

    // Enum to define scene types
    public enum SceneType
    {
        Instruction,
        Practice,
        MainGame,
        Quit
    }

    void Awake()
    {
        // Singleton pattern to ensure only one instance exists
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

    // Method to navigate to a specific scene
    public void NavigateToScene(SceneType sceneToLoad)
    {
        CurrentScene = sceneToLoad;

        // Load the appropriate scene based on scene type
        switch (sceneToLoad)
        {
            case SceneType.Instruction:
                SceneManager.LoadScene("TourGame");
                break;
            case SceneType.Practice:
                SceneManager.LoadScene("GetReadyPractice");
                break;
            case SceneType.MainGame:
                SceneManager.LoadScene("GetReadyFormal");
                break;
            case SceneType.Quit:
                SceneManager.LoadScene("EndExperiment");
                // Application.Quit(); // Or load a quit/exit scene
                break;
        }
    }

    // Optional: Method to get current scene type from any script
    public SceneType GetCurrentScene()
    {
        return CurrentScene;
    }
}
