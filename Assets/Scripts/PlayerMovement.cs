using UnityEngine;
using System.Collections;
using TMPro;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float stepDistance = 1f;
    [SerializeField] private int pressesPerStep;
    private int pressCounter = 0;
    private Vector3 movement;
    private bool canMove = false;

    public void SetPressesPerStep(int presses)
    {
        pressesPerStep = presses;
        pressCounter = 0;
    }

    public void ResetPosition(Vector3 position)
    {
        transform.position = position;
        pressCounter = 0;
    }

    public void EnableMovement() => canMove = true;
    public void DisableMovement() => canMove = false;

    private void Update()
    {
        if (!canMove) return;

        Vector3? direction = Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) ? Vector3.forward :
                             Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) ? Vector3.back :
                             Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow) ? Vector3.left :
                             Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow) ? Vector3.right : null;

        if (direction.HasValue)
        {
            IncrementCounter(direction.Value);
        }
    }

    private void IncrementCounter(Vector3 direction)
    {
        movement = direction;
        if (++pressCounter >= pressesPerStep)
        {
            Move();
            pressCounter = 0;
        }
    }

    private void Move()
    {
        transform.Translate(movement * stepDistance);
    }
}
