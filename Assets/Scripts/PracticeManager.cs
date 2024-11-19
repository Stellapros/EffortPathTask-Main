using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class PracticeManager : MonoBehaviour
{
    public static PracticeManager Instance { get; private set; }

    [Header("Practice Configuration")]
    [SerializeField] private int numberOfPracticeTrials = 3;
    [SerializeField] private float practiceTrialDuration = 15f;
    [SerializeField] private string practiceSceneName = "GetReadyPractice";
    [SerializeField] private string experimentSceneName = "DecisionPhase";

    [Header("UI Elements")]
    [SerializeField] private GameObject practiceUI;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private Button startPracticeButton;
    [SerializeField] private Button continueButton;

    [Header("Audio")]
    [SerializeField] private AudioClip buttonClickSound;
    private AudioSource audioSource;

    private int currentPracticeTrialIndex = -1;
    private bool isPracticeMode = false;
    private ExperimentManager experimentManager;
    private GridWorldManager gridWorldManager;
    private DecisionManager decisionManager;

    public event Action OnPracticeCompleted;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeAudio();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeAudio()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = buttonClickSound;
    }

    private void Start()
    {
        InitializeComponents();
        SetupEventListeners();
    }

    private void InitializeComponents()
    {
        experimentManager = ExperimentManager.Instance;
        gridWorldManager = GridWorldManager.Instance;
        decisionManager = FindAnyObjectByType<DecisionManager>();

        if (startPracticeButton != null)
            startPracticeButton.onClick.AddListener(StartPracticeMode);
        
        if (continueButton != null)
            continueButton.onClick.AddListener(ContinueToNextScreen);
    }

    private void SetupEventListeners()
    {
        if (gridWorldManager != null)
            gridWorldManager.OnTrialEnded += HandlePracticeTrialCompletion;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == practiceSceneName)
        {
            ShowPracticeUI();
            UpdateUIElements();
        }
    }

    public void StartPracticeMode()
    {
        PlayButtonSound();
        isPracticeMode = true;
        currentPracticeTrialIndex = 0;
        SetupPracticeTrial();
    }

    private void SetupPracticeTrial()
    {
        if (currentPracticeTrialIndex >= numberOfPracticeTrials)
        {
            EndPracticeMode();
            return;
        }

        UpdateUIElements();
        InitializeGridWorld();
    }

    private void UpdateUIElements()
    {
        UpdateProgressUI();
        UpdateInstructions();
    }

    private void UpdateProgressUI()
    {
        if (progressText != null)
        {
            progressText.text = $"Practice Trial {currentPracticeTrialIndex + 1} of {numberOfPracticeTrials}";
        }
    }

    private void UpdateInstructions()
    {
        string[] instructions = {
            "Move with arrow keys or W-A-S-D keys to collect the fruit. Take your time in practice mode.",
            "Now try deciding between Work and Skip. Think about effort versus reward.",
            "Final practice! Apply what you've learned about navigation and decision-making."
        };

        if (instructionText != null && currentPracticeTrialIndex < instructions.Length)
        {
            instructionText.text = instructions[currentPracticeTrialIndex];
        }
    }

    private void InitializeGridWorld()
    {
        if (gridWorldManager != null)
        {
            gridWorldManager.InitializeGridWorld(practiceTrialDuration);
        }
    }

    public void HandlePracticeTrialCompletion(bool success)
    {
        string feedback = success ? 
            "Excellent work! You've successfully collected the fruit!" : 
            "Keep practicing! Remember to plan your route and reach the fruit before time runs out.";
        
        StartCoroutine(ShowFeedbackAndContinue(feedback));
    }

    private IEnumerator ShowFeedbackAndContinue(string feedback)
    {
        if (feedbackPanel != null && feedbackText != null)
        {
            feedbackPanel.SetActive(true);
            feedbackText.text = feedback;
            yield return new WaitForSeconds(3f);
            feedbackPanel.SetActive(false);
        }

        currentPracticeTrialIndex++;
        SetupPracticeTrial();
    }

    public void ContinueToNextScreen()
    {
        PlayButtonSound();
        SceneManager.LoadScene(practiceSceneName);
    }

    private void EndPracticeMode()
    {
        isPracticeMode = false;
        HidePracticeUI();
        OnPracticeCompleted?.Invoke();
        
        if (experimentManager != null)
        {
            PlayButtonSound();
            SceneManager.LoadScene(experimentSceneName);
            experimentManager.StartFormalExperiment();
        }
    }

    private void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    private void ShowPracticeUI()
    {
        if (practiceUI != null)
        {
            practiceUI.SetActive(true);
        }
    }

    private void HidePracticeUI()
    {
        if (practiceUI != null)
        {
            practiceUI.SetActive(false);
        }
    }

    // Public utility methods
    public bool IsPracticeTrial() => isPracticeMode;
    public int GetCurrentPracticeTrialIndex() => currentPracticeTrialIndex;
    public int GetTotalPracticeTrials() => numberOfPracticeTrials;

    private void OnDestroy()
    {
        if (gridWorldManager != null)
            gridWorldManager.OnTrialEnded -= HandlePracticeTrialCompletion;
        
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}