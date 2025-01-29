using UnityEngine;
using TMPro;
using DG.Tweening;

public class CountdownAnimationManager : MonoBehaviour
{
    [Header("Warning Animation Settings")]
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float maxScale = 1.4f;
    [SerializeField] private Color warningColor = new Color(1f, 0.3f, 0.3f); // 红色警告色
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
        
        // 创建一个循环的动画序列
        currentAnimation
            // 放大和颜色变化
            .Append(timerText.transform.DOScale(originalScale * maxScale, pulseDuration / 2))
            .Join(timerText.DOColor(warningColor, pulseDuration / 2))
            // 恢复正常大小和颜色
            .Append(timerText.transform.DOScale(originalScale, pulseDuration / 2))
            .Join(timerText.DOColor(normalColor, pulseDuration / 2))
            // 设置循环
            .SetLoops(-1); // -1 表示无限循环

        currentAnimation.Play();
    }

    public void PlayFinalCountAnimation(int number)
    {
        StopCurrentAnimation();

        currentAnimation = DOTween.Sequence();
        
        // 为每个数字创建特殊的动画
        currentAnimation
            // 快速放大并变色
            .Append(timerText.transform.DOScale(originalScale * maxScale, 0.2f))
            .Join(timerText.DOColor(warningColor, 0.2f))
            // 保持一会儿
            .AppendInterval(0.3f)
            // 恢复正常大小和颜色
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

        // 确保文本恢复到初始状态
        if (timerText != null)
        {
            timerText.transform.localScale = originalScale;
            timerText.color = normalColor;
        }
    }
}