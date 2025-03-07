using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;

public class ScoreAnimationManager : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float scaleDuration = 0.3f;
    [SerializeField] private float colorDuration = 0.5f;
    [SerializeField] private float scaleMultiplier = 1.3f;

    [Header("Colors")]
    [SerializeField] private Color positiveScoreColor = new Color(0.694f, 0.925f, 0.180f); // #B1EC2E
    [SerializeField] private Color normalColor = new Color(0.584f, 0.761f, 0.749f); // #95C2BF

    [Header("Popup Text")]
    [SerializeField] private GameObject scorePopupPrefab;
    private Canvas canvas;

    private void Start()
    {
        canvas = Object.FindFirstObjectByType<Canvas>();
        if (!canvas)
        {
            Debug.LogError("Canvas not found in scene!");
        }
    }

    public void PlayScoreAnimation(TextMeshProUGUI scoreText, int points)
    {
        // Check if we're in GridWorld scene and if the scoreText is valid
        if (!IsValidScoreText(scoreText))
        {
            return;
        }

        // Stop all currently running animations
        scoreText.transform.DOKill();
        scoreText.DOKill();

        // Create popup text to display points
        CreateScorePopup(points, scoreText.transform.position);

        // Scale animation sequence
        Sequence scaleSequence = DOTween.Sequence();
        scaleSequence.Append(scoreText.transform.DOScale(Vector3.one * scaleMultiplier, scaleDuration / 2))
                    .Append(scoreText.transform.DOScale(Vector3.one, scaleDuration / 2));

        // Color change sequence
        Sequence colorSequence = DOTween.Sequence();
        colorSequence.Append(scoreText.DOColor(positiveScoreColor, colorDuration / 2))
                    .Append(scoreText.DOColor(normalColor, colorDuration / 2));
    }

    private bool IsValidScoreText(TextMeshProUGUI scoreText)
    {
        // Check if we're in the GridWorld scene
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "GridWorld")
        {
            return false;
        }

        // Check if the text component has the correct name
        if (!scoreText.gameObject.name.Contains("ScoreText"))
        {
            return false;
        }

        return true;
    }

    private void CreateScorePopup(int points, Vector3 position)
    {
        if (!scorePopupPrefab || !canvas) return;

        GameObject popupObj = Instantiate(scorePopupPrefab, position, Quaternion.identity, canvas.transform);
        TextMeshProUGUI popupText = popupObj.GetComponent<TextMeshProUGUI>();

        if (popupText)
        {
            popupText.text = $"+{points}";

            // Set initial values
            popupText.color = new Color(positiveScoreColor.r, positiveScoreColor.g, positiveScoreColor.b, 1);

            // Create float-up and fade-out animations
            Sequence popupSequence = DOTween.Sequence();
            popupSequence.Append(popupObj.transform.DOMoveY(position.y + 100f, 1f))
                        .Join(popupText.DOFade(0, 1f))
                        .OnComplete(() => Destroy(popupObj));
        }
    }
}