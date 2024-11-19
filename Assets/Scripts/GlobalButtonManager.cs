using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TMPro;

public class ButtonNavigationController : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private List<Button> navigationButtons = new List<Button>();

    [Header("Button Colors")]
    // [SerializeField] private Color normalColor = Color.white;
    // [SerializeField] private Color selectedColor = new Color(0.9f, 0.9f, 1f); // Light blue tint
    // [SerializeField] private Color pressedColor = new Color(0.8f, 0.8f, 1f); // Darker blue tint
    [SerializeField] private Color normalColor = new Color(0.67f, 0.87f, 0.86f);  // #aadedc
    // [SerializeField] private Color selectedColor = new Color(0.53f, 0.73f, 0.72f); // #88bab8 (darker version)
    [SerializeField] private Color selectedColor = new Color(0.87f, 0.86f, 0.67f);
    // [SerializeField] private Color selectedColor = new Color(0.67f, 0.87f, 0.86f); // #dedcaa
    [SerializeField] private Color pressedColor = new Color(0.87f, 0.86f, 0.67f); // #dedcaa

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;

    private AudioSource audioSource;
    private int currentButtonIndex = 0;
    private Button currentSelectedButton;
    private Dictionary<Button, Image> buttonImages = new Dictionary<Button, Image>();
    private bool isPressed = false;

    private void Start()
    {
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();

        // Cache button images and setup initial colors
        foreach (Button button in navigationButtons)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImages[button] = buttonImage;
                buttonImage.color = normalColor;
            }
        }

        // Set initial selection
        if (navigationButtons.Count > 0)
        {
            SetSelectedButton(navigationButtons[0]);
        }
    }

    private void Update()
    {
        if (navigationButtons.Count == 0) return;

        HandleKeyboardNavigation();
        UpdateVisualFeedback();
    }

    private void HandleKeyboardNavigation()
    {
        // Horizontal navigation (left/right arrows)
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            currentButtonIndex = (currentButtonIndex + 1) % navigationButtons.Count;
            SetSelectedButton(navigationButtons[currentButtonIndex]);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            currentButtonIndex = (currentButtonIndex - 1 + navigationButtons.Count) % navigationButtons.Count;
            SetSelectedButton(navigationButtons[currentButtonIndex]);
        }

        // Vertical navigation (up/down arrows)
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            currentButtonIndex = (currentButtonIndex + 1) % navigationButtons.Count;
            SetSelectedButton(navigationButtons[currentButtonIndex]);
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            currentButtonIndex = (currentButtonIndex - 1 + navigationButtons.Count) % navigationButtons.Count;
            SetSelectedButton(navigationButtons[currentButtonIndex]);
        }

        // Selection with Space or Return
        if (currentSelectedButton != null)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                isPressed = true;
                if (buttonImages.ContainsKey(currentSelectedButton))
                {
                    buttonImages[currentSelectedButton].color = pressedColor;
                }
            }
            else if ((Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.Return)) && isPressed)
            {
                isPressed = false;
                PlayButtonSound();
                currentSelectedButton.onClick.Invoke();
            }
        }
    }

    private void SetSelectedButton(Button button)
    {
        currentSelectedButton = button;
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private void UpdateVisualFeedback()
    {
        foreach (var buttonPair in buttonImages)
        {
            Button button = buttonPair.Key;
            Image buttonImage = buttonPair.Value;

            if (button == currentSelectedButton)
            {
                buttonImage.color = isPressed ? pressedColor : selectedColor;
            }
            else
            {
                buttonImage.color = normalColor;
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

    // Public method to add buttons to navigation
    public void AddButton(Button button)
    {
        if (!navigationButtons.Contains(button))
        {
            navigationButtons.Add(button);
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImages[button] = buttonImage;
                buttonImage.color = normalColor;
            }
        }
    }

    // Public method to remove buttons from navigation
    public void RemoveButton(Button button)
    {
        if (navigationButtons.Contains(button))
        {
            navigationButtons.Remove(button);
            buttonImages.Remove(button);

            if (currentSelectedButton == button)
            {
                currentButtonIndex = 0;
                if (navigationButtons.Count > 0)
                {
                    SetSelectedButton(navigationButtons[0]);
                }
            }
        }
    }
}