using UnityEngine;

public class HapticPointerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2.0f;

    [Header("Current Contact")]
    public HapticObject currentObject;

    [Header("Visual Feedback")]
    public Material idleMaterial;
    public Material contactMaterial;

    private Renderer pointerRenderer;

    void Start()
    {
        pointerRenderer = GetComponent<Renderer>();

        if (pointerRenderer != null && idleMaterial != null)
        {
            pointerRenderer.material = idleMaterial;
        }
    }

    void Update()
    {
        MovePointer();
    }

    private void MovePointer()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        float depth = 0f;

        if (Input.GetKey(KeyCode.Q))
        {
            depth = -1f;
        }

        if (Input.GetKey(KeyCode.E))
        {
            depth = 1f;
        }

        Vector3 movement = new Vector3(
            horizontal,
            vertical,
            depth
        );

        transform.Translate(
            movement * moveSpeed * Time.deltaTime,
            Space.World
        );
    }

    private void OnTriggerEnter(Collider other)
    {
        HapticObject hapticObject =
            other.GetComponentInParent<HapticObject>();

        if (hapticObject != null)
        {
            currentObject = hapticObject;

            currentObject.ActivateHaptics();

            SetContactVisual();

            Debug.Log(
                "POINTER ENTERED: " +
                currentObject.gameObject.name +
                " | Surface: " +
                currentObject.surfaceType +
                " | Intensity: " +
                currentObject.baseIntensity
            );
        }
    }

    private void OnTriggerStay(Collider other)
    {
        HapticObject hapticObject =
            other.GetComponentInParent<HapticObject>();

        if (hapticObject != null)
        {
            currentObject = hapticObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        HapticObject hapticObject =
            other.GetComponentInParent<HapticObject>();

        if (hapticObject != null &&
            hapticObject == currentObject)
        {
            currentObject.DeactivateHaptics();

            Debug.Log(
                "POINTER EXITED: " +
                currentObject.gameObject.name
            );

            currentObject = null;

            SetIdleVisual();
        }
    }

    private void SetContactVisual()
    {
        if (pointerRenderer != null &&
            contactMaterial != null)
        {
            pointerRenderer.material = contactMaterial;
        }
    }

    private void SetIdleVisual()
    {
        if (pointerRenderer != null &&
            idleMaterial != null)
        {
            pointerRenderer.material = idleMaterial;
        }
    }
}