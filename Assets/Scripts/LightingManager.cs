using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// Access to the script ///
/// LightingManager.Instance.SetIntensity(1.5f);
/// LightingManager.Instance.SetColor(Color.yellow);
/// </summary>
public class LightingManager : MonoBehaviour
{
    public static LightingManager Instance { get; private set; }

    [SerializeField] private Light directionalLight;

    [Header("Light Settings")]
    [SerializeField] private float intensity = 0.7f;
    [SerializeField] private Color color = Color.white;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupGlobalLight();
        }
        else
        {
            Destroy(gameObject);
        }
    }

        private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetupGlobalLight();
        UpdateLightSettings();
    }

    private void SetupGlobalLight()
    {
        if (directionalLight == null)
        {
            // Check if a directional light already exists in the scene
            directionalLight = FindAnyObjectByType<Light>();

            if (directionalLight == null || directionalLight.type != LightType.Directional)
            {
                // Create a new directional light if one doesn't exist
                GameObject lightObject = new GameObject("Global Directional Light");
                directionalLight = lightObject.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
                DontDestroyOnLoad(lightObject);
            }
        }

        // Ensure the light object persists across scene loads
        DontDestroyOnLoad(directionalLight.gameObject);
    }

    public void UpdateLightSettings()
    {
        if (directionalLight != null)
        {
            directionalLight.intensity = intensity;
            directionalLight.color = color;
        }
        else
        {
            Debug.LogWarning("Directional light is missing. Setting up a new one.");
            SetupGlobalLight();
            UpdateLightSettings();
        }
    }

    // Public methods to modify light settings at runtime
    public void SetIntensity(float newIntensity)
    {
        intensity = newIntensity;
        UpdateLightSettings();
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        UpdateLightSettings();
    }
}