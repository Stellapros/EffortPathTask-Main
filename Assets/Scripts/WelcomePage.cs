using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;

public class WelcomePage : MonoBehaviour
{
    [SerializeField] private Button continueButton;
    [SerializeField] private string nextSceneName = "StartScreen";
    [SerializeField] private string quitSceneName = "EndExperiment";
    [SerializeField] public AudioClip buttonClickSound;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI pageIndicatorText;
    private AudioSource audioSource;

    [TextArea(5, 10)]
    [SerializeField] private string[] consentPages;

    private int currentPageIndex = 0;
    private bool isLastPage = false;

    private void Start()
    {
        // Initialize audio source if needed
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Set up the continue button
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueButtonClick);
            continueButton.gameObject.SetActive(false); // Hide initially
        }
        else
        {
            Debug.LogError("Continue button not assigned in WelcomePage!");
        }

        // If consentPages is empty, initialize it with the default text split by consent items
        if (consentPages == null || consentPages.Length == 0)
        {
            // string defaultConsentText =
            // "First, please MAXIMIZE the game screen by clicking the [ ] icon in the bottom right corner if you haven't already. \n\n" +
            // "We are interested in understanding how people learn and make decisions. These experiments are being conducted by Prof. Matthew Apps of the University of Birmingham, England. He and his team can be contacted by e-mail at m.li.14@bham.ac.uk.\n\n" +
            // "During this study, you will be required to make decisions by clicking on buttons and answering a series of questions about your mood, personality, and demographic information after the task. You have the right to withdraw at any time by closing the browser. Your data will be stored pseudo-anonymously and separately from any personal data.\n\n" +
            // "Please read the following, as it indicates that you are giving informed consent to participate in a psychological experiment:\n\n" +
            // "□ I confirm that I have read and understand the information sheet for this study.\n" +
            // "□ I have had the opportunity to consider the information, ask questions, and receive satisfactory answers (if you still have questions about the research, please contact the experimenter directly).\n" +
            // "□ I understand that my participation is voluntary and that I am free to withdraw at any time without providing a reason.\n" +
            // "□ I understand that once my data have been included in the analysis (approximately one month after collection), I will no longer be able to withdraw my data.\n" +
            // "□ I understand that my data will be treated confidentially, and any publication resulting from this work will only report data that does not identify me.\n" +
            // "□ My anonymized responses may be shared with other researchers or made available in online data repositories.\n" +
            // "□ I am over 18 years old and freely agree to participate in this study.\n\n" +

            // "This information is being collected as part of a research project on decision-making conducted by the School of Psychology at the University of Birmingham.\n" +
            // "The information you provide, along with any data collected as part of the research project, will be stored in a secure database and accessed only by authorized personnel involved in the project.\n" +
            // "The University of Birmingham will retain this information for research, statistical, and audit purposes. By providing this information, you consent to the University storing your data for the purposes stated above.\n" +
            // "The information will be processed by the University of Birmingham in accordance with the provisions of the Data Protection Act 2018. No identifiable personal data will be published.\n\n" +

            // "Click the 'I AGREE' button below to proceed with the experiment. By clicking the button, you indicate that you have read and understood the above information and that you consent to participate in this experiment. Alternatively, click 'Quit' to exit the game.";


            string defaultConsentText =
            "First, please MAXIMIZE the game screen if you haven't already. Adjust the volume to a comfortable level.\n\n" +
            "This study explores how people track and respond to effort.\n\n" +
            "You will play an online game navigating a grid world to collect rewards by making effort-based decisions. Your performance will determine whether a financial bonus will be added. \n\n" +
            "The game takes approximately 25-30 minutes, followed by some questionnaires (25-30 minutes). The total participation time is about 1 hour.\n\n" +
            "You will receive £8 for completing the study, plus an additional £1-5 bonus based on your final score.\n\n" +
            "As this study involves repetitive button presses over periods of time, if you experience discomfort from repetitive wrist movements (e.g., carpal tunnel, arthritis), please refrain from participation. \n\n" +
            "Otherwise, the study carries no significant risks. You may withdraw at any time.\n\n" +
            "Participation is voluntary. You may withdraw at any point during the task without explanation (Press 'ESC'). \n\n" +
            "Data removal requests are possible within two weeks after collection; once included in the analysis, data cannot be withdrawn.\n\n" +
            "Your responses will be anonymized and used for research purposes. Results may be published in academic reports and shared with other researchers, but no identifiable data will be disclosed. Responsible University personnel may access the data for auditing or legal compliance.\n\n" +
            "Approved by the University of Birmingham STEM Ethical Review Committee (Approval: ERN_20-1897AP14). For inquiries or concerns: - Dr. Meijia Li: M.LI.14@bham.ac.uk, - Dr. Matthew Apps: M.A.J.Apps@bham.ac.uk, Complaints: - Dr. Birgit Whitman, B.Whitman@bham.ac.uk\n\n" +
            "Please confirm the following:\n" +
            "* I have read and understood the above information sheet.\n" +
            "* I can ask questions if needed.\n" +
            "* I understand participation is voluntary, and I can withdraw at any time.\n" +
            "* I understand data withdrawal is only possible within two weeks of collection.\n" +
            "* My data will remain confidential and anonymized.\n" +
            "* My anonymized data may be shared with other researchers or stored in online repositories.\n" +
            "* I agree to participate. \n\n" +
            "Click the 'I AGREE' button below to proceed with the experiment. By clicking the button, you indicate that you have read and understood the above information and that you consent to participate in this experiment. Alternatively, click 'ESC' to exit the study.";

            // Split the default text so each consent item ("o") is on its own page
            consentPages = SplitConsentTextByLines(defaultConsentText);
        }

        // Display the first page
        UpdatePageDisplay();

        // Add navigation control
        if (gameObject.GetComponent<ButtonNavigationController>() == null)
        {
            ButtonNavigationController navigationController = gameObject.AddComponent<ButtonNavigationController>();
            navigationController.AddElement(continueButton);
        }
    }

    private void Awake()
    {
        // Show cursor for this form scene
        ShowCursor();
    }

    private void OnDestroy()
    {
        // Hide cursor when leaving this scene
        HideCursor();
    }

    private void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        // Existing code for space and left arrow
        if (Input.GetKeyDown(KeyCode.Space))
        {
            NavigateToNextPage();
        }

        // Check for left arrow key press to navigate backward
        if (Input.GetKeyDown(KeyCode.LeftArrow) && currentPageIndex > 0)
        {
            NavigateToPreviousPage();
        }

        // NEW CODE: Check for ESC key to quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitToEndScreen();
        }
    }

    private void NavigateToNextPage()
    {
        if (isLastPage)
        {
            ContinueToNextScreen();
            return;
        }

        currentPageIndex++;
        if (currentPageIndex >= consentPages.Length)
        {
            currentPageIndex = consentPages.Length - 1;
        }

        UpdatePageDisplay();

        // Play sound if available
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    private void NavigateToPreviousPage()
    {
        currentPageIndex--;
        if (currentPageIndex < 0)
        {
            currentPageIndex = 0;
        }

        UpdatePageDisplay();

        // Play sound if available
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    private void UpdatePageDisplay()
    {
        // Update content
        if (contentText != null && consentPages.Length > currentPageIndex)
        {
            contentText.text = consentPages[currentPageIndex];
        }

        // Check if we're on the last page
        isLastPage = (currentPageIndex == consentPages.Length - 1);
        bool isFirstPage = (currentPageIndex == 0);

        // Update instruction text
        if (instructionText != null)
        {
            if (isLastPage)
            {
                instructionText.text = "Press the 'I AGREE' button to continue, or Press 'ESC' to quit";
            }
            else if (isFirstPage)
            {
                instructionText.text = "Press 'Space' for next page";
            }
            else
            {
                instructionText.text = "Press 'Space' to continue; \n\n Press '←' (left) to go back";
            }
        }

        // Show/hide continue button based on last page
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(isLastPage);
        }

        // Update page indicator if available
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"Page {currentPageIndex + 1} of {consentPages.Length}";
        }
    }

    private void OnContinueButtonClick()
    {
        if (isLastPage)
        {
            ContinueToNextScreen();
        }
        else
        {
            NavigateToNextPage();
        }
    }

    private void ContinueToNextScreen()
    {
        Debug.Log("Continuing to the next screen: " + nextSceneName);
        SceneManager.LoadScene(nextSceneName);
    }

    // Helper method to split consent text line by line
    private string[] SplitConsentTextByLines(string fullText)
    {
        List<string> pages = new List<string>();

        // Split by line breaks
        string[] lines = fullText.Split(new string[] { "\n" }, System.StringSplitOptions.None);

        // Process each line
        StringBuilder currentPage = new StringBuilder();

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // Check if this line starts with "o " (a consent item)
            bool isConsentItem = line.StartsWith("o ");

            // If it's a consent item or the introduction, make it a separate page
            if (isConsentItem || i == 0)
            {
                // If we've been building a page, finalize it first
                if (currentPage.Length > 0)
                {
                    pages.Add(currentPage.ToString().Trim());
                    currentPage.Clear();
                }

                // Start a new page with this line
                currentPage.Append(line);
            }
            else
            {
                // Handle the "Please read and confirm the following statements:" line
                if (line.Contains("Please read and confirm"))
                {
                    // If we've been building a page, finalize it first
                    if (currentPage.Length > 0)
                    {
                        pages.Add(currentPage.ToString().Trim());
                        currentPage.Clear();
                    }

                    // Add this as its own page
                    pages.Add(line);
                }
                else
                {
                    // For the last consent item which spans multiple lines (the lengthy legal text)
                    if (currentPage.Length > 0)
                    {
                        // If the current page is already a consent item, add this line to it
                        if (currentPage.ToString().Trim().StartsWith("o "))
                        {
                            currentPage.Append(" ").Append(line);
                        }
                        // Otherwise start a new page
                        else
                        {
                            pages.Add(currentPage.ToString().Trim());
                            currentPage.Clear();
                            currentPage.Append(line);
                        }
                    }
                    else
                    {
                        // Start a new page with this line
                        currentPage.Append(line);
                    }
                }
            }
        }

        // Add the last page if anything is left
        if (currentPage.Length > 0)
        {
            pages.Add(currentPage.ToString().Trim());
        }

        return pages.ToArray();
    }

    private void QuitToEndScreen()
    {
        Debug.Log("User pressed ESC. Quitting to: " + quitSceneName);
        SceneManager.LoadScene(quitSceneName);
    }
}