using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    private Mesh _hexMesh;
    private List<Vector3> _vertices;
    private List<int> _triangles;
    private List<Color> _colors;
    private MeshCollider _meshCollider;

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
        _meshCollider = gameObject.AddComponent<MeshCollider>();
        _hexMesh.name = "Hex Mesh";
        _vertices = new List<Vector3>();
        _triangles = new List<int>();
        _colors = new List<Color>();
    }

    public void Triangulate(HexCell[] cells)
    {
        _hexMesh.Clear();
        _vertices.Clear();
        _triangles.Clear();
        _colors.Clear();

        for (int i = 0; i < cells.Length; i++)
        {
            Triangulate(cells[i]);
        }

        _hexMesh.vertices = _vertices.ToArray();
        _hexMesh.triangles = _triangles.ToArray();
        _hexMesh.colors = _colors.ToArray();
        _hexMesh.RecalculateNormals();
        _meshCollider.sharedMesh = _hexMesh;
    }

    private void Triangulate(HexCell cell)
    {
        var center = cell.transform.localPosition;
        for (int i = 0; i < HexMetrics.Corners.Length - 1; i++)
        {
            AddTriangle(
                center,
                center + HexMetrics.Corners[i],
                center + HexMetrics.Corners[i + 1]
            );
            AddTriangleColor(cell.Color);
        }
    }

    private void AddTriangleColor(Color color)
    {
        _colors.AddRange(
            new[]
            {
                color,
                color,
                color
            }
        );
    }

    private void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var vertexIndex = _vertices.Count;
        _vertices.AddRange(
            new[]
            {
                v1,
                v2,
                v3
            }
        );
        _triangles.AddRange(
            new[]
            {
                vertexIndex,
                vertexIndex + 1,
                vertexIndex + 2
            }
        );
    }
}