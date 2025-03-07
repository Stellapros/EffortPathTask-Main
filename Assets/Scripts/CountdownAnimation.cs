using UnityEngine;
using TMPro;
using DG.Tweening;

/// Source: Produced by Claude
/// Date: 28/01/2025
/// For the countdown timer warning
/// Edited by ML

public class CountdownAnimationManager : MonoBehaviour
{
    [Header("Warning Animation Settings")]
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float maxScale = 1.4f;
    // [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f); // red
    [SerializeField] private Color warningColor = new Color(0.4f, 1f, 0.4f);  // green
    [SerializeField] private Color normalColor = Color.white;

    private TextMeshProUGUI timerText;
    private Vector3 originalScale;
    private Sequence currentAnimation;

    public void Initialize(TextMeshProUGUI timerText)
    {
        this.timerText = timerText;
        originalScale = timerText.transform.localScale;
        StopCurrentAnimation();
    }

    public void PlayWarningAnimation()
    {
        StopCurrentAnimation();

        currentAnimation = DOTween.Sequence();

        // Create a looping animation sequence
        currentAnimation
            // Scale up and change color
            .Append(timerText.transform.DOScale(originalScale * maxScale, pulseDuration / 2))
            .Join(timerText.DOColor(warningColor, pulseDuration / 2))
            // Return to normal size and color
            .Append(timerText.transform.DOScale(originalScale, pulseDuration / 2))
            .Join(timerText.DOColor(normalColor, pulseDuration / 2))
            // Set looping
            .SetLoops(-1); // -1 means infinite looping

        currentAnimation.Play();
    }

    public void PlayFinalCountAnimation(int number)
    {
        StopCurrentAnimation();

        currentAnimation = DOTween.Sequence();

        // Create a special animation for each number
        currentAnimation
            // Quickly scale up and change color
            .Append(timerText.transform.DOScale(originalScale * maxScale, 0.2f))
            .Join(timerText.DOColor(warningColor, 0.2f))
            // Hold for a moment
            .AppendInterval(0.3f)
            // Return to normal size and color
            .Append(timerText.transform.DOScale(originalScale, 0.3f))
            .Join(timerText.DOColor(normalColor, 0.3f));

        currentAnimation.Play();
    }

    public void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            currentAnimation.Kill();
            currentAnimation = null;
        }

        if (timerText != null)
        {
            timerText.transform.localScale = originalScale;
            timerText.color = normalColor;
        }
    }
}