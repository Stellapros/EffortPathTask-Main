using UnityEngine;

public class DestroyOutOfBounds : MonoBehaviour
{
    [SerializeField] private float topBound = 30;
    [SerializeField] private float lowerBound = -10;
    [SerializeField] private float sideBound = 15;

    private void Update()
    {
        Vector3 position = transform.position;
        if (position.z > topBound || position.z < lowerBound || Mathf.Abs(position.x) > sideBound)
        {
            Destroy(gameObject);
        }
    }
}