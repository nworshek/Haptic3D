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
    // MOTOR NUMBER
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


    // =========================================================
    // MOTOR LABEL
    // =========================================================

    private string GetMotorLabel()
    {
        return "M" + GetMotorNumber();
    }


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        fingertipRenderer =
            GetComponent<Renderer>();

        originalScale =
            transform.localScale;

        SetIdleVisual();

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
            UpdatePulse();
            UpdateHapticOutput();
        }
        else
        {
            transform.localScale =
                originalScale;
        }
    }


    // =========================================================
    // HAPTIC SURFACE DETECTION
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

        if (detectedObject == null)
        {
            if (isTouching &&
                currentObject != null)
            {
                EndHapticContact();
            }

            return;
        }

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
    // CONTACT START
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

        if (TrialLogger.Instance != null)
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

        UpdateHapticOutput(true);
    }


    // =========================================================
    // CONTACT END
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

        if (esp32Serial != null)
        {
            esp32Serial.MotorOff(
                GetMotorNumber()
            );
        }

        if (TrialLogger.Instance != null)
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
    // HAPTIC OUTPUT
    // =========================================================

    private void UpdateHapticOutput(
        bool forceUpdate = false
    )
    {
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

        esp32Serial.SetMotorIntensity(
            GetMotorNumber(),
            newIntensity
        );

        if (TrialLogger.Instance != null)
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
    // TRIAL STATE
    // =========================================================

    private string GetCurrentTrialState()
    {
        if (trialManager == null)
        {
            return "Unknown";
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
    // DEBUG VISUALIZATION
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            detectionRadius
        );
    }
}