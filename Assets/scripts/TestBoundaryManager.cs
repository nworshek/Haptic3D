using UnityEngine;

public class TestBoundaryManager : MonoBehaviour
{
    [Header("Tracked User")]
    public Transform xrRig;

    [Header("Boundary Limits")]
    public float minX = -1.0f;
    public float maxX = 1.0f;
    public float minZ = -1.0f;
    public float maxZ = 1.0f;

    [Header("Warning Zone")]
    public float warningDistance = 0.20f;

    [Header("Warning Object")]
    public GameObject warningObject;

    [Header("Debug")]
    public bool showDebugMessages = true;

    private bool wasWarningActive = false;
    private bool wasOutsideBoundary = false;

    void Start()
    {
        // Start with the warning hidden.
        if (warningObject != null)
        {
            warningObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning(
                "TestBoundaryManager: Warning Object is not assigned."
            );
        }
    }

    void Update()
    {
        if (xrRig == null)
        {
            return;
        }

        CheckBoundary();
    }

    private void CheckBoundary()
    {
        Vector3 position = xrRig.position;

        // Check whether the user has completely crossed
        // outside the defined testing area.
        bool outsideBoundary =
            position.x < minX ||
            position.x > maxX ||
            position.z < minZ ||
            position.z > maxZ;

        // Check whether the user is getting close
        // to any edge of the testing area.
        bool nearBoundary =
            position.x <= minX + warningDistance ||
            position.x >= maxX - warningDistance ||
            position.z <= minZ + warningDistance ||
            position.z >= maxZ - warningDistance;

        bool shouldShowWarning =
            outsideBoundary || nearBoundary;

        // Turn the warning text on/off.
        if (warningObject != null)
        {
            warningObject.SetActive(shouldShowWarning);
        }

        // ----------------------------------------------------
        // DEBUG MESSAGES
        // Only print when the user's boundary state changes.
        // ----------------------------------------------------

        if (showDebugMessages)
        {
            // User has just crossed completely outside.
            if (outsideBoundary && !wasOutsideBoundary)
            {
                Debug.LogWarning(
                    "USER OUTSIDE TEST BOUNDARY | Position: " +
                    position
                );
            }

            // User has entered the warning zone,
            // but is not yet outside.
            else if (
                shouldShowWarning &&
                !wasWarningActive &&
                !outsideBoundary
            )
            {
                Debug.LogWarning(
                    "USER APPROACHING TEST BOUNDARY | Position: " +
                    position
                );
            }

            // User has returned completely to the safe area.
            else if (
                !shouldShowWarning &&
                wasWarningActive
            )
            {
                Debug.Log(
                    "USER RETURNED TO SAFE TEST AREA | Position: " +
                    position
                );
            }
        }

        // Save the current state for the next frame.
        wasWarningActive = shouldShowWarning;
        wasOutsideBoundary = outsideBoundary;
    }
}