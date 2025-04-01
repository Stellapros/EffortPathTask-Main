using UnityEngine;

[CreateAssetMenu(fileName = "ExperimentConfig", menuName = "Experiment/Config")]
public class ExperimentConfig : ScriptableObject
{
    public string ServerUrl = "https://effortpatch-0b3abd136749.herokuapp.com/upload";
}