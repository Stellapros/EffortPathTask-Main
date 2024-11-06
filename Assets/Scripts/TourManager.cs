using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

public class TourManager : MonoBehaviour
{
    public static TourManager Instance { get; private set; }
    public event Action OnTourCompleted;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI welcomeText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipTourButton;
    [SerializeField] private Image highlightOverlay;
    [SerializeField] private GameObject pointerArrow;

    [Header("Visual Settings")]
    [SerializeField] private float fadeSpeed = 0.5f;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0f, 0.3f);
    [SerializeField] private float arrowBobSpeed = 2f;
    [SerializeField] private float arrowBobAmount = 50f;

    private TourStep[] tourSteps;
    private int currentStepIndex = -1;
    private bool isCalibrationSceneLoaded = false;
    private bool isTourActive = false;
    private bool isTransitioning = false;
    private Coroutine highlightCoroutine;
    private Coroutine arrowCoroutine;

    [System.Serializable]
    public class TourStep
    {
        public string instruction;
        public string targetElementName;
        public string sceneToLoad;
        public TourAction action;
        public float waitTime;
        public bool requiresNextButton;
    }

    public enum TourAction
    {
        None,
        WaitForFruitCollection,
        HighlightWorkButton,
        HighlightSkipButton,
        WaitForSkip
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeTourSteps();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SetupButtons();
            Debug.Log("TourManager initialized successfully");
        }
        else if (Instance != this)
        {
            Debug.Log("Destroying duplicate TourManager instance");
            Destroy(gameObject);
        }
    }

    // private string[] tourSteps = new string[]
    // {
    //     "Welcome to the Game Tour! Let's explore how to play.",
    //     "Your goal is to collect fruits in this grid-based world.",
    //     "Each fruit you collect is worth 10 points.",
    //     "This is the grid world. Your character can move around here.",
    //     "See that apple on the grid? That's your target!",
    //     "Now, let's look at how you make decisions.",
    //     "You can choose to 'Work' and enter the grid world to collect the fruit.",
    //     "Or you can 'Skip' and wait for the next opportunity.",
    //     "Let's try pressing 'Work' to enter the grid world.",
    //     "Great! Now try moving your character to collect the fruit.",
    //     "Excellent! You've collected the fruit and earned 10 points.",
    //     "Now, let's try skipping. Press the 'Skip' button.",
    //     "You've skipped this round. Sometimes, this might be a good strategy.",
    //     "That's it! You're ready to start the practice trials. Good luck!"
    // };

    private void InitializeTourSteps()
    {
        tourSteps = new TourStep[]
        {
                new TourStep {
                instruction = "Welcome to the Game Tour! Let's explore how to play.",
                action = TourAction.None,
                waitTime = 0f,
                requiresNextButton = true
            },

            // Initial GridWorld scene - Show the GridWorld
            new TourStep {
                instruction = "Let's try to get the fruit shown on the grid. This is the grid world. Your character can move around here.",
                sceneToLoad = "TourGridWorld",
                action = TourAction.None,
                waitTime = 0f,
                requiresNextButton = false
            },
            
            // Wait for fruit collection
            new TourStep {
                instruction = "See that apple on the grid? That's your target! Each fruit you collect is worth 10 points.",
                action = TourAction.WaitForFruitCollection,
                waitTime = 0f,
                requiresNextButton = false
            },

            // Move to Decision Phase
            new TourStep {
                instruction = "Now, let's look at how you make decisions.",
                sceneToLoad = "TourDecisionPhase",
                action = TourAction.None,
                waitTime = 1f,
                requiresNextButton = false
            },

            // Work Button Tutorial
            new TourStep {
                instruction = "Click 'Work' to enter the grid world and collect the fruit.",
                targetElementName = "WorkButton",
                action = TourAction.HighlightWorkButton,
                waitTime = 0f,
                requiresNextButton = false
            },

            // Skip Button Tutorial
            new TourStep {
                instruction = "Sometimes you might want to skip. Click 'Skip' to try it.",
                targetElementName = "SkipButton",
                action = TourAction.HighlightSkipButton,
                waitTime = 0f,
                requiresNextButton = false
            },

            // Wait during skip
            new TourStep {
                instruction = "Please wait for 3 seconds...",
                action = TourAction.WaitForSkip,
                waitTime = 3f,
                requiresNextButton = false
            },

            // Skip Done
            new TourStep {
                instruction = "Great job! You've skipped this round. Sometimes, this might be a good strategy. You've completed the tutorial. Ready to start the practice?",
                action = TourAction.None,
                waitTime = 0f,
                requiresNextButton = false
            },
            
            // Tour Completed
            new TourStep {
                instruction = "You've completed the tutorial. Ready to start the practice?",
                action = TourAction.None,
                waitTime = 0f,
                requiresNextButton = true
            }
        };
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}. Current tour state - isTourActive: {isTourActive}, currentStepIndex: {currentStepIndex}");

        if (scene.name == "CalibrationCounter")
        {
            isCalibrationSceneLoaded = true;
            Debug.Log("CalibrationCounter scene loaded, checking if tour can start...");
        }
        else
        {
            isCalibrationSceneLoaded = false;
        }

        // Ensure UI is properly shown after scene load if tour is active
        if (isTourActive)
        {
            StartCoroutine(SetupAfterSceneLoad());
        }
    }

    private IEnumerator SetupAfterSceneLoad()
    {
        yield return new WaitForEndOfFrame();
        if (isTourActive && !isTransitioning)
        {
            ShowUI(true);
            ExecuteCurrentStep();
        }
    }

    public void StartTour()
    {
        Debug.Log($"[TourManager] StartTour explicitly called. Current state - Scene: {SceneManager.GetActiveScene().name}");

        // Force reset any stuck states
        isTransitioning = false;

        // Set active and initialize
        isTourActive = true;
        currentStepIndex = -1;

        Debug.Log($"[TourManager] Tour initialized. isTourActive: {isTourActive}, currentStepIndex: {currentStepIndex}");

        // Show UI elements
        ShowUI(true);

        // Force the first step
        Debug.Log("[TourManager] Forcing first step...");
        ProcessNextStep();
    }

    private void SetupButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() =>
            {
                // Force activate tour if needed
                if (!isTourActive)
                {
                    StartTour();
                }
                else
                {
                    ProcessNextStep();
                }
            });
        }
        else
        {
            Debug.LogError("[TourManager] Next button reference missing!");
        }

        if (skipTourButton != null)
        {
            skipTourButton.onClick.RemoveAllListeners();
            skipTourButton.onClick.AddListener(EndTour);
        }
    }

    public void ProcessNextStep()
    {
        Debug.Log($"[TourManager] ProcessNextStep called. State - isTourActive: {isTourActive}, isTransitioning: {isTransitioning}, currentStepIndex: {currentStepIndex}");

        // Force active if somehow got deactivated
        if (!isTourActive)
        {
            Debug.LogWarning("[TourManager] Tour was inactive! Force activating...");
            isTourActive = true;
        }
        // Ensure we're not stuck in transition
        if (isTransitioning)
        {
            Debug.LogWarning("[TourManager] Found stuck in transition! Force clearing...");
            isTransitioning = false;
        }
        currentStepIndex++;
        Debug.Log($"[TourManager] Moving to step {currentStepIndex}");

        if (currentStepIndex >= tourSteps.Length)
        {
            Debug.Log("[TourManager] Tour completed, calling EndTour()");
            EndTour();
            return;
        }
        ExecuteCurrentStep();
    }

    private void ExecuteCurrentStep()
    {
        if (!isTourActive || isTransitioning)
        {
            Debug.Log("Tour is not active or is in transition, ignoring ExecuteCurrentStep()");
            return;
        }

        TourStep step = tourSteps[currentStepIndex];

        // Update instruction text
        if (instructionText != null)
        {
            instructionText.text = step.instruction;
        }

        // Show/hide next button based on step requirements
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(step.requiresNextButton);
        }

        // Handle scene loading if needed
        if (!string.IsNullOrEmpty(step.sceneToLoad))
        {
            isTransitioning = true;
            Debug.Log($"Loading scene: {step.sceneToLoad}");
            StartCoroutine(LoadSceneAndExecuteAction(step));
        }
        else
        {
            Debug.Log($"Executing current step action: {step.action}");
            StartCoroutine(ExecuteStepAction(step));
        }
    }

    private IEnumerator LoadSceneAndExecuteAction(TourStep step)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(step.sceneToLoad);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        isTransitioning = false;

        Debug.Log($"Scene loaded, executing step action: {step.action}");
        StartCoroutine(ExecuteStepAction(step));
    }

    private IEnumerator ExecuteStepAction(TourStep step)
    {
        switch (step.action)
        {
            case TourAction.WaitForFruitCollection:
                yield return StartCoroutine(WaitForFruit());
                break;

            case TourAction.HighlightWorkButton:
                HighlightElement(step.targetElementName);
                yield return StartCoroutine(WaitForButtonClick(step.targetElementName));
                break;

            case TourAction.HighlightSkipButton:
                HighlightElement(step.targetElementName);
                yield return StartCoroutine(WaitForButtonClick(step.targetElementName));
                break;

            case TourAction.WaitForSkip:
                yield return new WaitForSeconds(step.waitTime);
                ProcessNextStep();
                break;

            case TourAction.None:
                if (!step.requiresNextButton && step.waitTime > 0)
                {
                    yield return new WaitForSeconds(step.waitTime);
                    ProcessNextStep();
                }
                break;
        }
    }

    private void HighlightElement(string elementName)
    {
        if (string.IsNullOrEmpty(elementName) || highlightOverlay == null) return;

        GameObject element = GameObject.Find(elementName);
        if (element == null) return;

        RectTransform elementRect = element.GetComponent<RectTransform>();
        if (elementRect == null) return;

        highlightOverlay.gameObject.SetActive(true);
        highlightOverlay.rectTransform.position = elementRect.position;
        highlightOverlay.rectTransform.sizeDelta = elementRect.sizeDelta * 1.2f;

        if (pointerArrow != null)
        {
            pointerArrow.SetActive(true);
            pointerArrow.GetComponent<RectTransform>().position =
                elementRect.position + Vector3.up * 100f;
        }
    }

    private IEnumerator WaitForFruit()
    {
        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Fruit") == null);
        ProcessNextStep();
    }

    private IEnumerator WaitForButtonClick(string buttonName)
    {
        GameObject buttonObj = GameObject.Find(buttonName);
        if (buttonObj == null) yield break;

        Button button = buttonObj.GetComponent<Button>();
        if (button == null) yield break;

        bool clicked = false;
        UnityEngine.Events.UnityAction action = () => clicked = true;

        button.onClick.AddListener(action);
        yield return new WaitUntil(() => clicked);
        button.onClick.RemoveListener(action);
        ProcessNextStep();
    }

    private void ShowUI(bool show)
    {
        if (welcomeText != null) welcomeText.gameObject.SetActive(show);
        if (instructionText != null) instructionText.gameObject.SetActive(show);
        if (skipTourButton != null) skipTourButton.gameObject.SetActive(show);
        if (highlightOverlay != null) highlightOverlay.gameObject.SetActive(false);
        if (pointerArrow != null) pointerArrow.gameObject.SetActive(false);

        if (!show && nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
        }
    }

    private void ClearHighlights()
    {
        if (highlightOverlay != null) highlightOverlay.gameObject.SetActive(false);
        if (pointerArrow != null) pointerArrow.gameObject.SetActive(false);
    }

    public void EndTour()
    {
        isTourActive = false;
        ClearHighlights();
        ShowUI(false);
        Debug.Log("Tour has ended");
        OnTourCompleted?.Invoke();
        SceneManager.LoadScene("GetReadyPractise");
    }

    public bool IsTourActive() => isTourActive;
    public int GetCurrentStepIndex() => currentStepIndex;

}
