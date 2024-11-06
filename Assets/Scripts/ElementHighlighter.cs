using UnityEngine;
using System.Collections;

public class ElementHighlighter : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float zoomDuration = 0.5f;
    [SerializeField] private float zoomScale = 1.5f;
    [SerializeField] private float highlightPadding = 20f;
    
    private Vector3 originalCameraPosition;
    private float originalOrthographicSize;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        originalCameraPosition = mainCamera.transform.position;
        originalOrthographicSize = mainCamera.orthographicSize;
    }

    public IEnumerator ZoomToElement(GameObject element, float duration)
    {
        if (element == null) yield break;

        // Store original camera properties
        Vector3 startPosition = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        // Calculate target position (centered on element)
        Vector3 targetPosition = element.transform.position;
        targetPosition.z = mainCamera.transform.position.z;

        // Calculate appropriate orthographic size based on element bounds
        Bounds elementBounds = GetElementBounds(element);
        float targetSize = Mathf.Max(elementBounds.size.x, elementBounds.size.y) / 2f;

        // Zoom animation
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Smooth easing
            t = Mathf.SmoothStep(0, 1, t);
            
            mainCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize * zoomScale, t);
            
            yield return null;
        }

        // Ensure we reach target values
        mainCamera.transform.position = targetPosition;
        mainCamera.orthographicSize = targetSize * zoomScale;
    }

    public IEnumerator ResetCamera(float duration)
    {
        Vector3 startPosition = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            t = Mathf.SmoothStep(0, 1, t);
            
            mainCamera.transform.position = Vector3.Lerp(startPosition, originalCameraPosition, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, originalOrthographicSize, t);
            
            yield return null;
        }

        mainCamera.transform.position = originalCameraPosition;
        mainCamera.orthographicSize = originalOrthographicSize;
    }

    private Bounds GetElementBounds(GameObject element)
    {
        Bounds bounds = new Bounds(element.transform.position, Vector3.zero);
        Renderer[] renderers = element.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
        }

        // Add padding
        bounds.Expand(highlightPadding);
        return bounds;
    }
}