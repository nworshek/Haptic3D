using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TriangleRender : MonoBehaviour
{
    public float width = 0.15f;
    public float height = 0.15f;

    void Awake()
    {
        CreateTriangle();
    }

    void CreateTriangle()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (meshFilter == null)
            return;

        Mesh mesh = new Mesh();
        mesh.name = "Shape2D Triangle";

        float halfWidth = width / 2f;
        float halfHeight = height / 2f;

        // Separate vertices for front and back faces.
        mesh.vertices = new Vector3[]
        {
            // Front
            new Vector3(0f, halfHeight, 0f),
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),

            // Back
            new Vector3(0f, halfHeight, 0f),
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f)
        };

        mesh.triangles = new int[]
        {
            // Front
            0, 1, 2,

            // Back
            3, 5, 4
        };

        // Explicit normals
        mesh.normals = new Vector3[]
        {
            Vector3.back,
            Vector3.back,
            Vector3.back,

            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };

        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }
}