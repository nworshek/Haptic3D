
using UnityEngine;

[DefaultExecutionOrder(300)]
public class Shape2DHapticController : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [Tooltip("Detector responsible for Square/Circle/Triangle detection.")]
    public Shape2DDetector shapeDetector;

    [Tooltip("Existing shared ESP32 communication controller.")]
    public ESP32Serial esp32Serial;

    [Tooltip("HoloLens hand tracking component.")]
    public HoloLensHandTracker handTracker;


    // =========================================================
    // HAPTIC SETTINGS
    // =========================================================

    [Header("Haptic Settings")]
    [Range(0, 100)]
    [Tooltip("Motor intensity anywhere inside the active shape.")]
    public int boundaryIntensity = 60;

    [Tooltip("M1 = index finger.")]
    public int motorNumber = 1;


    // =========================================================
    // CONTROL
    // =========================================================

    [Header("Control")]
    [Tooltip("Master switch for Shape2D haptic output.")]
    public bool hapticsEnabled = false;


    // =========================================================
    // HAND TRACKING SAFETY
    // =========================================================

    [Header("Hand Tracking Safety")]
    [Tooltip("Enable for HoloLens deployment. Disable for Unity keyboard simulation.")]
    public bool requireHandTracking = false;

    [Tooltip("Current hand tracking safety status.")]
    public bool handTrackingValid = false;


    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]
    [Tooltip("Current motor intensity requested by Shape2D.")]
    public int currentIntensity = 0;

    [Tooltip("True when the fingertip is inside the active shape.")]
    public bool boundaryContact = false;


    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================

    private int lastSentIntensity = -1;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        lastSentIntensity = -1;

        SetMotorIntensity(0);
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    void LateUpdate()
    {
        // -----------------------------------------------------
        // CHECK HAND TRACKING
        // -----------------------------------------------------

        if (requireHandTracking)
        {
            handTrackingValid =
                handTracker != null &&
                handTracker.isActiveAndEnabled &&
                handTracker.handDetected;
        }
        else
        {
            // Simulation does not require HoloLens tracking.
            handTrackingValid = true;
        }


        // -----------------------------------------------------
        // TRACKING LOSS SAFETY
        // -----------------------------------------------------

        if (!handTrackingValid)
        {
            boundaryContact = false;

            SetMotorIntensity(0);

            return;
        }


        // -----------------------------------------------------
        // VALIDATE SHAPE DETECTOR
        // -----------------------------------------------------

        if (shapeDetector == null ||
            !shapeDetector.isActiveAndEnabled)
        {
            boundaryContact = false;

            SetMotorIntensity(0);

            return;
        }


        // -----------------------------------------------------
        // CHECK SHAPE CONTACT
        // -----------------------------------------------------

        boundaryContact =
            shapeDetector.IsTouchingBoundary();


        // -----------------------------------------------------
        // MASTER HAPTIC ENABLE
        // -----------------------------------------------------

        if (!hapticsEnabled)
        {
            SetMotorIntensity(0);

            return;
        }


        // -----------------------------------------------------
        // MOTOR OUTPUT
        // -----------------------------------------------------

        if (boundaryContact)
        {
            SetMotorIntensity(boundaryIntensity);
        }
        else
        {
            SetMotorIntensity(0);
        }
    }


    // =========================================================
    // MOTOR OUTPUT
    // =========================================================

    private void SetMotorIntensity(int intensity)
    {
        intensity = Mathf.Clamp(intensity, 0, 100);

        currentIntensity = intensity;

        // Avoid repeatedly sending identical commands.
        if (intensity == lastSentIntensity)
        {
            return;
        }

        // Communication controller must be assigned.
        if (esp32Serial == null)
        {
            return;
        }

        // This records the requested intensity.
        // It does not confirm physical ESP32 delivery.
        lastSentIntensity = intensity;

        if (intensity > 0)
        {
            esp32Serial.SetMotorIntensity(
                motorNumber,
                intensity
            );
        }
        else
        {
            esp32Serial.MotorOff(
                motorNumber
            );
        }
    }


    // =========================================================
    // PUBLIC CONTROLS
    // =========================================================

    public void EnableHaptics()
    {
        hapticsEnabled = true;

        Debug.Log("Shape2D haptics ENABLED.");
    }


    public void DisableHaptics()
    {
        hapticsEnabled = false;

        lastSentIntensity = -1;

        SetMotorIntensity(0);

        Debug.Log("Shape2D haptics DISABLED.");
    }


    // =========================================================
    // SAFETY
    // =========================================================

    public void EmergencyStop()
    {
        hapticsEnabled = false;

        boundaryContact = false;

        lastSentIntensity = -1;

        SetMotorIntensity(0);

        Debug.LogWarning("Shape2D emergency stop.");
    }


    void OnDisable()
    {
        hapticsEnabled = false;

        boundaryContact = false;

        lastSentIntensity = -1;

        SetMotorIntensity(0);
    }


    void OnApplicationQuit()
    {
        if (esp32Serial != null)
        {
            esp32Serial.MotorOff(motorNumber);
        }
    }
}
