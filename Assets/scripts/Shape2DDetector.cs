using UnityEngine;

[DefaultExecutionOrder(200)]
public class Shape2DDetector : MonoBehaviour
{
    // =========================================================
    // SHAPE TYPE
    // =========================================================

    public enum ShapeType
    {
        Square,
        Circle,
        Triangle
    }


    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    [Tooltip("The tracked fingertip used for shape exploration.")]
    public Transform fingertip;


    // =========================================================
    // ACTIVE SHAPE
    // =========================================================

    [Header("Active Shape")]
    public ShapeType activeShape = ShapeType.Square;


    // =========================================================
    // SQUARE
    // =========================================================

    [Header("Square")]
    public float squareWidth = 0.15f;
    public float squareHeight = 0.15f;


    // =========================================================
    // CIRCLE
    // =========================================================

    [Header("Circle")]
    public float circleRadius = 0.075f;


    // =========================================================
    // TRIANGLE
    // =========================================================

    [Header("Triangle")]
    [Tooltip("Width of the triangle base.")]
    public float triangleWidth = 0.15f;

    [Tooltip("Height of the triangle.")]
    public float triangleHeight = 0.15f;


    // =========================================================
    // DETECTION
    // =========================================================

    [Header("Detection")]
    [Tooltip("How close the fingertip must be to the interaction plane.")]
    public float planeDepthTolerance = 0.02f;

    [Tooltip("Exit depth tolerance while already inside a shape. Must be >= entry tolerance.")]
    public float planeExitTolerance = 0.03f;

    [Tooltip("Extra outward margin while already touching a shape (meters).")]
    public float shapeExitMargin = 0.005f;


    // =========================================================
    // DEBUG / STATUS
    // =========================================================

    [Header("Debug")]
    public bool showDebugShape = true;

    [Tooltip("True while the fingertip is inside the active shape.")]
    public bool touchingBoundary = false;

    [Tooltip("Distance of the fingertip from the interaction plane.")]
    public float fingertipDepthFromPlane = 0f;


    // =========================================================
    // UPDATE
    // =========================================================

    void LateUpdate()
    {
        if (fingertip == null)
        {
            touchingBoundary = false;
            fingertipDepthFromPlane = 0f;
            return;
        }

        // Convert fingertip world position into the local
        // coordinate system of ShapeInteractionPlane.
        Vector3 localPoint =
            transform.InverseTransformPoint(
                fingertip.position
            );

        // Measure how far the fingertip is from the plane.
        fingertipDepthFromPlane =
            Mathf.Abs(localPoint.z);

        // Fingertip must be close enough to the interaction plane.
        float depthLimit = touchingBoundary
            ? Mathf.Max(planeDepthTolerance, planeExitTolerance)
            : planeDepthTolerance;

        if (fingertipDepthFromPlane > depthLimit)
        {
            touchingBoundary = false;
            return;
        }

        // Check whether the fingertip is inside
        // the currently active shape.
        float margin = touchingBoundary ? Mathf.Max(0f, shapeExitMargin) : 0f;

        switch (activeShape)
        {
            case ShapeType.Square:

                touchingBoundary =
                    IsInsideSquare(localPoint, margin);

                break;


            case ShapeType.Circle:

                touchingBoundary =
                    IsInsideCircle(localPoint, margin);

                break;


            case ShapeType.Triangle:

                touchingBoundary =
                    IsInsideTriangle(localPoint, margin);

                break;


            default:

                touchingBoundary = false;

                break;
        }
    }


    // =========================================================
    // SQUARE DETECTION
    // =========================================================

    private bool IsInsideSquare(Vector3 point, float margin)
    {
        float halfWidth =
            squareWidth * 0.5f;

        float halfHeight =
            squareHeight * 0.5f;

        return
            Mathf.Abs(point.x) <= halfWidth + margin &&
            Mathf.Abs(point.y) <= halfHeight + margin;
    }


    // =========================================================
    // CIRCLE DETECTION
    // =========================================================

    private bool IsInsideCircle(Vector3 point, float margin)
    {
        float distanceFromCenter =
            new Vector2(
                point.x,
                point.y
            ).magnitude;

        return
            distanceFromCenter <= circleRadius + margin;
    }


    // =========================================================
    // TRIANGLE DETECTION
    // =========================================================

    private bool IsInsideTriangle(Vector3 point, float margin)
    {
        float halfWidth =
            triangleWidth * 0.5f;

        float halfHeight =
            triangleHeight * 0.5f;

        Vector2 top =
            new Vector2(
                0f,
                halfHeight
            );

        Vector2 bottomLeft =
            new Vector2(
                -halfWidth,
                -halfHeight
            );

        Vector2 bottomRight =
            new Vector2(
                halfWidth,
                -halfHeight
            );

        Vector2 fingertipPoint =
            new Vector2(
                point.x,
                point.y
            );

        if (PointInTriangle(fingertipPoint, top, bottomLeft, bottomRight))
        {
            return true;
        }

        // When already touching, permit a small outward edge margin.
        // This reduces chatter without enlarging the initial activation region.
        if (margin <= 0f)
        {
            return false;
        }

        float edgeDistance = Mathf.Min(
            DistanceToSegment(fingertipPoint, top, bottomLeft),
            Mathf.Min(
                DistanceToSegment(fingertipPoint, bottomLeft, bottomRight),
                DistanceToSegment(fingertipPoint, bottomRight, top)
            )
        );

        return edgeDistance <= margin;
    }


