using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FloorTileDebugger : MonoBehaviour
{
    private Renderer tileRenderer;
    private Material originalMaterial;
    private static bool debugMode = false;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalMaterial = tileRenderer.material;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            debugMode = !debugMode;
            Debug.Log($"Debug mode: {debugMode}");

            if (debugMode)
            {
                // Create a new material instance for debugging
                Material debugMat = new Material(Shader.Find("Unlit/Color"));
                debugMat.color = Color.magenta;
                tileRenderer.material = debugMat;
            }
            else
            {
                // Restore original material
                tileRenderer.material = originalMaterial;
            }
        }
    }

    void OnDestroy()
    {
        // Clean up any debug materials
        if (tileRenderer != null && tileRenderer.material != originalMaterial)
        {
            Destroy(tileRenderer.material);
        }
    }
}