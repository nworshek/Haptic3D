
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

    [Header("Fingertips")]
    public Transform indexFingertip;
    public Transform middleFingertip;
    public Transform ringFingertip;

    [Header("Collision")]
    public float collisionRadius = 0.010f;
    public float skinWidth = 0.001f;
    public LayerMask fingertipCollisionLayers;

    [Header("Shape2D Simulation")]
    [Tooltip("Enable only for simulated Shape2D testing.")]
    public bool constrainToShape2DPlane = false;

    [Tooltip("The Shape2D interaction plane.")]
    public Transform shape2DInteractionPlane;

    [Tooltip("The actual simulated index fingertip.")]
    public Transform shape2DTrackedFingertip;

    [Tooltip("Distance from the plane where the fingertip stops.")]
    public float shape2DPlaneOffset = 0.009f;

    [Header("Temporary Remote Testing")]
    [Tooltip("Allows the Lenovo experimenter GUI to move the simulated hand over the existing TCP connection.")]
    public bool allowRemoteControl = true;

    private Rigidbody rb;
    private Transform cameraTransform;

    private Vector3 startCameraRelativePosition;

    private Vector3 inputMovement;
    private Vector3 remoteMovement;

    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError(
                "SimulatedFingertipController: " +
                "FingertipManager Rigidbody is missing."
            );

            enabled = false;
            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError(
                "SimulatedFingertipController: " +
                "Main Camera could not be found."
            );

            enabled = false;
            return;
        }

        if (
            indexFingertip == null ||
            middleFingertip == null ||
            ringFingertip == null
        )
        {
            Debug.LogError(
                "SimulatedFingertipController: " +
                "Index, Middle, and Ring fingertips " +
                "must all be assigned."
            );

            enabled = false;
            return;
        }

        cameraTransform = Camera.main.transform;

        startCameraRelativePosition =
            cameraTransform.InverseTransformPoint(
                rb.position
            );

        remoteMovement = Vector3.zero;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        ReadInput();

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetFingertip();
        }
    }

    // =========================================================
    // FIXED UPDATE
    // =========================================================

    void FixedUpdate()
    {
        MoveHand();
    }

    // =========================================================
    // LOCAL KEYBOARD INPUT
    // =========================================================

    private void ReadInput()
    {
        inputMovement = Vector3.zero;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            inputMovement.x -= 1f;
        }

        if (Input.GetKey(KeyCode.RightArrow))
        {
            inputMovement.x += 1f;
        }

        if (Input.GetKey(KeyCode.UpArrow))
        {
            inputMovement.y += 1f;
        }

        if (Input.GetKey(KeyCode.DownArrow))
        {
            inputMovement.y -= 1f;
        }

        // Forward
        if (Input.GetKey(KeyCode.RightBracket))
        {
            inputMovement.z += 1f;
        }

        // Backward
        if (Input.GetKey(KeyCode.LeftBracket))
        {
            inputMovement.z -= 1f;
        }

        inputMovement = inputMovement.normalized;
    }

    // =========================================================
    // REMOTE MOVEMENT
    // =========================================================

    public void SetRemoteMovement(
        float x,
        float y,
        float z
    )
    {
        if (!allowRemoteControl)
        {
            remoteMovement = Vector3.zero;
            return;
        }

        remoteMovement = new Vector3(
            Mathf.Clamp(x, -1f, 1f),
            Mathf.Clamp(y, -1f, 1f),
            Mathf.Clamp(z, -1f, 1f)
        );

        if (remoteMovement.sqrMagnitude > 1f)
        {
            remoteMovement = remoteMovement.normalized;
        }
    }

    // =========================================================
    // STOP REMOTE MOVEMENT
    // =========================================================

    public void StopRemoteMovement()
    {
        remoteMovement = Vector3.zero;
    }

    // =========================================================
    // MOVE ENTIRE HAND
    // =========================================================

    private void MoveHand()
    {
        if (cameraTransform == null)
        {
            return;
        }

        // Combine local and remote input.
        Vector3 combinedMovement = inputMovement;

        if (allowRemoteControl)
        {
            combinedMovement += remoteMovement;
        }

        if (combinedMovement.sqrMagnitude > 1f)
        {
            combinedMovement = combinedMovement.normalized;
        }

        // Camera-relative movement.
        Vector3 worldMovement =
            cameraTransform.right *
            combinedMovement.x *
            horizontalSpeed;

        worldMovement +=
            cameraTransform.up *
            combinedMovement.y *
            verticalSpeed;

        worldMovement +=
            cameraTransform.forward *
            combinedMovement.z *
            depthSpeed;

        Vector3 movementStep =
            worldMovement * Time.fixedDeltaTime;

        if (movementStep.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 direction =
            movementStep.normalized;

        float requestedDistance =
            movementStep.magnitude;

        // =====================================================
        // COLLISION CHECK FOR ALL THREE FINGERTIPS
        // =====================================================

        float allowedDistance = requestedDistance;

        allowedDistance = Mathf.Min(
            allowedDistance,
            GetAllowedDistance(
                indexFingertip,
                direction,
                requestedDistance
            )
        );

        allowedDistance = Mathf.Min(
            allowedDistance,
            GetAllowedDistance(
                middleFingertip,
                direction,
                requestedDistance
            )
        );

        allowedDistance = Mathf.Min(
            allowedDistance,
            GetAllowedDistance(
                ringFingertip,
                direction,
                requestedDistance
            )
        );

        Vector3 targetPosition =
            rb.position +
            direction * allowedDistance;

        // =====================================================
        // CAMERA DISTANCE LIMITS
        // =====================================================

        Vector3 cameraRelativePosition =
            cameraTransform.InverseTransformPoint(
                targetPosition
            );

        cameraRelativePosition.z = Mathf.Clamp(
            cameraRelativePosition.z,
            minDistance,
            maxDistance
        );

        targetPosition =
            cameraTransform.TransformPoint(
                cameraRelativePosition
            );

        // =====================================================
        // SHAPE2D PLANE CONSTRAINT
        // =====================================================

        if (
            constrainToShape2DPlane &&
            shape2DInteractionPlane != null &&
            shape2DTrackedFingertip != null
        )
        {
            // Offset between the hand Rigidbody and fingertip.
            Vector3 fingertipOffset =
                shape2DTrackedFingertip.position -
                rb.position;

            // Predict where the fingertip will move.
            Vector3 proposedFingertipPosition =
                targetPosition + fingertipOffset;

            // Convert into the Shape2D plane coordinates.
            Vector3 localFingertip =
                shape2DInteractionPlane.InverseTransformPoint(
                    proposedFingertipPosition
                );

            Vector3 localCamera =
                shape2DInteractionPlane.InverseTransformPoint(
                    cameraTransform.position
                );

            // Identify the camera-facing side of the plane.
            float cameraSide =
                localCamera.z >= 0f ? 1f : -1f;

            // Prevent the simulated fingertip from
            // passing through the interaction plane.
            if (cameraSide > 0f)
            {
                localFingertip.z = Mathf.Max(
                    localFingertip.z,
                    shape2DPlaneOffset
                );
            }
            else
            {
                localFingertip.z = Mathf.Min(
                    localFingertip.z,
                    -shape2DPlaneOffset
                );
            }

            // Convert corrected position to world space.
            Vector3 correctedFingertipPosition =
                shape2DInteractionPlane.TransformPoint(
                    localFingertip
                );

            // Apply the correction to the entire simulated hand.
            targetPosition +=
                correctedFingertipPosition -
                proposedFingertipPosition;
        }

        // Move the hand Rigidbody.
        rb.MovePosition(targetPosition);
    }

    // =========================================================
    // CHECK COLLISION FOR ONE FINGERTIP
    // =========================================================

    private float GetAllowedDistance(
        Transform fingertip,
        Vector3 direction,
        float requestedDistance
    )
    {
        if (fingertip == null)
        {
            return requestedDistance;
        }

        RaycastHit hit;

        bool blocked = Physics.SphereCast(
            fingertip.position,
            collisionRadius,
            direction,
            out hit,
            requestedDistance + skinWidth,
            fingertipCollisionLayers,
            QueryTriggerInteraction.Ignore
        );

        if (!blocked)
        {
            return requestedDistance;
        }

        float allowedDistance = Mathf.Max(
            0f,
            hit.distance - skinWidth
        );

        return allowedDistance;
    }

    // =========================================================
    // RESET ENTIRE HAND
    // =========================================================

    public void ResetFingertip()
    {
        if (cameraTransform == null || rb == null)
        {
            return;
        }

        remoteMovement = Vector3.zero;

        Vector3 resetPosition =
            cameraTransform.TransformPoint(
                startCameraRelativePosition
            );

        rb.position = resetPosition;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // =========================================================
    // SAFETY / DISABLE
    // =========================================================

    void OnDisable()
    {
        remoteMovement = Vector3.zero;
        inputMovement = Vector3.zero;
    }
}
