using UnityEngine;

public class FingertipVisualClamp : MonoBehaviour
{
    [Header("References")]
    public Transform trackedPoint;

    [Header("Surface Detection")]
    public LayerMask surfaceMask = ~0;

    [Tooltip("Maximum distance used to find the nearest surface.")]
    public float searchRadius = 0.05f;

    [Header("Visual Sphere")]
    [Tooltip("World-space radius of the visible fingertip sphere.")]
    public float visualRadius = 0.00625f;

    [Tooltip("Small gap that keeps the sphere slightly above the surface.")]
    public float surfacePadding = 0.0005f;

    void LateUpdate()
    {
        if (trackedPoint == null)
            return;

        // Normally the visual follows the tracked fingertip exactly.
        Vector3 desiredPosition =
            trackedPoint.position;

        Collider[] nearbyColliders =
            Physics.OverlapSphere(
                trackedPoint.position,
                searchRadius,
                surfaceMask,
                QueryTriggerInteraction.Ignore
            );

        Collider closestCollider = null;
        Vector3 closestPoint = Vector3.zero;
        float closestDistance =
            Mathf.Infinity;

        foreach (Collider col in nearbyColliders)
        {
            HapticObject hapticObject =
                col.GetComponentInParent<HapticObject>();

            if (hapticObject == null)
                continue;

            Vector3 point =
                col.ClosestPoint(
                    trackedPoint.position
                );

            float distance =
                Vector3.Distance(
                    trackedPoint.position,
                    point
                );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestCollider = col;
                closestPoint = point;
            }
        }

        if (closestCollider != null)
        {
            Vector3 direction =
                trackedPoint.position -
                closestPoint;

            // When the tracked point is outside the collider,
            // move the visual sphere far enough away that its
            // outer edge rests on the surface.
            if (direction.sqrMagnitude > 0.000001f)
            {
                direction.Normalize();

                float minimumDistance =
                    visualRadius +
                    surfacePadding;

                if (closestDistance <
                    minimumDistance)
                {
                    desiredPosition =
                        closestPoint +
                        direction *
                        minimumDistance;
                }
            }
        }

        transform.position =
            desiredPosition;
    }
}