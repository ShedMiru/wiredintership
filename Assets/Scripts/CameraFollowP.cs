using UnityEngine;

public class CameraFollowP : MonoBehaviour
{
    [SerializeField] private Rigidbody2D targetRb; // drag PLAYER Rigidbody2D here
    [SerializeField] private float minY;
    [SerializeField] private float maxY;

    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (targetRb == null || cam == null) return;

        float halfH = cam.orthographicSize;

        // Move camera by the same Y displacement the player had this frame
        float newY = transform.position.y + targetRb.velocity.y * Time.deltaTime;

        // Keep camera EDGES inside bounds (no Mathf.Clamp)
        float bottom = newY - halfH - 0.005f;
        float top = newY + halfH + 0.005f;

        if (bottom < minY) newY = minY + halfH;
        if (top > maxY) newY = maxY - halfH;

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
