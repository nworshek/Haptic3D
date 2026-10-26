using UnityEngine;

public class Shape2DWorkspaceBoundary : MonoBehaviour
{
    [Header("Fingertip")]
    public Transform fingertip;

    [Header("Workspace Boundaries")]
    public Transform boundaryLeft;
    public Transform boundaryRight;
    public Transform boundaryTop;
    public Transform boundaryBottom;

    [Header("Warning")]
    public GameObject boundaryWarning;

    [Header("Status")]
    public bool fingertipInsideWorkspace = false;

    void Start()
    {
        // Start with the warning hidden.
        if (boundaryWarning != null)
        {
            boundaryWarning.SetActive(false);
        }
    }

    void Update()
    {
        CheckWorkspace();
    }

    private void CheckWorkspace()
    {
        // Make sure all required references exist.
        if (fingertip == null ||
            boundaryLeft == null ||
            boundaryRight == null ||
            boundaryTop == null ||
            boundaryBottom == null)
        {
            fingertipInsideWorkspace = false;

            // Keep warning hidden if setup is incomplete.
            if (boundaryWarning != null)
            {
                boundaryWarning.SetActive(false);
            }

            return;
        }

        // Convert fingertip position into the local coordinate
        // system of the Shape2D TestEnvironment.
        Vector3 localFingertip =
            transform.InverseTransformPoint(fingertip.position);

        // Read the four workspace limits.
        float left = boundaryLeft.localPosition.x;
        float right = boundaryRight.localPosition.x;
        float top = boundaryTop.localPosition.y;
        float bottom = boundaryBottom.localPosition.y;

        // Check whether fingertip is inside the 2D workspace.
        fingertipInsideWorkspace =
            localFingertip.x >= left &&
            localFingertip.x <= right &&
            localFingertip.y >= bottom &&
            localFingertip.y <= top;

        // Show warning only when fingertip leaves workspace.
        if (boundaryWarning != null)
        {
            boundaryWarning.SetActive(!fingertipInsideWorkspace);
        }
    }

    public bool IsFingertipInsideWorkspace()
    {
        return fingertipInsideWorkspace;
    }
}