using UnityEngine;

public class SpectatorCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 1.5f;
    public float cameraHeight = 1.7f;

    [Header("Mouse Look")]
    public float lookSensitivity = 2f;
    public float maxPitch = 85f;

    [Header("Domain Boundary")]
    public Transform domainCenter;
    public float domainRadius = 16f;
    public float boundaryPadding = 1.5f;

    private float pitch;
    private float yaw;

    public bool ControlsActive =>
        Cursor.lockState == CursorLockMode.Locked;

    private void Start()
    {
        pitch = transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f;

        yaw = transform.eulerAngles.y;

        Vector3 position = transform.position;
        position.y = cameraHeight;
        transform.position = position;

        LockCursor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (!ControlsActive)
        {
            if (Input.GetMouseButtonDown(0))
                LockCursor();

            return;
        }

        yaw += Input.GetAxisRaw("Mouse X") * lookSensitivity;
        pitch -= Input.GetAxisRaw("Mouse Y") * lookSensitivity;
        pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 movement =
            right * Input.GetAxisRaw("Horizontal") +
            forward * Input.GetAxisRaw("Vertical");

        if (movement.sqrMagnitude > 1f)
            movement.Normalize();

        float speed = moveSpeed;

        if (Input.GetKey(KeyCode.LeftShift))
            speed *= sprintMultiplier;

        Vector3 next = transform.position +
            movement * speed * Time.deltaTime;

        next.y = cameraHeight;

        // stay inside arena
        if (domainCenter != null)
        {
            Vector3 offset = next - domainCenter.position;
            offset.y = 0f;

            float limit = Mathf.Max(
                1f, domainRadius - boundaryPadding);

            if (offset.magnitude > limit)
            {
                offset = offset.normalized * limit;

                next.x = domainCenter.position.x + offset.x;
                next.z = domainCenter.position.z + offset.z;
            }
        }

        transform.position = next;
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
