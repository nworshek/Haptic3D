using UnityEngine;

public class Shape2DWorkspaceCalibration : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Participant's HoloLens camera.")]
    public Transform mainCamera;

    [Tooltip("Root object of the Shape2D experiment.")]
    public Transform shape2DExperiment;


    [Header("Placement")]
    [Tooltip("Distance in front of the participant in meters.")]
    public float forwardDistance = 0.7f;

    [Tooltip("Vertical offset relative to the participant's eye level.")]
    public float verticalOffset = -0.15f;


    [Header("Settings")]
    public bool calibrateOnStart = false;


    void Start()
    {
        if (calibrateOnStart)
        {
            Calibrate();
        }
    }


    // =========================================================
    // CALIBRATION
    // =========================================================

    public void Calibrate()
    {
        if (mainCamera == null ||
            shape2DExperiment == null)
        {
            Debug.LogWarning(
                "Shape2D calibration references are missing."
            );

            return;
        }


        // Use only the participant's horizontal viewing
        // direction. Ignore looking upward/downward.
        Vector3 forward =
            mainCamera.forward;

        forward.y = 0f;


        if (forward.sqrMagnitude <
            0.0001f)
        {
            forward =
                Vector3.forward;
        }


        forward.Normalize();


        // Position the Shape2D workspace in front
        // of the participant.
        Vector3 targetPosition =
            mainCamera.position +
            forward *
            forwardDistance;


        targetPosition.y =
            mainCamera.position.y +
            verticalOffset;


        shape2DExperiment.position =
            targetPosition;


        // Rotate the workspace so the 2D plane
        // faces the participant.
        Vector3 directionToParticipant =
            mainCamera.position -
            shape2DExperiment.position;

        directionToParticipant.y = 0f;


        if (directionToParticipant.sqrMagnitude >
            0.0001f)
        {
            shape2DExperiment.rotation =
                Quaternion.LookRotation(
                    -directionToParticipant.normalized,
                    Vector3.up
                );
        }


        Debug.Log(
            "Shape2D workspace calibrated."
        );
    }
}