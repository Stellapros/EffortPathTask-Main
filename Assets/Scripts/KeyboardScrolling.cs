using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine;  // Add this for UI event handling


public class KeyboardScrolling : MonoBehaviour
{
    [Header("Scrolling Configuration")]
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 0.1f;

    private void Update()
    {
        // Check for up/down arrow key input
        if (Input.GetKey(KeyCode.UpArrow))
        {
            // Scroll up (increase vertical position)
            scrollRect.verticalNormalizedPosition += scrollSpeed * Time.deltaTime;
            // Clamp value between 0 and 1
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            // Scroll down (decrease vertical position)
            scrollRect.verticalNormalizedPosition -= scrollSpeed * Time.deltaTime;
            // Clamp value between 0 and 1
            scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition);
        }
    }
}