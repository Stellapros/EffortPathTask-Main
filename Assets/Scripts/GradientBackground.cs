using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class GradientBackground : MonoBehaviour
{
    [SerializeField] private Color topColor = new Color(0.1f, 0.1f, 0.3f); // Dark blue
    [SerializeField] private Color bottomColor = new Color(0.3f, 0.1f, 0.3f); // Purple

    private void Start()
    {
        Image image = GetComponent<Image>();
        Texture2D texture = new Texture2D(1, 2);
        texture.SetPixel(0, 0, bottomColor);
        texture.SetPixel(0, 1, topColor);
        texture.Apply();
        image.sprite = Sprite.Create(texture, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f));
    }
}