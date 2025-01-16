using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AvatarActiveColour : MonoBehaviour
{
    private SpriteRenderer avatarRenderer;
    private PlayerController playerController;

    // Start is called before the first frame update
    void Start()
    {
        avatarRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
    }

    // Update is called once per frame
    void Update()
    {
        avatarRenderer.color = Color.white;
    }
}
