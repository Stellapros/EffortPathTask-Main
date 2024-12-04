using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    public GameObject experimentManagerPrefab;

    void Start()
    {
        if (ExperimentManager.Instance == null)
        {
            Instantiate(experimentManagerPrefab);
        }
        
        // Ensure GridWorldManager is properly set up as a persistent object
        if (GridWorldManager.Instance == null)
        {
            // Instantiate the GridWorldManager if it doesn't exist
            Instantiate(Resources.Load<GameObject>("GridWorldManager"));
        }

        // Load your first actual game scene
        SceneManager.LoadScene("TitlePage");
    }
}

