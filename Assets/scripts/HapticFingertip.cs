using UnityEngine;

public class HapticFingertip : MonoBehaviour
{
    public enum FingerType
    {
        Index,
        Middle,
        Ring
    }

    [Header("Finger")]
    public FingerType finger = FingerType.Index;

    [Header("ESP32 Connection")]
    public ESP32Serial esp32Serial;

    [Header("Current Contact")]
    public HapticObject currentObject;

    [Header("Haptic Detection")]
    public float detectionRadius = 0.020f;

    [Header("Visual Feedback")]
    public Renderer visualRenderer;
    public Material idleMaterial;
    public Material contactMaterial;

    [Header("Haptic Updating")]
    public float hapticUpdateRate = 0.05f;
    public int minimumIntensityChange = 2;

    [Header("Pulse Settings")]
    public float minimumPulseSpeed = 2f;
    public float maximumPulseSpeed = 10f;
    public float minimumPulseAmount = 0.02f;
    public float maximumPulseAmount = 0.20f;

    private Renderer fingertipRenderer;
    private Vector3 originalScale;

    private bool isTouching = false;

    private float nextHapticUpdateTime = 0f;
    private int lastSentIntensity = -1;

    private TrialManager trialManager;


    // =========================================================
    // MOTOR MAPPING
    // =========================================================

    public int GetMotorNumber()
    {
        switch (finger)
        {
            case FingerType.Index:
                return 1;

            case FingerType.Middle:
                return 2;

            case FingerType.Ring:
                return 3;

            default:
                return 1;
        }
    }


