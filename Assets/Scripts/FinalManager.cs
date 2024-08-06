using UnityEngine;
using UnityEngine.UI;
using System.IO; // Add this line to include the necessary namespace

[System.Diagnostics.DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
public class FinalManager : MonoBehaviour
{
    public Text completionText;
    public Text durationText;

    private void Start()
    {
        completionText.text = "Experiment completed. Thank you for your participation!";
        
        float totalDuration = GameManager.Instance.experimentEndTime - GameManager.Instance.experimentStartTime;
        float mainPhaseDuration = GameManager.Instance.experimentEndTime - GameManager.Instance.mainPhaseStartTime;
        
        durationText.text = $"Total Duration: {totalDuration:F2} seconds\nMain Phase Duration: {mainPhaseDuration:F2} seconds";
        
        ExportData();
    }


    private void ExportData()
    {
        string sourcePath = Path.Combine(Application.persistentDataPath, "experiment_data.csv");
        string destPath = Path.Combine(Application.persistentDataPath, $"experiment_data_{PlayerPrefs.GetString("ID")}.csv");
        File.Copy(sourcePath, destPath, true);
        Debug.Log($"Data exported to: {destPath}");
    }


    private string GetDebuggerDisplay()
    {
        return ToString();
    }
}