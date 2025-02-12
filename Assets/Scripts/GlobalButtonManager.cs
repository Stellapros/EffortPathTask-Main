using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System.Collections;

public class ButtonNavigationController : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private List<Component> navigationElements = new List<Component>();
    [SerializeField] public bool useHorizontalNavigation = true;

    [Header("Button Colors")]
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);
    [SerializeField] private Color selectedColor = new Color(0.87f, 0.86f, 0.67f);
    [SerializeField] private Color disabledColor = new Color(0.7f, 0.7f, 0.7f);

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;

    private AudioSource audioSource;
    private int currentElementIndex = -1;
    private Component currentSelectedElement;
    private Dictionary<Component, Image> elementImages = new Dictionary<Component, Image>();
    private bool isDropdownExpanded = false;
    private TMP_Dropdown activeDropdown = null;
    private int savedDropdownValue = 0;
    public bool isProcessing = false;

    private void Start()
    {
        // Ensure EventSystem exists
        if (EventSystem.current == null)
        {
            var eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                Debug.LogWarning("No EventSystem found in scene. Creating one...");
                var eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<EventSystem>();
                eventSystemGO.AddComponent<StandaloneInputModule>();
            }
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        CleanupNullElements();
        InitializeElementImages();
        ResetSelection();
    }

    private void CleanupNullElements()
    {
        navigationElements = navigationElements.Where(element =>
            element != null &&
            element.gameObject != null &&
            !element.Equals(null)
        ).ToList();
    }

    private void InitializeElementImages()
    {
        elementImages.Clear();
        foreach (var element in navigationElements.ToList())
        {
            if (element == null || element.gameObject == null || element.Equals(null))
            {
                navigationElements.Remove(element);
                continue;
            }

            if (element.TryGetComponent<Image>(out Image elementImage))
            {
                elementImages[element] = elementImage;
                elementImage.color = normalColor;
            }

            // Setup dropdown listeners
            if (element is TMP_Dropdown dropdown)
            {
                dropdown.onValueChanged.AddListener(_ => HandleDropdownValueChanged(dropdown));
            }
        }
    }

    private void HandleDropdownValueChanged(TMP_Dropdown dropdown)
    {
        if (!isDropdownExpanded)
        {
            isDropdownExpanded = false;
            activeDropdown = null;
        }
    }

    private void Update()
    {
        if (isProcessing || navigationElements == null || navigationElements.Count == 0) return;

        if (navigationElements.Any(e => e == null || e.gameObject == null || e.Equals(null)))
        {
            CleanupNullElements();
            if (navigationElements.Count == 0) return;
        }

        // Handle mouse input
        HandleMouseInput();

        if (!isDropdownExpanded)
        {
            HandleMainNavigation();
        }
        else if (activeDropdown != null)
        {
            HandleDropdownNavigation();
        }

        UpdateVisualFeedback();
    }

    private void HandleMouseInput()
    {
        if (Input.GetMouseButtonDown(0)) // Left click
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (RaycastResult result in results)
            {
                // Handle TMP_InputField clicks
                TMP_InputField inputField = result.gameObject.GetComponent<TMP_InputField>();
                if (inputField != null && navigationElements.Contains(inputField))
                {
                    inputField.Select();
                    inputField.ActivateInputField();
                    SetCurrentElement(inputField);
                    return;
                }

                // Handle dropdown clicks
                TMP_Dropdown dropdown = result.gameObject.GetComponent<TMP_Dropdown>();
                if (dropdown != null && navigationElements.Contains(dropdown))
                {
                    HandleDropdownActivation(dropdown);
                    SetCurrentElement(dropdown);
                    return;
                }

                // Handle dropdown options
                if (isDropdownExpanded && activeDropdown != null)
                {
                    var itemText = result.gameObject.GetComponent<TMP_Text>();
                    if (itemText != null)
                    {
                        string clickedText = itemText.text;
                        int optionIndex = activeDropdown.options.FindIndex(option => option.text == clickedText);
                        if (optionIndex != -1)
                        {
                            activeDropdown.value = optionIndex;
                            ExitDropdownMode(true);
                            return;
                        }
                    }
                }
            }
        }
    }

    private void SetCurrentElement(Component element)
    {
        currentSelectedElement = element;
        currentElementIndex = navigationElements.IndexOf(element);
        EventSystem.current.SetSelectedGameObject(element.gameObject);
    }

    private void HandleDropdownActivation(TMP_Dropdown dropdown)
    {
        if (!isDropdownExpanded)
        {
            isDropdownExpanded = true;
            activeDropdown = dropdown;
            savedDropdownValue = dropdown.value;
            dropdown.Show();
        }
    }

    private GameObject GetClickedObject()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0 ? results[0].gameObject : null;
    }

    private TMP_Dropdown.OptionData GetClickedDropdownOption(GameObject clickedObject)
    {
        TMP_Dropdown.OptionData clickedOption = null;
        var itemText = clickedObject.GetComponent<TMP_Text>();

        if (itemText != null && activeDropdown != null)
        {
            clickedOption = activeDropdown.options.FirstOrDefault(option =>
                option.text == itemText.text);
        }

        return clickedOption;
    }

    private void HandleMainNavigation()
    {
        if (navigationElements.Count == 2 && useHorizontalNavigation)
        {
            HandleTwoButtonNavigation();
        }
        else
        {
            HandleStandardNavigation();
        }

        HandleSelectionInput();
    }

    private void HandleTwoButtonNavigation()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            UpdateSelectedElement(0);
            if (navigationElements[0] is Button button)
            {
                HandleButtonPress(button);
            }
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            UpdateSelectedElement(1);
            if (navigationElements[1] is Button button)
            {
                HandleButtonPress(button);
            }
        }
    }

    private void HandleStandardNavigation()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            UpdateSelectedElement((currentElementIndex + 1) % navigationElements.Count);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            UpdateSelectedElement((currentElementIndex - 1 + navigationElements.Count) % navigationElements.Count);
        }
    }

    public void UpdateSelectedElement(int newIndex)
    {
        if (newIndex >= 0 && newIndex < navigationElements.Count)
        {
            currentElementIndex = newIndex;
            var element = navigationElements[currentElementIndex];
            if (element != null && element.gameObject != null && !element.Equals(null))
            {
                currentSelectedElement = element;
                EventSystem.current.SetSelectedGameObject(element.gameObject);
            }
        }
    }

    private void HandleSelectionInput()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (currentSelectedElement == null) return;

            if (currentSelectedElement is Button button && button.interactable)
            {
                HandleButtonPress(button);
            }
            else if (currentSelectedElement is TMP_Dropdown dropdown)
            {
                HandleDropdownActivation(dropdown);
            }
        }
    }

    private void HandleButtonPress(Button button)
    {
        if (!button.interactable) return;

        if (elementImages.TryGetValue(button, out Image image))
        {
            image.color = selectedColor;
        }

        PlayButtonSound();
        isProcessing = true;
        button.onClick.Invoke();
    }

    public void DisableAllButtons(int selectedIndex = -1)
    {
        isProcessing = true;
        for (int i = 0; i < navigationElements.Count; i++)
        {
            var element = navigationElements[i];
            if (element is Button button)
            {
                button.interactable = false;
                if (elementImages.TryGetValue(element, out Image image))
                {
                    // Keep selected color for the chosen button
                    if (i == selectedIndex)
                    {
                        image.color = selectedColor;
                    }
                    else
                    {
                        image.color = disabledColor;
                    }
                }
            }
        }
        ClearSelection();
    }

    public void EnableAllButtons()
    {
        isProcessing = false;
        foreach (var element in navigationElements)
        {
            if (element is Button button)
            {
                button.interactable = true;
                if (elementImages.TryGetValue(element, out Image image))
                {
                    image.color = normalColor;
                }
            }
        }
        ResetSelection();
    }

    public void ResetSelection()
    {
        ClearSelection();
        ResetButtonColors();
    }

    private void ClearSelection()
    {
        currentElementIndex = -1;
        currentSelectedElement = null;

        // Add null check for EventSystem
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
        else
        {
            Debug.LogWarning("EventSystem.current is null in ClearSelection()");
        }
    }
    private void ResetButtonColors()
    {
        foreach (var elementPair in elementImages)
        {
            if (elementPair.Value != null && elementPair.Value.gameObject != null)
            {
                var button = elementPair.Key as Button;
                elementPair.Value.color = (button != null && !button.interactable) ? disabledColor : normalColor;
            }
        }
    }

    // Public methods for managing navigation elements
    public void AddElement(Component element)
    {
        if (element != null && !navigationElements.Contains(element))
        {
            navigationElements.Add(element);
            if (element.TryGetComponent<Image>(out Image image))
            {
                elementImages[element] = image;
                image.color = normalColor;
            }
        }
    }

    public void ClearElements()
    {
        foreach (var element in navigationElements)
        {
            if (element != null)
            {
                if (elementImages.TryGetValue(element, out Image image))
                {
                    image.color = normalColor;
                }
            }
        }

        navigationElements.Clear();
        elementImages.Clear();
        currentElementIndex = -1;
        currentSelectedElement = null;
        EventSystem.current.SetSelectedGameObject(null);
        isProcessing = false;
    }


    private void HandleDropdownNavigation()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            if (activeDropdown.value < activeDropdown.options.Count - 1)
            {
                activeDropdown.value++;
            }
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            if (activeDropdown.value > 0)
            {
                activeDropdown.value--;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            ExitDropdownMode(true);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            activeDropdown.value = savedDropdownValue;
            ExitDropdownMode(false);
        }
    }

    private void ExitDropdownMode(bool confirmed)
    {
        if (!confirmed)
        {
            activeDropdown.value = savedDropdownValue;
        }
        activeDropdown.Hide();
        isDropdownExpanded = false;
        activeDropdown = null;
    }

    private void UpdateVisualFeedback()
    {
        foreach (var elementPair in elementImages)
        {
            if (elementPair.Value == null || elementPair.Value.gameObject == null) continue;

            var button = elementPair.Key as Button;
            if (button != null)
            {
                if (!button.interactable)
                {
                    elementPair.Value.color = disabledColor;
                }
                else if (elementPair.Key == currentSelectedElement)
                {
                    elementPair.Value.color = selectedColor;
                }
                else
                {
                    elementPair.Value.color = normalColor;
                }
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
}