    // =========================================================
    // POINT-IN-TRIANGLE TEST
    // =========================================================

    private bool PointInTriangle(
        Vector2 point,
        Vector2 a,
        Vector2 b,
        Vector2 c
    )
    {
        float d1 =
            Sign(
                point,
                a,
                b
            );

        float d2 =
            Sign(
                point,
                b,
                c
            );

        float d3 =
            Sign(
                point,
                c,
                a
            );

        bool hasNegative =
            d1 < 0f ||
            d2 < 0f ||
            d3 < 0f;

        bool hasPositive =
            d1 > 0f ||
            d2 > 0f ||
            d3 > 0f;

        return !(hasNegative && hasPositive);
    }


    private float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.00000001f)
        {
            return Vector2.Distance(point, a);
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
        return Vector2.Distance(point, a + t * segment);
    }


    private float Sign(
        Vector2 p1,
        Vector2 p2,
        Vector2 p3
    )
    {
        return
            (p1.x - p3.x) *
            (p2.y - p3.y) -
            (p2.x - p3.x) *
            (p1.y - p3.y);
    }


    void OnDisable()
    {
        touchingBoundary = false;
    }


    // =========================================================
    // DEBUG VISUALIZATION
    // =========================================================

    void OnDrawGizmos()
    {
        if (!showDebugShape)
        {
            return;
        }

        Gizmos.matrix =
            transform.localToWorldMatrix;

        switch (activeShape)
        {
            case ShapeType.Square:

                DrawSquareGizmo();

                break;


            case ShapeType.Circle:

                DrawCircleGizmo();

                break;


            case ShapeType.Triangle:

                DrawTriangleGizmo();

                break;
        }
    }


    // =========================================================
    // SQUARE DEBUG
    // =========================================================

    private void DrawSquareGizmo()
    {
        Gizmos.DrawWireCube(
            Vector3.zero,
            new Vector3(
                squareWidth,
                squareHeight,
                0.001f
            )
        );
    }


    // =========================================================
    // CIRCLE DEBUG
    // =========================================================

    private void DrawCircleGizmo()
    {
        const int segments = 64;

        Vector3 previousPoint =
            new Vector3(
                circleRadius,
                0f,
                0f
            );

        for (int i = 1; i <= segments; i++)
        {
            float angle =
                ((float)i / segments) *
                Mathf.PI *
                2f;

            Vector3 currentPoint =
                new Vector3(
                    Mathf.Cos(angle) *
                    circleRadius,

                    Mathf.Sin(angle) *
                    circleRadius,

                    0f
                );

            Gizmos.DrawLine(
                previousPoint,
                currentPoint
            );

            previousPoint =
                currentPoint;
        }
    }


    // =========================================================
    // TRIANGLE DEBUG
    // =========================================================

    private void DrawTriangleGizmo()
    {
        float halfWidth =
            triangleWidth * 0.5f;

        float halfHeight =
            triangleHeight * 0.5f;

        Vector3 top =
            new Vector3(
                0f,
                halfHeight,
                0f
            );

        Vector3 bottomLeft =
            new Vector3(
                -halfWidth,
                -halfHeight,
                0f
            );

        Vector3 bottomRight =
            new Vector3(
                halfWidth,
                -halfHeight,
                0f
            );

        Gizmos.DrawLine(
            top,
            bottomLeft
        );

        Gizmos.DrawLine(
            bottomLeft,
            bottomRight
        );

        Gizmos.DrawLine(
            bottomRight,
            top
        );
    }


    // =========================================================
    // PUBLIC SHAPE CONTROLS
    // =========================================================

    public void SetSquare()
    {
        touchingBoundary = false;
        activeShape =
            ShapeType.Square;
    }


    public void SetCircle()
    {
        touchingBoundary = false;
        activeShape =
            ShapeType.Circle;
    }


    public void SetTriangle()
    {
        touchingBoundary = false;
        activeShape =
            ShapeType.Triangle;
    }


    public string GetActiveShape()
    {
        return activeShape.ToString();
    }


    // =========================================================
    // HAPTIC STATUS
    // =========================================================

    // Kept for compatibility with Shape2DHapticController.
    // Although the old name says "Boundary", this now returns
    // true anywhere inside the filled active shape.
    public bool IsTouchingBoundary()
    {
        return touchingBoundary;
    }
}