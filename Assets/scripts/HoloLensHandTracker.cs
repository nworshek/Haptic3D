using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using UnityEngine.XR.WSA.Input;
#endif

public class HoloLensHandTracker : MonoBehaviour
{
    [Header("Tracking Status")]
    public bool handDetected = false;

    [Header("Tracked Hand Position (Raw World Space)")]
    public Vector3 trackedPosition;

    [Header("Adaptive Tracking Settings")]
    [Tooltip("Smoothing rate for slow movements. Lower values reduce jitter but add lag.")]
    [Range(1f, 30f)] public float positionSmoothing = 7f;

    [Tooltip("Smoothing rate for faster movements. Higher values respond more quickly.")]
    [Range(1f, 40f)] public float fastMovementSmoothing = 22f;

    [Tooltip("Movement speed in m/s at which fast smoothing is fully applied.")]
    [Range(0.05f, 2f)] public float fastMovementSpeed = 0.5f;

    [Tooltip("Offset in meters, expressed in the HoloLens camera's local axes. This adjusts an estimated hand location, not a measured fingertip.")]
    public Vector3 cameraRelativeOffset = Vector3.zero;

    [Tooltip("Camera reference for offset orientation. If null, Camera.main is used.")]
    public Transform headCamera;

    [Header("Visual Debug")]
    public GameObject handTrackingMarker;

    [Tooltip("How long the debug marker may remain at its last location after tracking loss. Does not affect handDetected or haptics.")]
    [Range(0f, 0.25f)] public float markerLossGraceSeconds = 0.08f;

    [Header("Debug")]
    public bool showDebugLog = false;

    private Vector3 smoothedPosition;
    private Vector3 previousRawPosition;
    private bool smoothingInitialized;
    private float lastDetectionTime = float.NegativeInfinity;

#if UNITY_WSA && !UNITY_EDITOR
    private readonly InteractionSourceState[] sourceStates = new InteractionSourceState[10];
#endif

    private void Start()
    {
        handDetected = false;
        trackedPosition = transform.position;
        smoothedPosition = transform.position;
        smoothingInitialized = false;
        if (headCamera == null && Camera.main != null)
            headCamera = Camera.main.transform;
        if (handTrackingMarker != null)
            handTrackingMarker.SetActive(false);
    }

    private void Update()
    {
#if UNITY_WSA && !UNITY_EDITOR
        UpdateHoloLensHand();
#endif
        if (!handDetected && handTrackingMarker != null &&
            Time.unscaledTime - lastDetectionTime >= markerLossGraceSeconds &&
            handTrackingMarker.activeSelf)
        {
            handTrackingMarker.SetActive(false);
        }
    }

#if UNITY_WSA && !UNITY_EDITOR
    private void UpdateHoloLensHand()
    {
        int sourceCount = InteractionManager.GetCurrentReading(sourceStates);
        for (int i = 0; i < sourceCount; i++)
        {
            InteractionSourceState state = sourceStates[i];
            if (state.source.kind != InteractionSourceKind.Hand)
                continue;

            Vector3 handPosition;
            if (state.sourcePose.TryGetPosition(out handPosition, InteractionSourceNode.Grip))
            {
                SetTrackedHandPosition(handPosition);
                return;
            }
        }
        ClearTrackedHand();
    }
#endif

    public void SetTrackedHandPosition(Vector3 position)
    {
        trackedPosition = position;
        Vector3 offset = cameraRelativeOffset;
        if (headCamera != null)
            offset = headCamera.TransformDirection(cameraRelativeOffset);
        Vector3 target = position + offset;

        if (!smoothingInitialized)
        {
            smoothedPosition = target;
            previousRawPosition = target;
            smoothingInitialized = true;
        }
        else
        {
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            float speed = Vector3.Distance(target, previousRawPosition) / dt;
            float t = Mathf.Clamp01(speed / Mathf.Max(fastMovementSpeed, 0.001f));
            float rate = Mathf.Lerp(positionSmoothing,
                Mathf.Max(positionSmoothing, fastMovementSmoothing), t);
            float alpha = 1f - Mathf.Exp(-rate * dt);
            smoothedPosition = Vector3.Lerp(smoothedPosition, target, alpha);
            previousRawPosition = target;
        }

        // InteractionSourcePose supplies world-space coordinates.
        transform.position = smoothedPosition;
        lastDetectionTime = Time.unscaledTime;

        if (handTrackingMarker != null && !handTrackingMarker.activeSelf)
            handTrackingMarker.SetActive(true);

        if (!handDetected && showDebugLog)
            Debug.Log("HoloLens hand detected.");
        handDetected = true;
    }

    public void ClearTrackedHand()
    {
        if (handDetected && showDebugLog)
            Debug.Log("HoloLens hand lost.");

        // Clear immediately: haptic safety must never use visual grace time.
        handDetected = false;
        smoothingInitialized = false;
        if (handTrackingMarker != null &&
            Time.unscaledTime - lastDetectionTime >= markerLossGraceSeconds)
            handTrackingMarker.SetActive(false);
    }
}
