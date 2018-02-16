using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    public bool UseCollider, UseColors, UseUVCoordinates, UseUV2Coordinates, UseTerrainTypes;

    private Mesh _hexMesh;
    [NonSerialized] private List<Vector3> Vertices, _terrainTypes;
    [NonSerialized] private List<int> Triangles;
    [NonSerialized] private List<Color> Colors;
    [NonSerialized] private List<Vector2> UVs, UV2s;
    private MeshCollider _meshCollider;

    #region Unity

    private void Awake()
    {
        GetComponent<MeshFilter>().mesh = _hexMesh = new Mesh();
        if (UseCollider)
        {
            _meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        _hexMesh.name = "Hex Mesh";
    }

    #endregion

    public void Clear()
    {
        _hexMesh.Clear();
        Vertices = ListPool<Vector3>.Get();
        if (UseTerrainTypes)
        {
            _terrainTypes = ListPool<Vector3>.Get();
        }

        Triangles = ListPool<int>.Get();
        if (UseColors)
        {
            Colors = ListPool<Color>.Get();
        }

        if (UseUVCoordinates)
        {
            UVs = ListPool<Vector2>.Get();
        }

        if (UseUV2Coordinates)
        {
            UV2s = ListPool<Vector2>.Get();
        }
    }

    public void Apply()
    {
        _hexMesh.SetVertices(Vertices);
        ListPool<Vector3>.Add(Vertices);
        if (UseColors)
        {
            _hexMesh.SetColors(Colors);
            ListPool<Color>.Add(Colors);
        }

        if (UseUVCoordinates)
        {
            _hexMesh.SetUVs(0, UVs);
            ListPool<Vector2>.Add(UVs);
        }

        if (UseUV2Coordinates)
        {
            _hexMesh.SetUVs(1, UV2s);
            ListPool<Vector2>.Add(UV2s);
        }

        if (UseTerrainTypes)
        {
            _hexMesh.SetUVs(2, _terrainTypes);
            ListPool<Vector3>.Add(_terrainTypes);
        }

        _hexMesh.SetTriangles(Triangles, 0);
        ListPool<int>.Add(Triangles);
        _hexMesh.RecalculateNormals();
        if (UseCollider)
        {
            _meshCollider.sharedMesh = _hexMesh;
        }
    }

    #region MeshConstruct

    #region Triangle

    public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var vertexIndex = Vertices.Count;
        Vertices.AddRange(new[] {HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), HexMetrics.Perturb(v3)});
        Triangles.AddRange(new[] {vertexIndex, vertexIndex + 1, vertexIndex + 2});
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var vertexIndex = Vertices.Count;
        Vertices.AddRange(new[] {v1, v2, v3});
        Triangles.AddRange(new[] {vertexIndex, vertexIndex + 1, vertexIndex + 2});
    }

    public void AddTriangleColor(Color color)
    {
        Colors.AddRange(new[] {color, color, color});
    }

    public void AddTriangleColor(Color c1, Color c2, Color c3)
    {
        Colors.AddRange(new[] {c1, c2, c3});
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        UVs.AddRange(new[] {uv1, uv2, uv3});
    }

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3)
    {
        UV2s.AddRange(new[] {uv1, uv2, uv3});
    }

    public void AddTriangleTerrainTypes(Vector3 types)
    {
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
    }

    #endregion

    #region Quad

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var vertexIndex = Vertices.Count;
        Vertices.AddRange(new[]
            {HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), HexMetrics.Perturb(v3), HexMetrics.Perturb(v4)});
        Triangles.AddRange(new[]
            {vertexIndex, vertexIndex + 2, vertexIndex + 1, vertexIndex + 1, vertexIndex + 2, vertexIndex + 3});
    }

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var vertexIndex = Vertices.Count;
        Vertices.AddRange(new[] {v1, v2, v3, v4});
        Triangles.AddRange(new[]
            {vertexIndex, vertexIndex + 2, vertexIndex + 1, vertexIndex + 1, vertexIndex + 2, vertexIndex + 3});
    }

    public void AddQuadColor(Color c1, Color c2, Color c3, Color c4)
    {
        Colors.AddRange(new[] {c1, c2, c3, c4});
    }

    public void AddQuadColor(Color c1, Color c2)
    {
        Colors.AddRange(new[] {c1, c1, c2, c2});
    }

    public void AddQuadColor(Color color)
    {
        Colors.AddRange(new[] {color, color, color, color});
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        UVs.AddRange(new[] {uv1, uv2, uv3, uv4});
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector2 uv3, Vector2 uv4)
    {
        UV2s.AddRange(new[] {uv1, uv2, uv3, uv4});
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        UVs.AddRange(new[]
        {
            new Vector2(uMin, vMin),
            new Vector2(uMax, vMin),
            new Vector2(uMin, vMax),
            new Vector2(uMax, vMax)
        });
    }

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        UV2s.AddRange(new[]
        {
            new Vector2(uMin, vMin),
            new Vector2(uMax, vMin),
            new Vector2(uMin, vMax),
            new Vector2(uMax, vMax)
        });
    }

    public void AddQuadTerrainTypes(Vector3 types)
    {
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
        _terrainTypes.Add(types);
    }

    #endregion

    #endregion
}