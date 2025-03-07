using UnityEngine;

public class CameraSetup : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;

    void Start()
    {
        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.orthographic = true;

            // Increase buffer for more space around the grid
            float buffer = 1.4f;  // Increased from 1.2 to 1.4

            float gridHeight = gridManager.GridHeight * gridManager.cellSize;
            float gridWidth = gridManager.GridWidth * gridManager.cellSize;

            // Get the current screen aspect ratio
            float screenAspect = (float)Screen.width / Screen.height;

            // Calculate the grid aspect ratio
            float gridAspect = gridWidth / gridHeight;

            // Calculate the orthographic size based on both the screen and grid aspects
            float orthoSize;
            if (screenAspect < gridAspect)
            {
                // Width is the constraining factor
                orthoSize = (gridWidth / screenAspect) / 2f;
            }
            else
            {
                // Height is the constraining factor
                orthoSize = gridHeight / 2f;
            }

            // Apply the buffer
            cam.orthographicSize = orthoSize * buffer;

            // Center the camera on the grid
            Vector3 gridCenter = new Vector3(
                (gridWidth - gridManager.cellSize) / 2f,
                (gridHeight - gridManager.cellSize) / 2f,
                -10
            );
            transform.position = gridCenter;
        }
    }

    // Add this method to handle screen resizing
    void OnRectTransformDimensionsChange()
    {
        Start();
    }
}