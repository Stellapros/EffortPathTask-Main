using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages scene transitions and button setup for the experiment.
/// </summary>
public class CustomSceneManager : MonoBehaviour
{
    // Singleton instance
    public static CustomSceneManager Instance { get; private set; }

    [System.Serializable]
    public class SceneTransition
    {
        public string fromScene;
        public string toScene;
        public string buttonName;
    }

    [SerializeField]
    private List<SceneTransition> sceneTransitions = new List<SceneTransition>();

    [SerializeField]
    private string initialScene = "WelcomePage";

    private void Awake()
    {
        // Singleton pattern implementation
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Load the initial scene if not already in it
        if (SceneManager.GetActiveScene().name != initialScene)
        {
            LoadScene(initialScene);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event when the object is destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Called when a new scene is loaded.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupButtons(scene.name);
    }

    /// <summary>
    /// Sets up button listeners for the current scene.
    /// </summary>
    private void SetupButtons(string sceneName)
    {
        foreach (var transition in sceneTransitions)
        {
            if (transition.fromScene == sceneName)
            {
                GameObject buttonObj = GameObject.Find(transition.buttonName);
                if (buttonObj != null)
                {
                    UnityEngine.UI.Button button = buttonObj.GetComponent<UnityEngine.UI.Button>();
                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners(); // Clear existing listeners
                        button.onClick.AddListener(() => LoadScene(transition.toScene));
                    }
                    else
                    {
                        Debug.LogWarning($"Button component not found on {transition.buttonName} in {sceneName}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Button {transition.buttonName} not found in {sceneName}");
                }
            }
        }
    }

    /// <summary>
    /// Loads a new scene by name.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Loads a new scene asynchronously.
    /// </summary>
    public AsyncOperation LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        return SceneManager.LoadSceneAsync(sceneName, mode);
    }

    /// <summary>
    /// Gets the currently active scene.
    /// </summary>
    public Scene GetActiveScene()
    {
        return SceneManager.GetActiveScene();
    }
}