using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class InstructionManager : MonoBehaviour
{
    /// <summary>
    /// This class manages the instructions for the game tour, including displaying instructions,
    /// </summary>

    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    // [SerializeField] private Button skipButton; 

    //     private string[] instructions = new string[]
    //      {
    // "1 Ahoy, brave explorer! You've been chosen for an extraordinary mission across two mysterious islands in The Motivation Expedition.",
    // "2 Each island presents unique challenges and mouthwatering rewards. Your task? Survive, thrive, and satisfy your fruity cravings!",
    // "3 Now, let's explore how to play.",
    // "4 On each island, you'll face different levels of effort to obtain the fruits. Some fruits are the easiest to gather. Others are quite challenging.",
    // "5 This is the grid-based island. Your character can move around here using the direction buttons (↑, ↓, ←, →) or WASD.",
    // "6 See that apple on the grid? That's your target! Each fruit you collect is worth 10 points. Your time is LIMITED since the fruit will disappear soon.",
    // "7 Your time on each island is also LIMITED, so you have to seize the chance. On each island, you'll face many decisions. Let's look at how you make decisions.",
    // "8 Use the '←' or '→' keys on your keyboard (or mouse) to select 'Work' and enter the island to collect fruit. Once inside, use the arrow keys to move your character. Note: if a fruit is difficult to reach, you may need to press the keys multiple times to take a single step.",
    // "9 After collecting the fruit, wait for the next opportunity to appear.",
    // "10 Or, you can choose to 'Skip', rest, and wait for the next opportunity. If you choose to 'Skip', you will earn 1 point for resting as well. Sometimes, this might be a good strategy.",
    // "11 You will have 2.5 seconds to choose; otherwise, you'll receive 0 point for that trial. Stay focused and don't miss any chances—or you'll have to wait for the next opportunity.",
    // "12 Your score will accumulate based on the fruits you collect. Each fruit has a base point value. Your total score will be displayed at the top of the screen. Try to maximize your score while managing your energy levels!",
    // "13 Remember, your goal is to explore the island, collect fruits, and decide which fruits are worth the effort. Your choices and strategies will provide valuable insights into decision-making processes.",
    // "14 The credits you earn will be converted into a bonus payment at the end. Your choices will remain completely anonymous and confidential.",
    // "15 If you're unsure about the instructions, feel free to press the 'Previous' button to read them again.",
    // "16 That's it! You're ready to start the practice trials. Good luck, and may the juiciest fruits be ever in your favor!"
    //     };

    // private string[] instructions = new string[]
    // {
    //     "1 Welcome to the Motivation Expedition, a strategic fruit-collecting adventure across two unique islands!",
    //     "2 Each island offers different challenges and rewards. Your mission is to navigate, collect fruits, and maximize your score.",
    //     "3 Now, let's explore how to play.",
    //     "4 You'll explore a grid-based island using direction buttons (↑, ↓, ←, →) or WASD keys",
    //     "5 Fruit offers will show up one-by-one, each worth 10 points.",
    //     "6 See that apple on the grid? That's your target! Time is limited, so act quickly to collect fruits before they disappear.",
    //     "7 Your time on each island is  LIMITED, so you have to seize the chance. On each island, you'll face many decisions. Let's look at how you make decisions.",
    //     "8 Use '←' or '→' keys to choose between 'Work' (collect fruit) or 'Skip' (rest). When working, use arrow keys to move your character. Some fruits require multiple steps.",
    //     "9 After collecting the fruit, wait for the next opportunity to appear.",
    //     "10 If you 'Skip', you'll earn 1 point for resting. Sometimes, this might be a good strategy.",
    //     "11 You have 2.5 seconds to make a decision. Delay results in 0 points. Stay focused and don't miss any chances—or you'll have to wait for the next opportunity.",
    //     "12 Your total score accumulates from fruit collection. Your total score will be displayed at the top of the screen. Try to maximize your score while managing your energy levels!",
    //     "13 Remember, your goal is to explore the island, collect fruits, and decide which fruits are worth the effort. Your choices and strategies will provide valuable insights into decision-making processes.",
    //     "14 Collected credits will be converted to a bonus payment. Your choices will remain completely anonymous and confidential.",
    //     "15 If you're unsure about the instructions, feel free to press the '←' button to read them again.",
    //     "16 That's it! You're ready to start with some practice. Good luck, and may the juiciest fruits be ever in your favor!"
    // };

    // private string[] instructions = new string[]
    // {
    //     "Welcome to the Motivation Expedition, a strategic fruit-collecting adventure across two unique islands!",
    //     "Each island presents different challenges and rewards. Your mission: navigate, collect fruits, maximize your score, and minimize the efforts.",
    //     "Now, let's explore how to play.",
    //     "You will decide whether to collect fruits. Each Fruit = 10 POINTS.",
    //     "You will use the keyboard to make your choice: Press 'A' to 'Work' (collect fruit) or 'D' to 'Skip' (rest). You will see an example on the next screen—no need to choose now.",
    //     "You have 2.5 seconds to decide — fail to choose in time, and you'll get 0 points plus a time penalty!",
    //     "If you choose WORK: A fruit will appear on the grid—your target! Time is limited, you will have to press quickly to collect fruits before they disappear.",
    //     "Use arrow keys to move your character. Some fruits are harder to reach, you may need to press the keys multiple times to take a single step.",
    //     "If you choose SKIP: Earn 0 points.",
    //     "Your time on each island is LIMITED, so sometimes it might be better to SKIP, if the fruit feels hard to reach.",
    //     "You will have to decide which fruits are worth collecting in this time limit.",
    //     "You will visit two different islands TWICE, with 5 minutes on each island each time. The islands will differ in how often you see different kinds of fruit.",
    //     "Remember, your goal is to explore the island, collect fruits, and decide which fruits are worth the effort. Each fruit is worth the same (10 points).",
    //     "Points will be converted to a bonus payment. Your choices will remain completely anonymous and confidential.",
    //     "If you're unsure about the instructions, feel free to press the LEFT '←' button to read them again.",
    //     "That's it! You're ready to start with some practice. Good luck, and may the juiciest fruits be ever in your favor!"
    // };

    // Suggested by Matt 2025/03/26
    private string[] instructions = new string[]
    {
    "Welcome to the Motivation Expedition, a strategic fruit-collecting adventure across two unique islands!",
    "Each island presents different challenges and rewards. Your mission: navigate, collect fruits, and maximize your score.",
    "Now, let's explore how to play.",
    "You will be collecting fruit on islands. Each fruit gives you points and increases your bonus payment! But collecting fruit requires effort. Try to maximize your score while minimizing your effort.",
    "Each fruit is worth 10 points, but some may require more effort (button presses) to reach than others. Each time you see a fruit, you must decide whether you want to work for it or skip it to find another.",
    "These decisions work as follows: Press 'A' to 'Work' (collect fruit) or 'D' to 'Skip' (rest). You will see an example on the next screen—no need to choose now.",
    "You have 2.5 seconds to decide—fail to choose in time, and you'll get 0 points plus a time penalty of 3 seconds!",
    "If you choose WORK: A fruit will appear on the grid—your target! Time is limited. You have 5 seconds to move your character and collect it. Press the direction buttons repetitively to navigate and reach the fruit before time runs out.",
    "Use the arrow keys to move your character. Some fruits are harder to reach, and you may need to press the keys multiple times to take a single step.",
    "If you choose SKIP: You earn 0 points but get to the next fruit faster.",
    "Your time on each island is LIMITED, so choose wisely which fruits are worth your effort.",
    "You must decide which fruits you would like to collect before time runs out.",
    "You will visit two different islands TWICE, spending 5 minutes on each island per visit. The islands will differ in how often you encounter different types of fruit.",
    "Remember, your goal is to explore the island, collect fruits, and decide which ones are worth the effort. Each fruit is worth the same (10 points).",
    "Points will be converted into a bonus payment. Your choices will remain completely anonymous and confidential.",
    "If you're unsure about the instructions, press the LEFT '←' button to read them again.",
    "That's it! You're all set to begin with some practice. Once you start, please don't quit the game midway. Make sure you have approximately 60 minutes available to finish the task in one go. Good luck, and may the juiciest fruits always be within your reach!"
    };


    private int currentInstructionIndex = 0;
    private bool isTransitioning = false;

    void Awake()
    {    // Make the InstructionManager persist across scene transitions
        DontDestroyOnLoad(gameObject);

        // Debug check
        if (instructionText == null)
        {
            Debug.LogError("InstructionManager: instructionText is null! Please assign in Inspector.");
        }
    }

    void Start()
    {
        // Initialize UI elements with null checks
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextInstruction);
        if (previousButton != null) previousButton.onClick.AddListener(ShowPreviousInstruction);

        Debug.Log("LoadScene GetReadyPractice from the GameTour");
        // if (skipButton != null) skipButton.onClick.AddListener(() => LoadScene("GetReadyPractice"));

        // Show first instruction
        UpdateInstructionDisplay();

        // Hide previous button on first instruction
        UpdateButtonStates();
    }

    void Update()
    {
        // Handle Space key for next instruction
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!isTransitioning)
            {
                if (currentInstructionIndex < instructions.Length - 1)
                {
                    ShowNextInstruction();
                }
                else
                {
                    EndTourAndLoadScene("GetReadyPractice");
                }
            }
        }

        // Handle Left Arrow key for previous instruction
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (!isTransitioning && currentInstructionIndex > 0)
            {
                ShowPreviousInstruction();
            }
        }
    }

    private void CleanupListeners()
    {
        if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextInstruction);
        if (previousButton != null) previousButton.onClick.RemoveListener(ShowPreviousInstruction);
        // if (skipButton != null) skipButton.onClick.RemoveAllListeners();
    }

    private void EndTourAndLoadScene(string sceneName)
    {
        // Clean up listeners first
        CleanupListeners();

        // Remove the ButtonNavigationController if it exists
        ButtonNavigationController navController = GetComponent<ButtonNavigationController>();
        if (navController != null)
        {
            navController.ClearElements();
            Destroy(navController);
        }

        // Load the next scene
        SceneManager.LoadScene(sceneName);

        // Destroy this GameObject after scene load
        Destroy(gameObject);
    }

    private void ShowNextInstruction()
    {
        if (isTransitioning) return;

        if (currentInstructionIndex == 5)
        {
            StartCoroutine(ShowDecisionPhaseDemo());
        }
        else if (currentInstructionIndex == 7)
        {
            StartCoroutine(ShowGridWorldDemo());
        }
        else if (currentInstructionIndex < instructions.Length - 1)
        {
            currentInstructionIndex++;
            UpdateInstructionDisplay();
        }
        else
        {
            EndTourAndLoadScene("GetReadyPractice");
        }

        UpdateButtonStates();
    }

    private void ShowPreviousInstruction()
    {
        if (isTransitioning || currentInstructionIndex <= 0) return;

        currentInstructionIndex--;
        UpdateInstructionDisplay();
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        // Add null checks for each button
        if (previousButton != null && previousButton.gameObject != null)
        {
            previousButton.gameObject.SetActive(false);
        }

        if (nextButton != null && nextButton.gameObject != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = !isTransitioning;
        }

        // if (skipButton != null && skipButton.gameObject != null)
        // {
        //     skipButton.gameObject.SetActive(true);
        // }
    }
    // private void UpdateInstructionDisplay()
    // {
    //     instructionText.text = instructions[currentInstructionIndex];
    // }


    private void UpdateInstructionDisplay()
    {
        // Find the TextMeshProUGUI component if it's null
        if (instructionText == null)
        {
            instructionText = GetComponent<TextMeshProUGUI>();

            // If still null, try finding in children
            if (instructionText == null)
            {
                instructionText = GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        // If still null after searching, log more detailed error
        if (instructionText == null)
        {
            Debug.LogError("InstructionManager: Cannot find TextMeshProUGUI component. " +
                "Ensure the script is on the correct GameObject and a TextMeshProUGUI exists.");
            return;
        }

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

        // Show different navigation instructions based on whether it's the first instruction
        if (currentInstructionIndex == 0)
        {
            instructionText.text += "\n\n<font=\"Electronic Highway Sign SDF\"><size=70%>Press 'Space' to continue</size></font>";
        }
        else
        {
            instructionText.text += "\n\n<font=\"Electronic Highway Sign SDF\"><size=70%>Press 'Space' to continue; ← to go back</size></font>";
        }

        // Update page indicator if available
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"Page {currentInstructionIndex + 1} of {instructions.Length}";
        }
    }

    private IEnumerator ShowGridWorldDemo()
    {
        isTransitioning = true;
        UpdateButtonStates();

        // Disable the audio listener in the main scene
        AudioListener mainSceneListener = FindAnyObjectByType<AudioListener>();
        mainSceneListener.enabled = false;

        // Hide the canvas elements before loading the demonstration scene
        welcomeText.gameObject.SetActive(false);
        instructionText.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        previousButton.gameObject.SetActive(false);
        // skipButton.gameObject.SetActive(false);
        pageIndicatorText.gameObject.SetActive(false);

        // Load GridWorld scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("TourGridWorld", LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Wait for 3 seconds
        yield return new WaitForSeconds(3f);

        // Unload GridWorld scene
        asyncLoad = SceneManager.UnloadSceneAsync("TourGridWorld");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Re-enable the audio listener in the main scene
        mainSceneListener.enabled = true;

        // Show the canvas elements after unloading the demonstration scene
        welcomeText.gameObject.SetActive(true);
        instructionText.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true);
        previousButton.gameObject.SetActive(true);
        // skipButton.gameObject.SetActive(true);
        pageIndicatorText.gameObject.SetActive(true);

        // Move to next instruction
        currentInstructionIndex++;
        UpdateInstructionDisplay();

        isTransitioning = false;
        UpdateButtonStates();
    }

    private IEnumerator ShowDecisionPhaseDemo()
    {
        isTransitioning = true;
        UpdateButtonStates();

        // Disable the audio listener in the main scene
        AudioListener mainSceneListener = FindAnyObjectByType<AudioListener>();
        mainSceneListener.enabled = false;

        // Hide the canvas elements before loading the demonstration scene
        welcomeText.gameObject.SetActive(false);
        instructionText.gameObject.SetActive(false);
        nextButton.gameObject.SetActive(false);
        previousButton.gameObject.SetActive(false);
        // skipButton.gameObject.SetActive(false);

        // Load Decision Phase scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("TourDecisionPhase", LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Wait for 3 seconds
        yield return new WaitForSeconds(3f);

        // Unload Decision Phase scene
        asyncLoad = SceneManager.UnloadSceneAsync("TourDecisionPhase");
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Re-enable the audio listener in the main scene
        mainSceneListener.enabled = true;

        // Show the canvas elements after unloading the demonstration scene
        welcomeText.gameObject.SetActive(true);
        instructionText.gameObject.SetActive(true);
        nextButton.gameObject.SetActive(true);
        previousButton.gameObject.SetActive(true);
        // skipButton.gameObject.SetActive(true);

        // Move to next instruction
        currentInstructionIndex++;
        UpdateInstructionDisplay();

        isTransitioning = false;
        UpdateButtonStates();
    }

    private void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}