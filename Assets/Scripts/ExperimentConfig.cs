using UnityEngine;

[CreateAssetMenu(fileName = "ExperimentConfig", menuName = "Experiment/Config")]
public class ExperimentConfig : ScriptableObject
{
    [SerializeField] private string serverUrl = "https://default-server-url.com/api/submit-data";
    public string ServerUrl => serverUrl;
}
