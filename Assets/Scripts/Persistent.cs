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

        // Load your first actual game scene
        SceneManager.LoadScene("TitlePage");
    }
}