    private string GetMotorLabel()
    {
        return "M" + GetMotorNumber();
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        if (visualRenderer != null)
        {
            fingertipRenderer =
                visualRenderer;
        }
        else
        {
            fingertipRenderer =
                GetComponent<Renderer>();
        }

        originalScale =
            transform.localScale;

        SetIdleVisual();


        // -----------------------------------------------------
        // Find ESP32
        // -----------------------------------------------------

        if (esp32Serial == null)
        {
            esp32Serial =
                FindObjectOfType<ESP32Serial>();
        }

        if (esp32Serial == null)
        {
            Debug.LogWarning(
                "HapticFingertip: ESP32Serial could not be found."
            );
        }


        // -----------------------------------------------------
        // Find TrialManager
        // -----------------------------------------------------

        trialManager =
            FindObjectOfType<TrialManager>();

        if (trialManager == null)
        {
            Debug.LogWarning(
                "HapticFingertip: TrialManager could not be found."
            );
        }


        Debug.Log(
            gameObject.name +
            " configured as " +
            finger +
            " | Motor " +
            GetMotorLabel()
        );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    void Update()
    {
        DetectHapticSurface();


        if (isTouching &&
            currentObject != null)
        {
            // Keep the fingertip visual at its calibrated size.
            transform.localScale =
                originalScale;


            // Motor output is permitted during:
            //
            // 1. An ACTIVE official Haptic Only trial
            //
            // OR
            //
            // 2. Experimenter Test Mode with test haptics enabled.
            if (HapticsAreAllowed())
            {
                UpdateHapticOutput();
            }
            else
            {
                EnsureMotorOff();
            }
        }
        else
        {
            transform.localScale =
                originalScale;
        }
    }


    // =========================================================
    // HAPTIC PERMISSION
    // =========================================================

    private bool HapticsAreAllowed()
    {
        if (trialManager == null)
        {
            return false;
        }


        // -----------------------------------------------------
        // OFFICIAL PARTICIPANT TRIAL
        // -----------------------------------------------------
        //
        // Haptics are allowed during the study ONLY when:
        //
        // 1. A trial is actively running
        // 2. The condition is Haptic Only
        //
        // This preserves the normal participant experiment
        // behavior and prevents vibration during Visual
        // Exploration, Neutral, or Prepared trials.

        bool officialHapticTrial =
            trialManager.trialActive &&
            trialManager.currentState ==
                TrialManager.TrialState.HapticOnly;


        // -----------------------------------------------------
        // EXPERIMENTER TEST MODE
        // -----------------------------------------------------
        //
        // This is completely separate from an official trial.
        // The experimenter must:
        //
        // 1. Enter Experimenter Test Mode
        // 2. Explicitly enable Test Haptics
        //
        // No official trial needs to be active.

        bool experimenterTesting =
            trialManager.experimenterTestMode &&
            trialManager.testHapticsEnabled;


        return
            officialHapticTrial ||
            experimenterTesting;
    }


    // =========================================================
    // ENSURE THIS MOTOR IS OFF
    // =========================================================

    private void EnsureMotorOff()
    {
        if (esp32Serial == null)
        {
            return;
        }


        // Only send the OFF command when necessary.
        // This prevents sending M#:0 every frame.

        if (lastSentIntensity != 0)
        {
            esp32Serial.MotorOff(
                GetMotorNumber()
            );

            lastSentIntensity = 0;
        }
    }


    // =========================================================
    // DETECT HAPTIC SURFACE
    // =========================================================

    private void DetectHapticSurface()
    {
        Collider[] nearbyColliders =
            Physics.OverlapSphere(
                transform.position,
                detectionRadius,
                ~0,
                QueryTriggerInteraction.Collide
            );


        HapticObject detectedObject =
            null;


        foreach (Collider col in nearbyColliders)
        {
            HapticObject hapticObject =
                col.GetComponentInParent<HapticObject>();


            if (hapticObject != null)
            {
                detectedObject =
                    hapticObject;

                break;
            }
        }


        // -----------------------------------------------------
        // No object detected
        // -----------------------------------------------------

        if (detectedObject == null)
        {
            if (isTouching &&
                currentObject != null)
            {
                EndHapticContact();
            }

            return;
        }


        // -----------------------------------------------------
        // New object detected
        // -----------------------------------------------------

        if (detectedObject != currentObject)
        {
            if (isTouching &&
                currentObject != null)
            {
                EndHapticContact();
            }


            BeginHapticContact(
                detectedObject
            );
        }
    }


    // =========================================================
    // BEGIN CONTACT
    // =========================================================

    private void BeginHapticContact(
        HapticObject hapticObject
    )
    {
        currentObject =
            hapticObject;

        isTouching =
            true;

        lastSentIntensity =
            -1;


        currentObject.ActivateHaptics();

        SetContactVisual();


        Debug.Log(
            finger +
            " CONTACT START: " +
            currentObject.gameObject.name
        );


        // -----------------------------------------------------
        // LOG CONTACT
        // -----------------------------------------------------

        if (TrialLogger.Instance != null &&
            !IsExperimenterTestMode())
        {
            TrialLogger.Instance.LogEvent(
                GetCurrentTrialState(),
                currentObject.gameObject.name,
                "Contact Start",
                -1,
                finger.ToString(),
                GetMotorLabel()
            );
        }


        // -----------------------------------------------------
        // INITIAL MOTOR OUTPUT
        // -----------------------------------------------------
        //
        // HapticsAreAllowed() handles both:
        //
        // - Official active Haptic Only trials
        // - Experimenter Test Mode

        if (HapticsAreAllowed())
        {
            UpdateHapticOutput(
                true
            );
        }
        else
        {
            EnsureMotorOff();
        }
    }


    // =========================================================
    // END CONTACT
    // =========================================================

    private void EndHapticContact()
    {
        if (currentObject == null)
        {
            return;
        }


        string objectName =
            currentObject.gameObject.name;


        currentObject.DeactivateHaptics();


        // -----------------------------------------------------
        // Turn off ONLY this fingertip's motor
        // -----------------------------------------------------

        if (esp32Serial != null)
        {
            esp32Serial.MotorOff(
                GetMotorNumber()
            );
        }


        // -----------------------------------------------------
        // Log contact end
        //
        // Experimenter Test Mode is intentionally excluded
        // from official participant logging.
        // -----------------------------------------------------

        if (TrialLogger.Instance != null &&
            !IsExperimenterTestMode())
        {
            TrialLogger.Instance.LogEvent(
                GetCurrentTrialState(),
                objectName,
                "Contact End",
                -1,
                finger.ToString(),
                GetMotorLabel()
            );
        }


        Debug.Log(
            finger +
            " CONTACT END: " +
            objectName
        );


        lastSentIntensity =
            -1;

        currentObject =
            null;

        isTouching =
            false;


        transform.localScale =
            originalScale;


        SetIdleVisual();
    }


    // =========================================================
    // UPDATE HAPTIC OUTPUT
    // =========================================================

    private void UpdateHapticOutput(
        bool forceUpdate = false
    )
    {
        // -----------------------------------------------------
        // SECONDARY SAFETY CHECK
        // -----------------------------------------------------
        //
        // Even if this method is called accidentally,
        // HapticsAreAllowed() must approve the motor output.

        if (!HapticsAreAllowed())
        {
            EnsureMotorOff();

            return;
        }


        if (currentObject == null ||
            esp32Serial == null)
        {
            return;
        }


        if (!forceUpdate &&
            Time.time < nextHapticUpdateTime)
        {
            return;
        }


        nextHapticUpdateTime =
            Time.time +
            hapticUpdateRate;


        int newIntensity =
            currentObject
            .GetIntensityAtPosition(
                transform.position
            );


        bool shouldSend =
            forceUpdate ||
            lastSentIntensity < 0 ||
            Mathf.Abs(
                newIntensity -
                lastSentIntensity
            ) >= minimumIntensityChange;


        if (!shouldSend)
        {
            return;
        }


        // -----------------------------------------------------
        // SEND INDEPENDENT MOTOR INTENSITY
        // -----------------------------------------------------

        esp32Serial.SetMotorIntensity(
            GetMotorNumber(),
            newIntensity
        );


        // -----------------------------------------------------
        // LOG HAPTIC UPDATE
        //
        // Experimenter Test Mode is intentionally NOT written
        // into the official participant trial log.
        // -----------------------------------------------------

        if (TrialLogger.Instance != null &&
            !IsExperimenterTestMode())
        {
            TrialLogger.Instance.LogEvent(
                GetCurrentTrialState(),
                currentObject.gameObject.name,
                "Haptic Update",
                newIntensity,
                finger.ToString(),
                GetMotorLabel()
            );
        }


        lastSentIntensity =
            newIntensity;
    }


    // =========================================================
    // EXPERIMENTER TEST MODE CHECK
    // =========================================================

    private bool IsExperimenterTestMode()
    {
        if (trialManager == null)
        {
            return false;
        }

        return
            trialManager.experimenterTestMode;
    }


    // =========================================================
    // VISUAL PULSE
    // =========================================================

    private void UpdatePulse()
    {
        if (currentObject == null)
        {
            return;
        }


        int currentIntensity =
            currentObject
            .GetIntensityAtPosition(
                transform.position
            );


        float normalizedIntensity =
            Mathf.Clamp01(
                currentIntensity / 100f
            );


        float pulseSpeed =
            Mathf.Lerp(
                minimumPulseSpeed,
                maximumPulseSpeed,
                normalizedIntensity
            );


        float pulseAmount =
            Mathf.Lerp(
                minimumPulseAmount,
                maximumPulseAmount,
                normalizedIntensity
            );


        float pulse =
            Mathf.Sin(
                Time.time *
                pulseSpeed
            );


        float scaleMultiplier =
            1f +
            pulse *
            pulseAmount;


        transform.localScale =
            originalScale *
            scaleMultiplier;
    }


    // =========================================================
    // CURRENT TRIAL STATE
    // =========================================================

    private string GetCurrentTrialState()
    {
        if (trialManager == null)
        {
            return "Unknown";
        }


        if (trialManager.experimenterTestMode)
        {
            return "ExperimenterTest";
        }


        return
            trialManager
            .currentState
            .ToString();
    }


    // =========================================================
    // VISUAL FEEDBACK
    // =========================================================

    private void SetContactVisual()
    {
        if (fingertipRenderer != null &&
            contactMaterial != null)
        {
            fingertipRenderer.material =
                contactMaterial;
        }
    }


    private void SetIdleVisual()
    {
        if (fingertipRenderer != null &&
            idleMaterial != null)
        {
            fingertipRenderer.material =
                idleMaterial;
        }
    }


    // =========================================================
    // FAIL-SAFE
    // =========================================================

    private void OnDisable()
    {
        if (esp32Serial != null)
        {
            esp32Serial.MotorOff(
                GetMotorNumber()
            );
        }


        currentObject =
            null;

        isTouching =
            false;

        lastSentIntensity =
            -1;
    }


    // =========================================================
    // DEBUG GIZMO
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}