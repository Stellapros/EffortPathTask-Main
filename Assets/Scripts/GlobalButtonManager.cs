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
    [SerializeField] private bool useHorizontalNavigation = true; // New field to toggle horizontal navigation

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

        // More defensive checks before initial selection
        if (navigationElements != null && navigationElements.Count > 0)
        {
            var firstValidElement = navigationElements.FirstOrDefault();
            if (firstValidElement != null)
            {
                SetSelectedElement(firstValidElement);
                currentElementIndex = 0;
            }
        }
    }

    private void Update()
    {
        if (navigationElements.Count == 0) return;

        // If dropdown is expanded, only handle dropdown navigation
        if (isDropdownExpanded && activeDropdown != null)
        {
            HandleDropdownNavigation();
        }
        // Otherwise handle standard navigation
        else
        {
            HandleStandardNavigation();
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

    // // if up-down navigation needed in the drop down, can use this code
    // private void HandleDropdownNavigation()
    // {
    //     if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ||
    //         (useHorizontalNavigation && Input.GetKeyDown(KeyCode.RightArrow)) ||
    //         (useHorizontalNavigation && Input.GetKeyDown(KeyCode.D)))
    //     {
    //         if (currentDropdownIndex < activeDropdown.options.Count - 1)
    //         {
    //             currentDropdownIndex++;
    //             activeDropdown.value = currentDropdownIndex;
    //         }
    //     }
    //     else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
    //              (useHorizontalNavigation && Input.GetKeyDown(KeyCode.LeftArrow)) ||
    //              (useHorizontalNavigation && Input.GetKeyDown(KeyCode.A)))
    //     {
    //         if (currentDropdownIndex > 0)
    //         {
    //             currentDropdownIndex--;
    //             activeDropdown.value = currentDropdownIndex;
    //         }
    //     }
    //     else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
    //     {
    //         activeDropdown.value = currentDropdownIndex;
    //         ExitDropdownMode(true);
    //     }
    //     else if (Input.GetKeyDown(KeyCode.Escape))
    //     {
    //         activeDropdown.value = savedDropdownValue;
    //         ExitDropdownMode(false);
    //     }
    // }

    private void HandleStandardNavigation()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            NavigateToNextElement();
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            NavigateToPreviousElement();
        }
        else if (useHorizontalNavigation && (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)))
        {
            NavigateToNextElement();
        }
        else if (useHorizontalNavigation && (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)))
        {
            NavigateToPreviousElement();
        }
        else if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            HandleElementSelection();
        }
    }

    private void HandleElementSelection()
    {
        if (currentSelectedElement is TMP_Dropdown dropdown)
        {
            // Enter dropdown mode
            EnterDropdownMode(dropdown);
        }
        else if (currentSelectedElement is Button button)
        {
            if (elementImages.ContainsKey(button))
            {
                elementImages[button].color = pressedColor;
                PlayButtonSound();
                button.onClick.Invoke();
            }
        }
        else if (currentSelectedElement is TMP_InputField inputField)
        {
            inputField.ActivateInputField();
            inputField.Select();
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

    // private void ExitDropdownMode(bool confirmed)
    // {
    //     if (!confirmed)
    //     {
    //         activeDropdown.value = savedDropdownValue;
    //     }

    //     isDropdownExpanded = false;
    //     activeDropdown.Hide();
    //     activeDropdown = null;
    // }

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

    // private void UpdateVisualFeedback()
    // {
    //     foreach (var elementPair in elementImages)
    //     {
    //         Component element = elementPair.Key;
    //         Image elementImage = elementPair.Value;

    //         elementImage.color = (element == currentSelectedElement) ? selectedColor : normalColor;
    //     }
    // }

    private void UpdateVisualFeedback()
    {
        // Avoid processing if elementImages is null or empty
        if (elementImages == null || elementImages.Count == 0) return;

        // Create a list to track elements to remove
        List<Component> elementsToRemove = new List<Component>();

        foreach (var elementPair in elementImages.ToList()) // Create a copy to safely iterate
        {
            Component element = elementPair.Key;
            Image elementImage = elementPair.Value;

            // Comprehensive null and destroyed object checks
            if (element == null ||
                elementImage == null ||
                !elementImage || // Unity's null check for destroyed objects
                element.gameObject == null ||
                !element.gameObject.activeInHierarchy)
            {
                elementsToRemove.Add(element);
                continue;
            }

            try
            {
                // Safe color assignment
                elementImage.color = (element == currentSelectedElement) ? selectedColor : normalColor;
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

    // public void RemoveElement(Component element)
    // {
    //     if (navigationElements.Contains(element))
    //     {
    //         if (element == activeDropdown)
    //         {
    //             ExitDropdownMode(false);
    //         }

    //         navigationElements.Remove(element);
    //         elementImages.Remove(element);

    //         if (currentSelectedElement == element)
    //         {
    //             currentElementIndex = 0;
    //             if (navigationElements.Count > 0)
    //             {
    //                 SetSelectedElement(navigationElements[0]);
    //             }
    //         }
    //     }
    // }
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
