using UnityEngine;

public class MockHMDController : MonoBehaviour
{
    [Header("References")]
    public Transform xrCamera;

    [Header("Movement")]
    public float moveSpeed = 2.0f;
    public float verticalSpeed = 1.5f;

    [Header("Look")]
    public float mouseSensitivity = 2.0f;

    private float pitch = 0f;

    void Update()
    {
        HandleMovement();
        HandleLook();
    }

    private void HandleMovement()
    {
        float horizontal = 0f;
        float forward = 0f;
        float vertical = 0f;

        // XR Rig movement: WASD ONLY
        if (Input.GetKey(KeyCode.A))
            horizontal = -1f;

        if (Input.GetKey(KeyCode.D))
            horizontal = 1f;

        if (Input.GetKey(KeyCode.W))
            forward = 1f;

        if (Input.GetKey(KeyCode.S))
            forward = -1f;

        // Vertical XR Rig movement
        if (Input.GetKey(KeyCode.E))
            vertical = 1f;

        if (Input.GetKey(KeyCode.Q))
            vertical = -1f;

        Vector3 forwardDirection = transform.forward;
        Vector3 rightDirection = transform.right;

        forwardDirection.y = 0f;
        rightDirection.y = 0f;

        forwardDirection.Normalize();
        rightDirection.Normalize();

        Vector3 movement =
            forwardDirection * forward +
            rightDirection * horizontal;

        transform.position +=
            movement * moveSpeed * Time.deltaTime;

        transform.position +=
            Vector3.up *
            vertical *
            verticalSpeed *
            Time.deltaTime;
    }

    private void HandleLook()
    {
        if (xrCamera == null)
            return;

        if (!Input.GetMouseButton(1))
            return;

        float mouseX =
            Input.GetAxis("Mouse X") *
            mouseSensitivity;

        float mouseY =
            Input.GetAxis("Mouse Y") *
            mouseSensitivity;

        transform.Rotate(
            Vector3.up,
            mouseX,
            Space.World
        );

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -80f, 80f);

        xrCamera.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
    }
}