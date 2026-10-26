using UnityEngine;

public class Shape2DFingertipVisual : MonoBehaviour
{
    [Header("References")]
    public Transform trackedFingertip;
    public Transform interactionPlane;
    public Transform mainCamera;

    [Header("Settings")]
    public float visualPlaneOffset = 0.012f;
    public bool preventPlanePenetration = true;

    void LateUpdate()
    {
        if (trackedFingertip == null ||
            interactionPlane == null ||
            mainCamera == null)
        {
            return;
        }

        Vector3 localPosition =
            interactionPlane.InverseTransformPoint(
                trackedFingertip.position
            );

        Vector3 cameraLocalPosition =
            interactionPlane.InverseTransformPoint(
                mainCamera.position
            );

        // Determine which side of the plane faces the camera.
        float cameraSide =
            cameraLocalPosition.z >= 0f ? 1f : -1f;

        if (preventPlanePenetration)
        {
            // Keep the visual sphere on the camera-facing side.
            if (cameraSide > 0f)
            {
                localPosition.z =
                    Mathf.Max(
                        localPosition.z,
                        visualPlaneOffset
                    );
            }
            else
            {
                localPosition.z =
                    Mathf.Min(
                        localPosition.z,
                        -visualPlaneOffset
                    );
            }
        }

        transform.position =
            interactionPlane.TransformPoint(
                localPosition
            );
    }
}