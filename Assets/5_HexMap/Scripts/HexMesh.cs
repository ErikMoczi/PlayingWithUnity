using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HexMesh : MonoBehaviour
{
    public bool UseCollider, UseCellData, UseUVCoordinates, UseUV2Coordinates;

    private Mesh _hexMesh;
    [NonSerialized] private List<Vector3> _vertices, _cellIndices;
    [NonSerialized] private List<int> _triangles;
    [NonSerialized] private List<Color> _cellWeights;
    [NonSerialized] private List<Vector2> _UVs, _UV2s;
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
        _vertices = ListPool<Vector3>.Get();
        if (UseCellData)
        {
            _cellWeights = ListPool<Color>.Get();
            _cellIndices = ListPool<Vector3>.Get();
        }

        if (UseUVCoordinates)
        {
            _UVs = ListPool<Vector2>.Get();
        }

        if (UseUV2Coordinates)
        {
            _UV2s = ListPool<Vector2>.Get();
        }

        _triangles = ListPool<int>.Get();
    }

    public void Apply()
    {
        _hexMesh.SetVertices(_vertices);
        ListPool<Vector3>.Add(_vertices);
        if (UseCellData)
        {
            _hexMesh.SetColors(_cellWeights);
            ListPool<Color>.Add(_cellWeights);
            _hexMesh.SetUVs(2, _cellIndices);
            ListPool<Vector3>.Add(_cellIndices);
        }

        if (UseUVCoordinates)
        {
            _hexMesh.SetUVs(0, _UVs);
            ListPool<Vector2>.Add(_UVs);
        }

        if (UseUV2Coordinates)
        {
            _hexMesh.SetUVs(1, _UV2s);
            ListPool<Vector2>.Add(_UV2s);
        }

        _hexMesh.SetTriangles(_triangles, 0);
        ListPool<int>.Add(_triangles);
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
        var vertexIndex = _vertices.Count;
        _vertices.AddRange(new[] {HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), HexMetrics.Perturb(v3)});
        _triangles.AddRange(new[] {vertexIndex, vertexIndex + 1, vertexIndex + 2});
    }

    public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        var vertexIndex = _vertices.Count;
        _vertices.AddRange(new[] {v1, v2, v3});
        _triangles.AddRange(new[] {vertexIndex, vertexIndex + 1, vertexIndex + 2});
    }

    public void AddTriangleCellData(Vector3 indices, Color weights1, Color weights2, Color weights3)
    {
        _cellIndices.AddRange(new[] {indices, indices, indices});
        _cellWeights.AddRange(new[] {weights1, weights2, weights3});
    }

    public void AddTriangleCellData(Vector3 indices, Color weights)
    {
        AddTriangleCellData(indices, weights, weights, weights);
    }

    public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        _UVs.Add(uv1);
        _UVs.Add(uv2);
        _UVs.Add(uv3);
//        _UVs.AddRange(new[] {uv1, uv2, uv3});
    }

    public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
    {
        _UV2s.Add(uv1);
        _UV2s.Add(uv2);
        _UV2s.Add(uv3);
//        _UV2s.AddRange(new[] {uv1, uv2, uv3});
    }

    #endregion

    #region Quad

    public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var vertexIndex = _vertices.Count;
        _vertices.AddRange(new[]
            {HexMetrics.Perturb(v1), HexMetrics.Perturb(v2), HexMetrics.Perturb(v3), HexMetrics.Perturb(v4)});
        _triangles.AddRange(new[]
            {vertexIndex, vertexIndex + 2, vertexIndex + 1, vertexIndex + 1, vertexIndex + 2, vertexIndex + 3});
    }

    public void AddQuadUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        var vertexIndex = _vertices.Count;
        _vertices.AddRange(new[] {v1, v2, v3, v4});
        _triangles.AddRange(new[]
            {vertexIndex, vertexIndex + 2, vertexIndex + 1, vertexIndex + 1, vertexIndex + 2, vertexIndex + 3});
    }

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2, Color weights3, Color weights4)
    {
        _cellIndices.AddRange(new[] {indices, indices, indices, indices});
        _cellWeights.AddRange(new[] {weights1, weights2, weights3, weights4});
    }

    public void AddQuadCellData(Vector3 indices, Color weights1, Color weights2)
    {
        AddQuadCellData(indices, weights1, weights1, weights2, weights2);
    }

    public void AddQuadCellData(Vector3 indices, Color weights)
    {
        AddQuadCellData(indices, weights, weights, weights, weights);
    }

    public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        _UVs.Add(uv1);
        _UVs.Add(uv2);
        _UVs.Add(uv3);
        _UVs.Add(uv4);
//        _UVs.AddRange(new[] {uv1, uv2,uv3, uv4});
    }

    public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
    {
        _UV2s.Add(uv1);
        _UV2s.Add(uv2);
        _UV2s.Add(uv3);
        _UV2s.Add(uv4);
//        _UV2s.AddRange(new[] {uv1, uv2,uv3, uv4});
    }

    public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
    {
        _UVs.AddRange(new[]
        {
            new Vector2(uMin, vMin),
            new Vector2(uMax, vMin),
            new Vector2(uMin, vMax),
            new Vector2(uMax, vMax)
        });
    }

    public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
    {
        _UV2s.AddRange(new[]
        {
            new Vector2(uMin, vMin),
            new Vector2(uMax, vMin),
            new Vector2(uMin, vMax),
            new Vector2(uMax, vMax)
        });
    }

    public void AddQuadTerrainTypes(Vector3 types)
    {
        _cellIndices.Add(types);
        _cellIndices.Add(types);
        _cellIndices.Add(types);
        _cellIndices.Add(types);
    }

    #endregion

    #endregion
}