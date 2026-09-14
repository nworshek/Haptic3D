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

    private Rigidbody rb;
    private Transform cameraTransform;

    private Vector3 startCameraRelativePosition;
    private Vector3 inputMovement;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        rb =
            GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError(
                "SimulatedFingertipController: " +
                "FingertipManager Rigidbody is missing."
            );

            enabled =
                false;

            return;
        }

        if (Camera.main == null)
        {
            Debug.LogError(
                "SimulatedFingertipController: " +
                "Main Camera could not be found."
            );

            enabled =
                false;

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

            enabled =
                false;

            return;
        }

        cameraTransform =
            Camera.main.transform;

        // Save the initial position of the entire
        // hand relative to the camera.
        startCameraRelativePosition =
            cameraTransform.InverseTransformPoint(
                rb.position
            );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        ReadInput();

        if (
            Input.GetKeyDown(
                KeyCode.R
            )
        )
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
    // INPUT
    // =========================================================

    private void ReadInput()
    {
        inputMovement =
            Vector3.zero;


        // Left
        if (
            Input.GetKey(
                KeyCode.LeftArrow
            )
        )
        {
            inputMovement.x -=
                1f;
        }


        // Right
        if (
            Input.GetKey(
                KeyCode.RightArrow
            )
        )
        {
            inputMovement.x +=
                1f;
        }


        // Up
        if (
            Input.GetKey(
                KeyCode.UpArrow
            )
        )
        {
            inputMovement.y +=
                1f;
        }


        // Down
        if (
            Input.GetKey(
                KeyCode.DownArrow
            )
        )
        {
            inputMovement.y -=
                1f;
        }


        // Forward
        if (
            Input.GetKey(
                KeyCode.RightBracket
            )
        )
        {
            inputMovement.z +=
                1f;
        }


        // Back
        if (
            Input.GetKey(
                KeyCode.LeftBracket
            )
        )
        {
            inputMovement.z -=
                1f;
        }


        inputMovement =
            inputMovement.normalized;
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


        // Convert keyboard movement into camera-relative
        // world-space movement.
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


        if (
            movementStep.sqrMagnitude <=
            0f
        )
        {
            return;
        }


        Vector3 direction =
            movementStep.normalized;


        float requestedDistance =
            movementStep.magnitude;


        // Check all fingertips.
        // The closest collision controls how far
        // the entire hand is allowed to move.
        float allowedDistance =
            requestedDistance;


        allowedDistance =
            Mathf.Min(
                allowedDistance,
                GetAllowedDistance(
                    indexFingertip,
                    direction,
                    requestedDistance
                )
            );


        allowedDistance =
            Mathf.Min(
                allowedDistance,
                GetAllowedDistance(
                    middleFingertip,
                    direction,
                    requestedDistance
                )
            );


        allowedDistance =
            Mathf.Min(
                allowedDistance,
                GetAllowedDistance(
                    ringFingertip,
                    direction,
                    requestedDistance
                )
            );


        Vector3 targetPosition =
            rb.position +
            direction *
            allowedDistance;


        // Keep the whole hand within its allowed
        // depth range from the camera.
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


    // =========================================================
    // CHECK ONE FINGERTIP
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


        bool blocked =
            Physics.SphereCast(
                fingertip.position,
                collisionRadius,
                direction,
                out hit,
                requestedDistance +
                skinWidth,
                fingertipCollisionLayers,
                QueryTriggerInteraction.Ignore
            );


        if (!blocked)
        {
            return requestedDistance;
        }


        float allowedDistance =
            Mathf.Max(
                0f,
                hit.distance -
                skinWidth
            );


        return
            allowedDistance;
    }


    // =========================================================
    // RESET ENTIRE HAND
    // =========================================================

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