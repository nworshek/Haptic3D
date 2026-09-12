using UnityEngine;

public class HapticObject : MonoBehaviour
{
    public enum SurfaceType
    {
        Flat,
        Ridges,
        Bumps
    }

    [Header("Surface Type")]
    public SurfaceType surfaceType;

    [Header("Base Haptic Intensity")]
    [Range(0, 100)]
    public int baseIntensity = 35;

    [Header("Edge / Corner Feedback")]
    [Range(0, 100)]
    public int edgeBoost = 15;

    [Range(0, 100)]
    public int cornerBoost = 25;

    [Header("Surface Pattern")]
    public float patternFrequency = 6f;

    [Range(0, 50)]
    public int patternStrength = 20;

    [Header("Debug")]
    public bool showDebugMessages = true;

    public int GetIntensityAtPosition(Vector3 fingertipWorldPosition)
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            return baseIntensity;
        }

        // Convert fingertip position into cube-local coordinates.
        Vector3 localPoint =
            transform.InverseTransformPoint(fingertipWorldPosition);

        Bounds bounds =
            meshFilter.sharedMesh.bounds;

        Vector3 center =
            bounds.center;

        Vector3 halfSize =
            bounds.extents;

        Vector3 relative =
            localPoint - center;

        // Normalize position so cube surfaces are approximately ±1.
        float nx =
            Mathf.Abs(relative.x / halfSize.x);

        float ny =
            Mathf.Abs(relative.y / halfSize.y);

        float nz =
            Mathf.Abs(relative.z / halfSize.z);

        int surfaceCount = 0;

        const float edgeThreshold = 0.80f;

        if (nx >= edgeThreshold)
            surfaceCount++;

        if (ny >= edgeThreshold)
            surfaceCount++;

        if (nz >= edgeThreshold)
            surfaceCount++;

        int intensity = baseIntensity;

        // ----------------------------------------------------
        // SURFACE-SPECIFIC TEXTURE
        // ----------------------------------------------------

        switch (surfaceType)
        {
            case SurfaceType.Flat:
                // Flat stays mostly constant.
                break;


            case SurfaceType.Ridges:
            {
                // Creates alternating vibration strength
                // as the fingertip moves across the surface.
                float wave =
                    Mathf.Sin(
                        (
                            localPoint.x +
                            localPoint.y +
                            localPoint.z
                        )
                        * patternFrequency * 100f
                    );

                float normalizedWave =
                    (wave + 1f) * 0.5f;

                intensity +=
                    Mathf.RoundToInt(
                        normalizedWave *
                        patternStrength
                    );

                break;
            }


            case SurfaceType.Bumps:
            {
                // Two overlapping waves make the vibration
                // rise and fall more like individual bumps.
                float waveX =
                    Mathf.Sin(
                        localPoint.x *
                        patternFrequency *
                        100f
                    );

                float waveY =
                    Mathf.Sin(
                        localPoint.y *
                        patternFrequency *
                        100f
                    );

                float waveZ =
                    Mathf.Sin(
                        localPoint.z *
                        patternFrequency *
                        100f
                    );

                float bumpPattern =
                    Mathf.Abs(
                        waveX *
                        waveY *
                        waveZ
                    );

                intensity +=
                    Mathf.RoundToInt(
                        bumpPattern *
                        patternStrength
                    );

                break;
            }
        }

        // ----------------------------------------------------
        // EDGE / CORNER BOOST
        // ----------------------------------------------------

        if (surfaceCount >= 3)
        {
            intensity += cornerBoost;
        }
        else if (surfaceCount >= 2)
        {
            intensity += edgeBoost;
        }

        return Mathf.Clamp(
            intensity,
            0,
            100
        );
    }

    public void ActivateHaptics()
    {
        if (showDebugMessages)
        {
            Debug.Log(
                "Haptic contact: " +
                gameObject.name +
                " | Surface: " +
                surfaceType
            );
        }
    }

    public void DeactivateHaptics()
    {
        if (showDebugMessages)
        {
            Debug.Log(
                "Haptic contact ended: " +
                gameObject.name
            );
        }
    }
}