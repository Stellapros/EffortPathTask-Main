using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Linq;
using TMPro;


public class InstructionManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button skipButton;

    private string[] instructions = new string[]
    {
        "1Ahoy, brave explorer! You've been chosen for an extraordinary mission across two mysterious islands in The Motivation Expedition.",
        "2Each island presents unique challenges and mouthwatering rewards. Your task? Survive, thrive, and satisfy your fruity cravings!",
        "3Now, let's explore how to play.",
        "4On each island, you'll face different levels of effort to obtain the fruits. Some fruits are the easiest to gather. Others are quite challenging.",
        "5This is the grid-based island. Your character can move around here using the direction buttons (↑, ↓, ←, →) or WASD.",
        "6See that apple on the grid? That's your target! Each fruit you collect is worth 10 points. Your time is LIMITED since the fruit will disappear soon.",
        "7Your time on each island is also LIMITED, so you have to seize the chance. On each island, you'll face many decisions. Let's look at how you make decisions.",
        "8Use the '←' or '→' keys on your keyboard (or mouse) to select 'Work' and enter the island to collect fruit. Once inside, use the arrow keys to move your character. Note: if a fruit is difficult to reach, you may need to press the keys multiple times to take a single step.",
        "9After collecting the fruit, wait for the next opportunity to appear.",
        "10Or, you can choose to 'Skip', rest, and wait for the next opportunity. If you choose to 'Skip', you will earn 1 point for resting. Sometimes, this might be a good strategy.",
        "11You will have 2.5 seconds to choose; otherwise, you'll receive 0 point for that trial. Stay focused and don't miss any chances—or you'll have to wait for the next opportunity.",
        "12Your score will accumulate based on the fruits you collect. Each fruit has a base point value. Your total score will be displayed at the top of the screen. Try to maximize your score while managing your energy levels!",
        "13Remember, your goal is to explore the island, collect fruits, and decide which fruits are worth the effort. Your choices and strategies will provide valuable insights into decision-making processes.",
        "14The credits you earn will be converted into a bonus payment at the end. Your choices will remain completely anonymous and confidential.",
        "15If you're unsure about the instructions, feel free to press the 'Previous' button to read them again.",
        "16That's it! You're ready to start the practice trials. Good luck, and may the juiciest fruits be ever in your favor!"
    };

    private int currentInstructionIndex = 0;
    private bool isTransitioning = false;
    private ButtonNavigationController navigationController;
    private bool isKeyPressed = false;
    private bool isProcessingInput = false;  //

    void Awake()
    {
        TryInitializeComponents();
        DontDestroyOnLoad(gameObject);
    }

    private void TryInitializeComponents()
    {
        // Find instruction text
        if (instructionText == null)
        {
            instructionText = GameObject.Find("InstructionText")?.GetComponent<TextMeshProUGUI>();
            if (instructionText == null)
            {
                instructionText = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.gameObject.name.Contains("Instruction"));
            }
        }

        // Find welcome text
        if (welcomeText == null)
        {
            welcomeText = GameObject.Find("WelcomeText")?.GetComponent<TextMeshProUGUI>();
            if (welcomeText == null)
            {
                welcomeText = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None)
                    .FirstOrDefault(t => t.gameObject.name.Contains("Welcome"));
            }
        }

        // Find buttons
        if (nextButton == null)
        {
            nextButton = GameObject.Find("NextButton")?.GetComponent<Button>();
            if (nextButton == null)
            {
                nextButton = FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.gameObject.name.Contains("Next"));
            }
        }

        if (previousButton == null)
        {
            previousButton = GameObject.Find("PreviousButton")?.GetComponent<Button>();
            if (previousButton == null)
            {
                previousButton = FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.gameObject.name.Contains("Previous"));
            }
        }

        if (skipButton == null)
        {
            skipButton = GameObject.Find("SkipButton")?.GetComponent<Button>();
            if (skipButton == null)
            {
                skipButton = FindObjectsByType<Button>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.gameObject.name.Contains("Skip"));
            }
        }

        LogComponentStatus();
    }


    private void LogComponentStatus()
    {
        if (instructionText == null)
            Debug.LogWarning("InstructionManager: Could not find InstructionText component!");
        if (welcomeText == null)
            Debug.LogWarning("InstructionManager: Could not find WelcomeText component!");
        if (nextButton == null)
            Debug.LogWarning("InstructionManager: Could not find NextButton component!");
        if (previousButton == null)
            Debug.LogWarning("InstructionManager: Could not find PreviousButton component!");
        if (skipButton == null)
            Debug.LogWarning("InstructionManager: Could not find SkipButton component!");
    }


    void Start()
    {
        // Only proceed if we have the minimum required components
        if (ValidateMinimumRequirements())
        {
            SetupButtons();
            UpdateInstructionDisplay();
            UpdateButtonStates();
            SetupNavigation();
        }
    }

    private bool ValidateMinimumRequirements()
    {
        // We absolutely need instructionText and nextButton to function
        if (instructionText == null)
        {
            // Debug.LogError("InstructionManager: InstructionText component is required but missing!");
            enabled = false;
            return false;
        }

        if (nextButton == null)
        {
            Debug.LogError("InstructionManager: NextButton component is required but missing!");
            enabled = false;
            return false;
        }

        return true;
    }

    private void SetupButtons()
    {
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(() =>
        {
            if (!isProcessingInput)
            {
                ShowNextInstruction();
            }
        });

        if (previousButton != null)
        {
            previousButton.onClick.RemoveAllListeners();
            previousButton.onClick.AddListener(() =>
            {
                if (!isProcessingInput)
                {
                    ShowPreviousInstruction();
                }
            });
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(() =>
            {
                if (!isProcessingInput)
                {
                    LoadScene("GetReadyPractice");
                }
            });
        }
    }

    private void SetupNavigation()
    {
        navigationController = gameObject.AddComponent<ButtonNavigationController>();
        navigationController.useHorizontalNavigation = true;
        UpdateNavigationElements();
    }

    private void UpdateNavigationElements()
    {
        if (navigationController == null) return;

        navigationController.ClearElements();

        if (currentInstructionIndex == 0)
        {
            if (skipButton != null) navigationController.AddElement(skipButton);
            if (nextButton != null) navigationController.AddElement(nextButton);
        }
        else
        {
            if (previousButton != null) navigationController.AddElement(previousButton);
            if (nextButton != null) navigationController.AddElement(nextButton);
        }
    }


    private void UpdateButtonStates()
    {
        if (!enabled) return;

        if (previousButton != null)
            previousButton.gameObject.SetActive(currentInstructionIndex > 0);

        if (nextButton != null)
            nextButton.interactable = !isTransitioning;

        if (skipButton != null)
            skipButton.gameObject.SetActive(currentInstructionIndex == 0);

        // Update navigation elements whenever button states change
        UpdateNavigationElements();
    }

    private void ShowNextInstruction()
    {
        if (isTransitioning || !enabled || isProcessingInput) return;

        isProcessingInput = true;  // 设置标志表示正在处理输入

        if (currentInstructionIndex == 4)
        {
            StartCoroutine(ShowGridWorldDemo());
        }
        else if (currentInstructionIndex == 6)
        {
            StartCoroutine(ShowDecisionPhaseDemo());
        }
        else if (currentInstructionIndex < instructions.Length - 1)
        {
            AdvanceToNextInstruction();
        }
        else if (currentInstructionIndex == instructions.Length - 1)
        {
            LoadScene("GetReadyPractice");
        }

        StartCoroutine(ResetInputFlag());  // 使用协程延迟重置输入标志
    }

    private IEnumerator ResetInputFlag()
    {
        yield return new WaitForSeconds(0.1f);  // 短暂延迟
        isProcessingInput = false;
        isKeyPressed = false;
    }

    private void AdvanceToNextInstruction()
    {
        currentInstructionIndex++;
        UpdateInstructionDisplay();
        UpdateButtonStates();
    }

    private void ShowPreviousInstruction()
    {
        if (isTransitioning || currentInstructionIndex <= 0) return;

        currentInstructionIndex--;
        UpdateInstructionDisplay();
        UpdateButtonStates();
    }


    private void UpdateInstructionDisplay()
    {
        if (instructionText == null)
        {
            Debug.LogError("InstructionManager: instructionText is null!");
            return;
        }

        if (currentInstructionIndex < 0 || currentInstructionIndex >= instructions.Length)
        {
            Debug.LogError($"InstructionManager: Invalid instruction index: {currentInstructionIndex}");
            return;
        }

        instructionText.text = instructions[currentInstructionIndex];
        instructionText.text += "\n\n<size=70%>Use ←/→ to navigate buttons, Space/Enter to confirm</size>";
    }

    private IEnumerator ShowGridWorldDemo()
    {
        isTransitioning = true;
        UpdateButtonStates();

        AudioListener mainSceneListener = FindAnyObjectByType<AudioListener>();
        mainSceneListener.enabled = false;

        // Hide UI elements
        SetUIElementsActive(false);

        // Load and show demo
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("TourGridWorld", LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(3f);

        // Unload demo
        asyncLoad = SceneManager.UnloadSceneAsync("TourGridWorld");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        mainSceneListener.enabled = true;

        // Show UI elements
        SetUIElementsActive(true);

        // Advance to next instruction
        AdvanceToNextInstruction();

        isTransitioning = false;
        UpdateButtonStates();
    }

    private IEnumerator ShowDecisionPhaseDemo()
    {
        isTransitioning = true;
        UpdateButtonStates();

        AudioListener mainSceneListener = FindAnyObjectByType<AudioListener>();
        mainSceneListener.enabled = false;

        // Hide UI elements
        SetUIElementsActive(false);

        // Load and show demo
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("TourDecisionPhase", LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(3f);

        // Unload demo
        asyncLoad = SceneManager.UnloadSceneAsync("TourDecisionPhase");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        mainSceneListener.enabled = true;

        // Show UI elements
        SetUIElementsActive(true);

        // Advance to next instruction
        AdvanceToNextInstruction();

        isTransitioning = false;
        UpdateButtonStates();
    }

    private void SetUIElementsActive(bool active)
    {
        if (!enabled) return;

        if (welcomeText != null)
            welcomeText.gameObject.SetActive(active);

        if (instructionText != null)
            instructionText.gameObject.SetActive(active);

        if (nextButton != null)
            nextButton.gameObject.SetActive(active);

        if (previousButton != null)
            previousButton.gameObject.SetActive(active && currentInstructionIndex > 0);

        if (skipButton != null)
            skipButton.gameObject.SetActive(active && currentInstructionIndex == 0);
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    void Update()
    {
        if (!enabled || isProcessingInput) return;

        // 处理键盘输入
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (!isKeyPressed && !isTransitioning)
            {
                isKeyPressed = true;
                ShowNextInstruction();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (!isKeyPressed && !isTransitioning)
            {
                isKeyPressed = true;
                ShowPreviousInstruction();
            }
        }
    }

    void OnDestroy()
    {
        // 清理事件监听
        if (nextButton != null) nextButton.onClick.RemoveAllListeners();
        if (previousButton != null) previousButton.onClick.RemoveAllListeners();
        if (skipButton != null) skipButton.onClick.RemoveAllListeners();
    }
}