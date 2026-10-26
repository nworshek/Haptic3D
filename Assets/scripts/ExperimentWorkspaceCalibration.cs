using UnityEngine;

public class ExperimentWorkspaceCalibration : MonoBehaviour
{
    [Header("References")]
    public Transform mainCamera;
    public Transform experimentWorkspace;

    [Header("Workspace Placement")]
    [Tooltip("Distance in meters from the participant to the experiment workspace.")]
    public float forwardDistance = 0.7f;

    [Header("Settings")]
    [Tooltip("Automatically calibrate when the scene starts.")]
    public bool calibrateOnStart = false;

    void Start()
    {
        if (calibrateOnStart)
        {
            CalibrateWorkspace();
        }
    }

    public void CalibrateWorkspace()
    {
        if (mainCamera == null)
        {
            Debug.LogWarning(
                "Workspace calibration failed: Main Camera not assigned."
            );
            return;
        }

        if (experimentWorkspace == null)
        {
            Debug.LogWarning(
                "Workspace calibration failed: ExperimentWorkspace not assigned."
            );
            return;
        }

        // Use the participant's horizontal viewing direction.
        // Ignore head tilt so looking slightly up/down does not
        // move the experiment vertically.
        Vector3 forward = mainCamera.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.001f)
        {
            forward = Vector3.forward;
        }

        forward.Normalize();

        // Position the workspace a fixed distance in front
        // of the participant.
        Vector3 targetPosition =
            mainCamera.position +
            forward * forwardDistance;

        // Preserve the workspace's existing vertical position.
        targetPosition.y = experimentWorkspace.position.y;

        experimentWorkspace.position = targetPosition;

        // Rotate the workspace so its forward orientation follows
        // the participant's horizontal starting direction.
        float yaw = Mathf.Atan2(
            forward.x,
            forward.z
        ) * Mathf.Rad2Deg;

        experimentWorkspace.rotation =
            Quaternion.Euler(
                0f,
                yaw,
                0f
            );

        Debug.Log(
            "Experiment workspace calibrated. " +
            "Position: " +
            experimentWorkspace.position +
            " | Forward Distance: " +
            forwardDistance +
            " m"
        );
    }
}