using UnityEngine;

public class Shape2DStartMarker : MonoBehaviour
{
    [Header("References")]
    public Transform fingertip;
    public Transform startMarker;

    [Header("Detection Settings")]
    [Tooltip("Maximum distance from the start marker to count as ready.")]
    public float detectionRadius = 0.02f;

    [Header("Status")]
    public bool fingertipAtStart = false;
    public float currentDistance = 0f;

    void Update()
    {
        CheckStartPosition();
    }

    private void CheckStartPosition()
    {
        if (fingertip == null || startMarker == null)
        {
            fingertipAtStart = false;
            currentDistance = 0f;
            return;
        }

        // Compare fingertip and start marker positions
        // in world space.
        currentDistance = Vector3.Distance(
            fingertip.position,
            startMarker.position
        );

        fingertipAtStart =
            currentDistance <= detectionRadius;
    }

    public bool IsFingertipAtStart()
    {
        return fingertipAtStart;
    }
}