using UnityEngine;

public class SimulatedFingertipController : MonoBehaviour
{
    [Header("Movement")]
    public float horizontalSpeed = 0.5f;
    public float verticalSpeed = 0.5f;
    public float depthSpeed = 0.5f;

    [Header("Distance Limits")]
    public float minDistance = 0.15f;
    public float maxDistance = 1.0f;

    [Header("Collision")]
    public float collisionRadius = 0.010f;
    public float skinWidth = 0.001f;
    public LayerMask fingertipCollisionLayers;

    private Rigidbody rb;
    private Transform cameraTransform;

    private Vector3 startCameraRelativePosition;
    private Vector3 inputMovement;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError(
                "SimulatedFingertipController: Rigidbody is missing."
            );

            enabled = false;
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError(
                "SimulatedFingertipController: Main Camera could not be found."
            );

            enabled = false;
            return;
        }

        cameraTransform = Camera.main.transform;

        // Save initial fingertip position relative to the camera.
        startCameraRelativePosition =
            cameraTransform.InverseTransformPoint(
                rb.position
            );
    }

    void Update()
    {
        ReadInput();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetFingertip();
        }
    }

    void FixedUpdate()
    {
        MoveFingertip();
    }

    private void ReadInput()
    {
        inputMovement = Vector3.zero;

        // Left / Right
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            inputMovement.x -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            inputMovement.x += 1f;
        }

        // Up / Down
        if (Input.GetKey(KeyCode.UpArrow))
        {
            inputMovement.y += 1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            inputMovement.y -= 1f;
        }

        // Forward / Back
        if (Input.GetKey(KeyCode.RightBracket))
        {
            inputMovement.z += 1f;
        }

        if (Input.GetKey(KeyCode.LeftBracket))
        {
            inputMovement.z -= 1f;
        }

        inputMovement =
            inputMovement.normalized;
    }

    private void MoveFingertip()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // Convert keyboard input into movement relative
        // to the direction the camera is facing.
        Vector3 worldMovement =
            cameraTransform.right *
            inputMovement.x *
            horizontalSpeed;

        worldMovement +=
            cameraTransform.up *
            inputMovement.y *
            verticalSpeed;

        worldMovement +=
            cameraTransform.forward *
            inputMovement.z *
            depthSpeed;

        Vector3 movementStep =
            worldMovement *
            Time.fixedDeltaTime;

        if (movementStep.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 direction =
            movementStep.normalized;

        float distance =
            movementStep.magnitude;

        RaycastHit hit;

        bool blocked =
            Physics.SphereCast(
                rb.position,
                collisionRadius,
                direction,
                out hit,
                distance + skinWidth,
                fingertipCollisionLayers,
                QueryTriggerInteraction.Ignore
            );

        Vector3 targetPosition;

        if (blocked)
        {
            float allowedDistance =
                Mathf.Max(
                    0f,
                    hit.distance -
                    skinWidth
                );

            targetPosition =
                rb.position +
                direction *
                allowedDistance;
        }
        else
        {
            targetPosition =
                rb.position +
                movementStep;
        }

        // Keep fingertip within allowed distance
        // from the camera.
        Vector3 cameraRelativePosition =
            cameraTransform.InverseTransformPoint(
                targetPosition
            );

        cameraRelativePosition.z =
            Mathf.Clamp(
                cameraRelativePosition.z,
                minDistance,
                maxDistance
            );

        targetPosition =
            cameraTransform.TransformPoint(
                cameraRelativePosition
            );

        rb.MovePosition(
            targetPosition
        );
    }

    public void ResetFingertip()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Vector3 resetPosition =
            cameraTransform.TransformPoint(
                startCameraRelativePosition
            );

        rb.position =
            resetPosition;

        rb.velocity =
            Vector3.zero;

        rb.angularVelocity =
            Vector3.zero;
    }
}