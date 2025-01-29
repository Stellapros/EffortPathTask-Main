using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class ButtonNavigationController : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private List<Component> navigationElements = new List<Component>();
    [SerializeField] public bool useHorizontalNavigation = true; // New field to toggle horizontal navigation

    [Header("Button Colors")]
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color selectedColor = new Color(0.87f, 0.86f, 0.67f);
    [SerializeField] private Color pressedColor = new Color(0.87f, 0.86f, 0.67f);

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;

    private AudioSource audioSource;
    private int currentElementIndex = 0;
    private Component currentSelectedElement;
    private Dictionary<Component, Image> elementImages = new Dictionary<Component, Image>();
    private bool isDropdownExpanded = false;
    private int currentDropdownIndex = 0;
    private TMP_Dropdown activeDropdown = null;
    private int savedDropdownValue = 0; // Store the original value in case of cancellation

private void Start()
{
    audioSource = gameObject.AddComponent<AudioSource>();

    // Remove any null elements from the list
    navigationElements = navigationElements.Where(element => element != null).ToList();

    foreach (Component element in navigationElements)
    {
        Image elementImage = null;

        if (element is Button button)
            elementImage = button.GetComponent<Image>();
        else if (element is TMP_InputField inputField)
            elementImage = inputField.GetComponent<Image>();
        else if (element is TMP_Dropdown dropdown)
        {
            elementImage = dropdown.GetComponent<Image>();

            // Add listener to track dropdown state
            dropdown.onValueChanged.AddListener(_ =>
            {
                if (!isDropdownExpanded) // Only handle if this is a "real" selection
                {
                    isDropdownExpanded = false;
                    activeDropdown = null;
                }
            });
        }

        if (elementImage != null && elementImage.gameObject != null)
        {
            elementImages[element] = elementImage;
            elementImage.color = normalColor;
        }
    }

    // Ensure no default selection
    currentElementIndex = -1;
    currentSelectedElement = null;
    EventSystem.current.SetSelectedGameObject(null);
}

    private void Update()
    {
        // Add a more immediate response to key presses
        if (navigationElements.Count == 0) return;

        // Check for immediate key down instead of relying solely on Update()
        if (!isDropdownExpanded)
        {
            // Add more immediate key handling
            HandleQuickNavigation();
        }
        else if (isDropdownExpanded && activeDropdown != null)
        {
            HandleDropdownNavigation();
        }

        UpdateVisualFeedback();
    }

    private void HandleDropdownNavigation()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            if (currentDropdownIndex < activeDropdown.options.Count - 1)
            {
                currentDropdownIndex++;
                // Only highlight the option, don't commit the value yet
                activeDropdown.value = currentDropdownIndex;
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            if (currentDropdownIndex > 0)
            {
                currentDropdownIndex--;
                // Only highlight the option, don't commit the value yet
                activeDropdown.value = currentDropdownIndex;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            // Confirm selection
            activeDropdown.value = currentDropdownIndex;
            ExitDropdownMode(true);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Cancel selection - restore original value
            activeDropdown.value = savedDropdownValue;
            ExitDropdownMode(false);
        }
    }

        public void ClearElements()
    {
        navigationElements.Clear();
        elementImages.Clear();
        currentElementIndex = 0;
        currentSelectedElement = null;
    }

    [SerializeField] private float navigationRepeatDelay = 0.2f; // Adjustable repeat rate
    private float nextNavigationTime = 0f;

    private void HandleQuickNavigation()
    {
        if (Time.time >= nextNavigationTime)
        {
            bool navigated = false;

            // More granular and immediate key checks
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                NavigateToNextElement();
                navigated = true;
            }
            else if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                NavigateToPreviousElement();
                navigated = true;
            }

            // Optional: Add horizontal navigation if needed
            if (useHorizontalNavigation)
            {
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                {
                    NavigateToNextElement();
                    navigated = true;
                }
                else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                {
                    NavigateToPreviousElement();
                    navigated = true;
                }
            }
            if (navigated)
            {
                nextNavigationTime = Time.time + navigationRepeatDelay;
            }
        }
        // Immediate selection handling
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            HandleElementSelection();
    }

