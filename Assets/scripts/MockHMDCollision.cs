using UnityEngine;

public class MockHMDCollision : MonoBehaviour
{
    [Header("References")]
    public Transform xrRig;
    public Transform mainCamera;

    [Header("Collision")]
    public float cameraRadius = 0.12f;
    public float skinWidth = 0.01f;

    public LayerMask cameraCollisionLayers;

    private Vector3 previousCameraPosition;
    private Vector3 previousRigPosition;

    void Start()
    {
        if (xrRig == null)
        {
            Debug.LogError(
                "MockHMDCollision: XR Rig is not assigned."
            );

            enabled = false;
            return;
        }

        if (mainCamera == null)
        {
            if (Camera.main != null)
            {
                mainCamera = Camera.main.transform;
            }
        }

        if (mainCamera == null)
        {
            Debug.LogError(
                "MockHMDCollision: Main Camera could not be found."
            );

            enabled = false;
            return;
        }

        previousCameraPosition =
            mainCamera.position;

        previousRigPosition =
            xrRig.position;
    }

    void LateUpdate()
    {
        CheckCameraCollision();
    }

    private void CheckCameraCollision()
    {
        Vector3 currentCameraPosition =
            mainCamera.position;

        Vector3 movement =
            currentCameraPosition -
            previousCameraPosition;

        float distance =
            movement.magnitude;

        // If the camera did not move,
        // there is nothing to check.
        if (distance <= 0.0001f)
        {
            previousCameraPosition =
                currentCameraPosition;

            previousRigPosition =
                xrRig.position;

            return;
        }

        Vector3 direction =
            movement.normalized;

        RaycastHit hit;

        bool blocked =
            Physics.SphereCast(
                previousCameraPosition,
                cameraRadius,
                direction,
                out hit,
                distance + skinWidth,
                cameraCollisionLayers,
                QueryTriggerInteraction.Ignore
            );

        if (blocked)
        {
            float allowedDistance =
                Mathf.Max(
                    0f,
                    hit.distance -
                    skinWidth
                );

            Vector3 allowedCameraPosition =
                previousCameraPosition +
                direction *
                allowedDistance;

            Vector3 correction =
                allowedCameraPosition -
                currentCameraPosition;

            // Move the entire XR Rig backward
            // by the required correction.
            xrRig.position += correction;

            currentCameraPosition =
                mainCamera.position;
        }

        previousCameraPosition =
            currentCameraPosition;

        previousRigPosition =
            xrRig.position;
    }
}