using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class InstructionManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;
    [SerializeField] private Button skipButton;

    // private string[] instructions = new string[]
    // {
    //     "Ahoy, brave explorer! You've been chosen for an extraordinary mission across two mysterious islands in The Motivation Expedition. Each island presents unique challenges and mouthwatering rewards. Your task? Survive, thrive, and satisfy your fruity cravings!",
    //     "Now, let's explore how to play.",
    //     "On each island, you'll face different levels of effort to obtain the fruits. Some fruits are the easiest to gather. Some are quite challenging.",
    //     "This is the grid-based island. Your character can move around here using direction buttons (↑ or ↓ or ← or →) or WASD.",
    //     "See that apple on the grid? That's your target! Each fruit you collect is worth 10 points. Your time is LIMITED since the fruit will disappear soon.",
    //     "Your time on each island is also LIMITED, so you have to seize the chance. On each island, you'll face many decisions. Let's look at how you make decisions.",
    //     "You can choose to 'Work' and enter the island to collect the fruit. If you choose to work, use the direction keys on your keyboard to move your character. If a fruit is hard to reach, you will have to press multiple times to move one step.",
    //     "Or, you can 'Skip', rest, and wait for the next opportunity. If you choose to 'Skip', you can rest for 1 point. Sometimes, this might be a good strategy.",
    //     "You will have 2.5 seconds to choose; otherwise, 0 points will be given for that trial. Make sure you are focused and do not miss any chance, or you will have to wait until the next chance shows up.",
    //     "Your score will accumulate based on the fruits you collect: Each fruit has a base point value. Your total score will be displayed at the top of the screen. Try to maximize your score while managing your energy levels!",
    //     "Remember, your goal is to explore both islands, collect fruits, and make decisions about which fruits are worth the effort. Your choices and strategies will provide valuable insights into decision-making processes.",
    //     "The credits you earn will be converted into a bonus payment at the end. Your choices will be completely anonymous and confidential.",
    //     "If you are unsure about the instruction, feel free to press the 'Previous' button to read again.",
    //     "That's it! You're ready to start the practice trials. Good luck, and may the juiciest fruits be ever in your favor!"
    // };

    private string[] instructions = new string[]
        {
            "Ahoy, brave explorer! You've been chosen for an extraordinary mission across two mysterious islands in The Motivation Expedition.", 
            "Each island presents unique challenges and mouthwatering rewards. Your task? Survive, thrive, and satisfy your fruity cravings!",
            "Now, let's explore how to play.",
            "On each island, you'll face different levels of effort to obtain the fruits. Some fruits are the easiest to gather. Others are quite challenging.",
            "This is the grid-based island. Your character can move around here using the direction buttons (↑, ↓, ←, →) or WASD.",
            "See that apple on the grid? That's your target! Each fruit you collect is worth 10 points. Your time is LIMITED since the fruit will disappear soon.",
            "Your time on each island is also LIMITED, so you have to seize the chance. On each island, you'll face many decisions. Let's look at how you make decisions.",
            "Use the '←' or '→' keys on your keyboard (or mouse) to select 'Work' and enter the island to collect fruit. Once inside, use the arrow keys to move your character. Note: if a fruit is difficult to reach, you may need to press the keys multiple times to take a single step.",
            "After collecting the fruit, wait for the next opportunity to appear.",
            "Or, you can choose to 'Skip', rest, and wait for the next opportunity. If you choose to 'Skip', you will earn 1 point for resting. Sometimes, this might be a good strategy.",
            "You will have 2.5 seconds to choose; otherwise, you'll receive 0 points for that trial. Stay focused and don't miss any chances—or you'll have to wait for the next opportunity.",
            "Your score will accumulate based on the fruits you collect. Each fruit has a base point value. Your total score will be displayed at the top of the screen. Try to maximize your score while managing your energy levels!",
            "Remember, your goal is to explore both islands, collect fruits, and decide which fruits are worth the effort. Your choices and strategies will provide valuable insights into decision-making processes.",
            "The credits you earn will be converted into a bonus payment at the end. Your choices will remain completely anonymous and confidential.",
            "If you're unsure about the instructions, feel free to press the 'Previous' button to read them again.",
            "That's it! You're ready to start the practice trials. Good luck, and may the juiciest fruits be ever in your favor!"
        };

    private int currentInstructionIndex = 0;
    private bool isTransitioning = false;

    void Awake()
    {    // Make the InstructionManager persist across scene transitions
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // Initialize UI elements
        nextButton.onClick.AddListener(ShowNextInstruction);
        previousButton.onClick.AddListener(ShowPreviousInstruction);
        skipButton.onClick.AddListener(() => LoadScene("GetReadyPractice"));

        // Show first instruction
        UpdateInstructionDisplay();

        // Hide previous button on first instruction
        UpdateButtonStates();

        // Add in Start() method
ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
navigationController.AddElement(previousButton);
navigationController.AddElement(nextButton);
navigationController.AddElement(skipButton);
    }

    private void UpdateButtonStates()
    {
        previousButton.gameObject.SetActive(currentInstructionIndex > 0);
        nextButton.interactable = !isTransitioning;
    }

    private void ShowNextInstruction()
    {
        if (isTransitioning) return;

        if (currentInstructionIndex == 3) // Grid World demonstration
        {
            StartCoroutine(ShowGridWorldDemo());
        }
        else if (currentInstructionIndex == 6) // Decision Phase demonstration
        {
            StartCoroutine(ShowDecisionPhaseDemo());
        }
        else if (currentInstructionIndex < instructions.Length - 1)
        {
            currentInstructionIndex++;
            UpdateInstructionDisplay();
        }
        else
        {
            LoadScene("GetReadyPractice");
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

    private void UpdateInstructionDisplay()
    {
        instructionText.text = instructions[currentInstructionIndex];
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
        skipButton.gameObject.SetActive(false);

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
        skipButton.gameObject.SetActive(true);

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
        skipButton.gameObject.SetActive(false);

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
        skipButton.gameObject.SetActive(true);

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