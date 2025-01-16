using UnityEngine;
using UnityEngine.SceneManagement;

public class GameInitializer : MonoBehaviour
{
    void Awake()
    {
        // Explicitly load the TitlePage scene
        SceneManager.LoadScene("TitlePage", LoadSceneMode.Single);
    }
}