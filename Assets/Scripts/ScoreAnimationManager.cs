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
    [SerializeField] private Color positiveScoreColor = new Color(0.4f, 1f, 0.4f); // 绿色
    [SerializeField] private Color normalColor = new Color(0.584f, 0.761f, 0.749f); // #95C2BF
    
    [Header("Popup Text")]
    [SerializeField] private GameObject scorePopupPrefab;
    private Canvas canvas;

    private void Start()
    {
        canvas = FindObjectOfType<Canvas>();
        if (!canvas)
        {
            Debug.LogError("Canvas not found in scene!");
        }
    }

    public void PlayScoreAnimation(TextMeshProUGUI scoreText, int points)
    {
        // 停止所有当前正在运行的动画
        scoreText.transform.DOKill();
        scoreText.DOKill();

        // 创建弹出文本显示得分
        CreateScorePopup(points, scoreText.transform.position);

        // 缩放动画序列
        Sequence scaleSequence = DOTween.Sequence();
        scaleSequence.Append(scoreText.transform.DOScale(Vector3.one * scaleMultiplier, scaleDuration / 2))
                    .Append(scoreText.transform.DOScale(Vector3.one, scaleDuration / 2));

        // 颜色变化序列
        Sequence colorSequence = DOTween.Sequence();
        colorSequence.Append(scoreText.DOColor(positiveScoreColor, colorDuration / 2))
                    .Append(scoreText.DOColor(normalColor, colorDuration / 2));
    }

    private void CreateScorePopup(int points, Vector3 position)
    {
        if (!scorePopupPrefab || !canvas) return;

        GameObject popupObj = Instantiate(scorePopupPrefab, position, Quaternion.identity, canvas.transform);
        TextMeshProUGUI popupText = popupObj.GetComponent<TextMeshProUGUI>();
        
        if (popupText)
        {
            popupText.text = $"+{points}";
            
            // 设置初始值
            popupText.color = new Color(positiveScoreColor.r, positiveScoreColor.g, positiveScoreColor.b, 1);
            
            // 创建上浮和淡出动画
            Sequence popupSequence = DOTween.Sequence();
            popupSequence.Append(popupObj.transform.DOMoveY(position.y + 100f, 1f))
                        .Join(popupText.DOFade(0, 1f))
                        .OnComplete(() => Destroy(popupObj));
        }
    }
}