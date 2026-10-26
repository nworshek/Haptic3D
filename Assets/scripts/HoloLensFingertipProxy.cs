using UnityEngine;
[DefaultExecutionOrder(100)]
public class HoloLensFingertipProxy : MonoBehaviour
{
    // =========================================================
    // PARTICIPANT HAND
    // =========================================================
    public enum ParticipantHand
    {
        Right,
        Left
    }
    // =========================================================
    // REFERENCES
    // =========================================================
    [Header("References")]
    [Tooltip("HoloLens hand tracking component.")]
    public HoloLensHandTracker handTracker;
    [Tooltip("Existing FingertipManager that controls fingertip positions.")]
    public Transform fingertipManager;
    [Tooltip("Visual sphere to hide when hand tracking is lost.")]
    public GameObject fingertipVisual;
    // =========================================================
    // TRACKING
    // =========================================================
    [Header("Tracking")]
    public bool useHandTracking = false;
    // =========================================================
    // PARTICIPANT HAND
    // =========================================================
    [Header("Participant Hand")]
    public ParticipantHand participantHand =
        ParticipantHand.Right;
    // =========================================================
    // HAND OFFSET
    // =========================================================
    [Header("Hand Offset")]
    public Vector3 handOffset =
        Vector3.zero;
    // =========================================================
    // COLLISION
    // =========================================================
    [Header("Collision")]
    [Tooltip("Radius used to keep the tracked fingertip outside solid surfaces.")]
    public float collisionRadius = 0.00625f;
    [Tooltip("Small gap maintained between the fingertip and the surface.")]
    public float skinWidth = 0.001f;
    [Tooltip("Layers that should physically block the tracked fingertip.")]
    public LayerMask collisionLayers = ~0;
    [Tooltip("Number of collision/slide calculations allowed per frame.")]
    [Range(1, 5)]
    public int maxSlideIterations = 3;
    // =========================================================
    // BEHAVIOR
    // =========================================================
    [Header("Behavior")]
    [Tooltip("Hide the visual sphere when hand tracking is lost.")]
    public bool hideWhenHandLost = false;
    // =========================================================
    // PRIVATE VARIABLES
    // =========================================================
    private Vector3 originalPosition;
    // =========================================================
    // START
    // =========================================================
    void Start()
    {
        if (fingertipManager != null)
        {
            originalPosition =
                fingertipManager.position;
        }
        // If tracking is required but no hand is detected,
        // hide only the visual sphere.
        UpdateFingertipVisibility();
    }
    // =========================================================
    // LATE UPDATE (after hand tracker Update)
    // =========================================================
    void LateUpdate()
    {
        if (!useHandTracking)
        {
            return;
        }
        if (handTracker == null ||
            fingertipManager == null)
        {
            SetFingertipVisible(false);
            return;
        }
        if (handTracker.handDetected)
        {
            // -------------------------------------------------
            // DESIRED TRACKED POSITION
            // -------------------------------------------------
            Vector3 desiredPosition =
                handTracker.transform.position +
                handOffset;
            // -------------------------------------------------
            // COLLISION + SURFACE SLIDING
            // -------------------------------------------------
            Vector3 safePosition =
                GetCollisionSafePosition(
                    fingertipManager.position,
                    desiredPosition
                );
            // -------------------------------------------------
            // MOVE FINGERTIP
            // -------------------------------------------------
            fingertipManager.position =
                safePosition;
            // -------------------------------------------------
            // RESTORE VISUAL
            // -------------------------------------------------
            SetFingertipVisible(true);
        }
        else
        {
            // Tracking lost:
            // Hide only the visual sphere.
            // Do not disable FingertipManager.
            SetFingertipVisible(false);
        }
    }
    // =========================================================
    // VISIBILITY
    // =========================================================
    private void SetFingertipVisible(bool visible)
    {
        if (!hideWhenHandLost)
        {
            return;
        }
        if (fingertipVisual == null)
        {
            return;
        }
        if (fingertipVisual.activeSelf != visible)
        {
            fingertipVisual.SetActive(visible);
        }
    }
    private void UpdateFingertipVisibility()
    {
        if (!hideWhenHandLost)
        {
            return;
        }
        if (!useHandTracking)
        {
            SetFingertipVisible(true);
            return;
        }
        bool handValid =
            handTracker != null &&
            handTracker.isActiveAndEnabled &&
            handTracker.handDetected;
        SetFingertipVisible(handValid);
    }
    // =========================================================
    // COLLISION + SURFACE SLIDING
    // =========================================================
    private Vector3 GetCollisionSafePosition(
        Vector3 currentPosition,
        Vector3 desiredPosition
    )
    {
        Vector3 position =
            currentPosition;
        Vector3 remainingMovement =
            desiredPosition -
            currentPosition;
        // Multiple iterations allow the fingertip
        // to slide along solid surfaces.
        for (
            int iteration = 0;
            iteration < maxSlideIterations;
            iteration++
        )
        {
            float distance =
                remainingMovement.magnitude;
            if (distance <= 0.00001f)
            {
                break;
            }
            Vector3 direction =
                remainingMovement /
                distance;
            RaycastHit hit;
            bool blocked =
                Physics.SphereCast(
                    position,
                    collisionRadius,
                    direction,
                    out hit,
                    distance + skinWidth,
                    collisionLayers,
                    QueryTriggerInteraction.Ignore
                );
            // -------------------------------------------------
            // NO COLLISION
            // -------------------------------------------------
            if (!blocked)
            {
                position +=
                    remainingMovement;
                break;
            }
            // -------------------------------------------------
            // MOVE UP TO SURFACE
            // -------------------------------------------------
            float allowedDistance =
                Mathf.Max(
                    0f,
                    hit.distance -
                    skinWidth
                );
            position +=
                direction *
                allowedDistance;
            // -------------------------------------------------
            // REMAINING MOVEMENT
            // -------------------------------------------------
            float movementUsed =
                Mathf.Min(
                    distance,
                    allowedDistance
                );
            Vector3 unusedMovement =
                direction *
                (distance - movementUsed);
            // -------------------------------------------------
            // SLIDE ALONG SURFACE
            // -------------------------------------------------
            Vector3 slideMovement =
                Vector3.ProjectOnPlane(
                    unusedMovement,
                    hit.normal
                );
            // -------------------------------------------------
            // SMALL OUTWARD CORRECTION
            // -------------------------------------------------
            position +=
                hit.normal *
                skinWidth;
            remainingMovement =
                slideMovement;
        }
        return position;
    }
    // =========================================================
    // ENABLE / DISABLE TRACKING
    // =========================================================
    public void EnableHandTracking()
    {
        useHandTracking = true;
        UpdateFingertipVisibility();
    }
    public void DisableHandTracking()
    {
        useHandTracking = false;
        // Restore the visual for desktop simulation.
        if (fingertipVisual != null)
        {
            fingertipVisual.SetActive(true);
        }
    }
    // =========================================================
    // RESET
    // =========================================================
    public void ResetToOriginalPosition()
    {
        if (fingertipManager != null)
        {
            fingertipManager.position =
                originalPosition;
        }
    }
    // =========================================================
    // PARTICIPANT HAND
    // =========================================================
    public void SetParticipantHandRight()
    {
        participantHand =
            ParticipantHand.Right;
        Debug.Log(
            "Participant hand set to RIGHT."
        );
    }
    public void SetParticipantHandLeft()
    {
        participantHand =
            ParticipantHand.Left;
        Debug.Log(
            "Participant hand set to LEFT."
        );
    }
    public string GetParticipantHand()
    {
        return participantHand.ToString();
    }
    // =========================================================
    // SAFETY / DISABLE
    // =========================================================
    void OnDisable()
    {
        // Restore visual if this tracking component
        // is disabled for desktop simulation.
        if (fingertipVisual != null)
        {
            fingertipVisual.SetActive(true);
        }
    }
}
