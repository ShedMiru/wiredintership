using UnityEngine;

public class CameraFollowP : MonoBehaviour
{
    [SerializeField] private Transform target;   // player
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float xOffset = 0f;

    private float xVelocity;

    void LateUpdate()
    {
        if (target == null) return;

        float targetX = target.position.x + xOffset;
        float newX = Mathf.SmoothDamp(transform.position.x, targetX, ref xVelocity, smoothTime);

        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }
}
