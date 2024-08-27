using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class DecisionManager : MonoBehaviour
{
    public Image[] effortImages; // Array of three effort Image components
    public Button yesButton;
    public Button noButton;
    public float waitTime = 3f; // Wait time in seconds after a 'No' response

    [SerializeField] private MonoBehaviour experimentManagerObject;
    private IExperimentManager experimentManager;

    private int currentImageIndex = 0;

    void Start()
    {
        // Ensure UI elements are properly assigned
        if (effortImages == null || effortImages.Length != 3 || yesButton == null || noButton == null)
        {
            Debug.LogError("UI elements not properly assigned in DecisionManager!");
            return;
        }

        // Get the IExperimentManager reference from the assigned experimentManagerObject
        experimentManager = experimentManagerObject as IExperimentManager;
        if (experimentManager == null)
        {
            Debug.LogError("ExperimentManager not found or does not implement IExperimentManager!");
            return;
        }

        // Add listeners to the buttons
        yesButton.onClick.AddListener(OnYesClicked);
        noButton.onClick.AddListener(OnNoClicked);

        // Initialize the display
        ShuffleAndDisplayImages();
    }

    void ShuffleAndDisplayImages()
    {
        // Get three random sprites from the experiment manager
        Sprite[] sprites = new Sprite[3];
        for (int i = 0; i < 3; i++)
        {
            sprites[i] = experimentManager.GetCurrentTrialSprite();
            experimentManager.SkipTrial(); // Move to next trial to get a different sprite
        }

        // Shuffle the sprites
        for (int i = 0; i < sprites.Length; i++)
        {
            Sprite temp = sprites[i];
            int randomIndex = Random.Range(i, sprites.Length);
            sprites[i] = sprites[randomIndex];
            sprites[randomIndex] = temp;
        }

        // Assign shuffled sprites to images
        for (int i = 0; i < effortImages.Length; i++)
        {
            effortImages[i].sprite = sprites[i];
            effortImages[i].gameObject.SetActive(i == 0); // Show only the first image initially
        }

        currentImageIndex = 0;
    }

    void OnYesClicked()
    {
        // Get the current trial data from the experiment manager
        Vector2 playerPosition = experimentManager.GetCurrentTrialPlayerPosition();
        Vector2 rewardPosition = experimentManager.GetCurrentTrialRewardPosition();
        float rewardValue = experimentManager.GetCurrentTrialRewardValue();

        // Start the trial in the GridWorldManager
        GridWorldManager.Instance.StartTrial(playerPosition, rewardPosition, rewardValue);

        // Load the "GridWorld" scene
        SceneManager.LoadScene("GridWorld");
    }

    void OnNoClicked()
    {
        // Start the coroutine to wait and show the next image
        StartCoroutine(WaitAndShowNext());
    }

    IEnumerator WaitAndShowNext()
    {
        // Disable buttons during wait time
        yesButton.interactable = false;
        noButton.interactable = false;

        yield return new WaitForSeconds(waitTime);

        // Move to next image or reshuffle if all images have been shown
        currentImageIndex++;
        if (currentImageIndex >= effortImages.Length)
        {
            ShuffleAndDisplayImages();
        }
        else
        {
            for (int i = 0; i < effortImages.Length; i++)
            {
                effortImages[i].gameObject.SetActive(i == currentImageIndex);
            }
        }

        // Re-enable buttons
        yesButton.interactable = true;
        noButton.interactable = true;
    }
}