private void HandleElementSelection()
{
    if (currentSelectedElement == null) return;

    switch (currentSelectedElement)
    {
        case TMP_Dropdown dropdown when dropdown != null && dropdown.gameObject.activeInHierarchy:
            EnterDropdownMode(dropdown);
            break;

        case Button button when button != null &&
                     button.gameObject != null &&
                     button.gameObject.activeInHierarchy:
            if (elementImages.TryGetValue(button, out var buttonImage) && buttonImage != null)
            {
                Debug.Log("Button selected and about to invoke");

                // Ensure button is still valid before proceeding
                if (button != null && !button.Equals(null) && button.interactable)
                {
                    buttonImage.color = pressedColor;
                    PlayButtonSound();

                    // Prevent multiple invocations by temporarily disabling the button
                    button.interactable = false;

                    // Use Invoke with a small delay to reset interactability
                    Invoke("ResetButtonInteractable", 0.2f);

                    button.onClick.Invoke();
                }
            }
            break;

        case TMP_InputField inputField when inputField != null &&
                                             inputField.gameObject != null &&
                                             inputField.gameObject.activeInHierarchy:
            inputField.ActivateInputField();
            inputField.Select();
            break;
    }
}

    private void ResetButtonInteractable()
    {
        // Add comprehensive null and destroyed object checks
        if (currentSelectedElement != null &&
            currentSelectedElement is Button button &&
            button != null &&
            !button.Equals(null) &&
            button.gameObject != null)
        {
            try
            {
                button.interactable = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not reset button interactability: {e.Message}");
            }
        }
    }

    private void EnterDropdownMode(TMP_Dropdown dropdown)
    {
        isDropdownExpanded = true;
        activeDropdown = dropdown;
        savedDropdownValue = dropdown.value; // Store the original value
        currentDropdownIndex = dropdown.value;
        dropdown.Show();
    }

    private void ExitDropdownMode(bool confirmed)
    {
        // Check if activeDropdown is null before accessing it
        if (activeDropdown == null) return;

        try
        {
            if (!confirmed)
            {
                // Only set value if dropdown is not null
                activeDropdown.value = savedDropdownValue;
            }

            // Hide the dropdown only if it's not null
            activeDropdown.Hide();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ExitDropdownMode: {e.Message}");
        }
        finally
        {
            isDropdownExpanded = false;
            activeDropdown = null;
        }
    }
    private void NavigateToNextElement()
    {
        if (!isDropdownExpanded) // Only allow navigation when not in dropdown
        {
            currentElementIndex = (currentElementIndex + 1) % navigationElements.Count;
            SetSelectedElement(navigationElements[currentElementIndex]);
        }
    }

    private void NavigateToPreviousElement()
    {
        if (!isDropdownExpanded) // Only allow navigation when not in dropdown
        {
            currentElementIndex = (currentElementIndex - 1 + navigationElements.Count) % navigationElements.Count;
            SetSelectedElement(navigationElements[currentElementIndex]);
        }
    }

    private void SetSelectedElement(Component element)
    {
        if (element == null) return; // Early exit if element is null

        if (!isDropdownExpanded) // Only allow selection changes when not in dropdown
        {
            currentSelectedElement = element;

            // Null checks before setting selected game object
            if (element is Button button && button != null && button.gameObject != null)
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            else if (element is TMP_InputField inputField && inputField != null && inputField.gameObject != null)
                EventSystem.current.SetSelectedGameObject(inputField.gameObject);
            else if (element is TMP_Dropdown dropdown && dropdown != null && dropdown.gameObject != null)
                EventSystem.current.SetSelectedGameObject(dropdown.gameObject);
        }
    }

    [SerializeField] private float selectionPulseSpeed = 2f;
    [SerializeField] private float selectionPulseIntensity = 0.2f;

    private void UpdateVisualFeedback()
    {
        // Create a list to track elements to remove
        List<Component> elementsToRemove = new List<Component>();

        foreach (var elementPair in elementImages.ToList()) // Create a copy to safely iterate
        {
            Component element = elementPair.Key;
            Image elementImage = elementPair.Value;

            // Comprehensive null and destroyed object checks
            if (element == null ||
                elementImage == null ||
                elementImage.gameObject == null ||
                !elementImage.gameObject.activeInHierarchy)
            {
                elementsToRemove.Add(element);
                continue;
            }

            try
            {
                if (element == currentSelectedElement)
                {
                    // Pulsing effect for selected element
                    float pulse = Mathf.PingPong(Time.time * selectionPulseSpeed, 1f);
                    Color pulseColor = Color.Lerp(selectedColor, Color.white, pulse * selectionPulseIntensity);
                    elementImage.color = pulseColor;
                }
                else
                {
                    elementImage.color = normalColor;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating visual feedback for {element}: {e.Message}");
                elementsToRemove.Add(element);
            }
        }

        // Remove any invalid elements
        foreach (var elementToRemove in elementsToRemove)
        {
            try
            {
                RemoveElement(elementToRemove);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error removing element: {e.Message}");
            }
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    public void AddElement(Component element)
    {
        if (!navigationElements.Contains(element))
        {
            navigationElements.Add(element);

            Image elementImage = null;
            if (element is Button button)
            {
                elementImage = button.GetComponent<Image>();
            }
            else if (element is TMP_InputField inputField)
            {
                elementImage = inputField.GetComponent<Image>();
            }
            else if (element is TMP_Dropdown dropdown)
            {
                elementImage = dropdown.GetComponent<Image>();
            }

            if (elementImage != null)
            {
                elementImages[element] = elementImage;
                elementImage.color = normalColor;
            }
        }
    }

    public void RemoveElement(Component element)
    {
        if (element == null) return;

        if (navigationElements != null && navigationElements.Contains(element))
        {
            if (element == activeDropdown)
            {
                try
                {
                    ExitDropdownMode(false);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error exiting dropdown mode: {e.Message}");
                }
            }

            navigationElements.Remove(element);

            if (elementImages != null)
            {
                elementImages.Remove(element);
            }

            if (currentSelectedElement == element)
            {
                currentElementIndex = 0;
                if (navigationElements != null && navigationElements.Count > 0)
                {
                    SetSelectedElement(navigationElements[0]);
                }
                else
                {
                    currentSelectedElement = null;
                }
            }
        }
    }
}
