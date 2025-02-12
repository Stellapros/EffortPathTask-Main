using UnityEngine;
using TMPro;

public class PlayModeIndicator : MonoBehaviour
{
    private static PlayModeIndicator _instance;
    public static PlayModeIndicator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PlayModeIndicator>();
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [SerializeField] private TextMeshProUGUI playModeText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Hide initially
        HidePlayMode();
    }

    public void ShowPlayMode()
    {
        if (playModeText != null)
        {
            playModeText.enabled = true;
        }
    }

    public void HidePlayMode()
    {
        if (playModeText != null)
        {
            playModeText.enabled = false;
        }
    }
}