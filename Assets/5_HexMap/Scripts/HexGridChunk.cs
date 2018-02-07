using UnityEngine;
using UnityEngine.UI;

public class HexGridChunk : MonoBehaviour
{
    public HexMesh Terrain, Rivers;

    private HexCell[] _cells;
    private Canvas _gridCanvas;

    private void Awake()
    {
        _gridCanvas = GetComponentInChildren<Canvas>();
        Terrain = GetComponentInChildren<HexMesh>();

        _cells = new HexCell[HexMetrics.ChunkSizeX * HexMetrics.ChunkSizeZ];
        ShowUI(false);
    }

    private void LateUpdate()
    {
        Triangulate();
        enabled = false;
    }

    public void Refresh()
    {
        enabled = true;
    }

    public void AddCell(int index, HexCell cell)
    {
        _cells[index] = cell;
        cell.Chunk = this;
        cell.transform.SetParent(Terrain.transform, false);
        cell.UIRect.SetParent(_gridCanvas.transform, false);
    }

    public void ShowUI(bool visible)
    {
        _gridCanvas.gameObject.SetActive(visible);
    }

    public void Triangulate()
    {
        Terrain.Clear();
        Rivers.Clear();
        for (int i = 0; i < _cells.Length; i++)
        {
            Triangulate(_cells[i]);
        }

        Terrain.Apply();
        Rivers.Apply();
    }

    private void Triangulate(HexCell cell)
    {
        for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
        {
            Triangulate(direction, cell);
        }
    }

    private void Triangulate(HexDirection direction, HexCell cell)
    {
        var center = cell.Position;
        var e = new EdgeVertices(
            center + HexMetrics.GetFirstSolidCorner(direction),
            center + HexMetrics.GetSecondSolidCorner(direction)
        );

        if (cell.HasRiver)
        {
            if (cell.HasRiverThroughEdge(direction))
            {
                e.V3.y = cell.StreamBedY;
                if (cell.HasRiverBeginOrEnd)
                {
                    TriangulateWithRiverBeginOrEnd(direction, cell, center, e);
                }
                else
                {
                    TriangulateWithRiver(direction, cell, center, e);
                }
            }
            else
            {
                TriangulateAdjacentToRiver(direction, cell, center, e);
            }
        }
        else
        {
            TriangulateEdgeFan(center, e, cell.Color);
        }

        if (direction <= HexDirection.SE)
        {
            TriangulateConnection(direction, cell, e);
        }
    }

    private void TriangulateConnection(HexDirection direction, HexCell cell, EdgeVertices e1)
    {
        var neighbor = cell.GetNeighbor(direction);
        if (neighbor == null)
        {
            return;
        }

        var bridge = HexMetrics.GetBridge(direction);
        bridge.y = neighbor.Position.y - cell.Position.y;
        var e2 = new EdgeVertices(
            e1.V1 + bridge,
            e1.V5 + bridge
        );

        if (cell.HasRiverThroughEdge(direction))
        {
            e2.V3.y = neighbor.StreamBedY;
            TriangulateRiverQuad(e1.V2, e1.V4, e2.V2, e2.V4, cell.RiverSurfaceY, neighbor.RiverSurfaceY, 0.8f,
                cell.HasIncomingRiver && cell.IncomingRiver == direction);
        }

        if (cell.GetEdgeType(direction) == HexEdgeType.Slope)
        {
            TriangulateEdgeTerraces(e1, cell, e2, neighbor);
        }
        else
        {
            TriangulateEdgeStrip(e1, cell.Color, e2, neighbor.Color);
        }

        var nextNeighbor = cell.GetNeighbor(direction.Next());
        if (direction <= HexDirection.E && nextNeighbor != null)
        {
            var v5 = e1.V5 + HexMetrics.GetBridge(direction.Next());
            v5.y = nextNeighbor.Position.y;

            if (cell.Elevation <= neighbor.Elevation)
            {
                if (cell.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(e1.V5, cell, e2.V5, neighbor, v5, nextNeighbor);
                }
                else
                {
                    TriangulateCorner(v5, nextNeighbor, e1.V5, cell, e2.V5, neighbor);
                }
            }
            else if (neighbor.Elevation <= nextNeighbor.Elevation)
            {
                TriangulateCorner(e2.V5, neighbor, v5, nextNeighbor, e1.V5, cell);
            }
            else
            {
                TriangulateCorner(v5, nextNeighbor, e1.V5, cell, e2.V5, neighbor);
            }
        }
    }

    private void TriangulateCorner(Vector3 bottom, HexCell bottomCell, Vector3 left, HexCell leftCell, Vector3 right,
        HexCell rightCell)
    {
        var leftEdgeType = bottomCell.GetEdgeType(leftCell);
        var rightEdgeType = bottomCell.GetEdgeType(rightCell);

        if (leftEdgeType == HexEdgeType.Slope)
        {
            if (rightEdgeType == HexEdgeType.Slope)
            {
                TriangulateCornerTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
            else if (rightEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(left, leftCell, right, rightCell, bottom, bottomCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (rightEdgeType == HexEdgeType.Slope)
        {
            if (leftEdgeType == HexEdgeType.Flat)
            {
                TriangulateCornerTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerCliffTerraces(bottom, bottomCell, left, leftCell, right, rightCell);
            }
        }
        else if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            if (leftCell.Elevation < rightCell.Elevation)
            {
                TriangulateCornerCliffTerraces(right, rightCell, bottom, bottomCell, left, leftCell);
            }
            else
            {
                TriangulateCornerTerracesCliff(left, leftCell, right, rightCell, bottom, bottomCell);
            }
        }
        else
        {
            Terrain.AddTriangle(bottom, left, right);
            Terrain.AddTriangleColor(bottomCell.Color, leftCell.Color, rightCell.Color);
        }
    }

    private void TriangulateEdgeTerraces(EdgeVertices begin, HexCell beginCell, EdgeVertices end, HexCell endCell)
    {
        var e2 = EdgeVertices.TerraceLerp(begin, end, 1);
        var c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, 1);

        TriangulateEdgeStrip(begin, beginCell.Color, e2, c2);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            var e1 = e2;
            var c1 = c2;
            e2 = EdgeVertices.TerraceLerp(begin, end, i);
            c2 = HexMetrics.TerraceLerp(beginCell.Color, endCell.Color, i);
            TriangulateEdgeStrip(e1, c1, e2, c2);
        }

        TriangulateEdgeStrip(e2, c2, end, endCell.Color);
    }

    private void TriangulateCornerTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var v3 = HexMetrics.TerraceLerp(begin, left, 1);
        var v4 = HexMetrics.TerraceLerp(begin, right, 1);
        var c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);
        var c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, 1);

        Terrain.AddTriangle(begin, v3, v4);
        Terrain.AddTriangleColor(beginCell.Color, c3, c4);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            var v1 = v3;
            var v2 = v4;
            var c1 = c3;
            var c2 = c4;
            v3 = HexMetrics.TerraceLerp(begin, left, i);
            v4 = HexMetrics.TerraceLerp(begin, right, i);
            c3 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            c4 = HexMetrics.TerraceLerp(beginCell.Color, rightCell.Color, i);
            Terrain.AddQuad(v1, v2, v3, v4);
            Terrain.AddQuadColor(c1, c2, c3, c4);
        }

        Terrain.AddQuad(v3, v4, left, right);
        Terrain.AddQuadColor(c3, c4, leftCell.Color, rightCell.Color);
    }

    private void TriangulateCornerTerracesCliff(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var b = 1f / (rightCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }

        var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(right), b);
        var boundaryColor = Color.Lerp(beginCell.Color, rightCell.Color, b);

        TriangulateBoundaryTriangle(begin, beginCell, left, leftCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateCornerCliffTerraces(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 right, HexCell rightCell)
    {
        var b = 1f / (leftCell.Elevation - beginCell.Elevation);
        if (b < 0)
        {
            b = -b;
        }

        var boundary = Vector3.Lerp(HexMetrics.Perturb(begin), HexMetrics.Perturb(left), b);
        var boundaryColor = Color.Lerp(beginCell.Color, leftCell.Color, b);

        TriangulateBoundaryTriangle(right, rightCell, begin, beginCell, boundary, boundaryColor);

        if (leftCell.GetEdgeType(rightCell) == HexEdgeType.Slope)
        {
            TriangulateBoundaryTriangle(left, leftCell, right, rightCell, boundary, boundaryColor);
        }
        else
        {
            Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(left), HexMetrics.Perturb(right), boundary);
            Terrain.AddTriangleColor(leftCell.Color, rightCell.Color, boundaryColor);
        }
    }

    private void TriangulateBoundaryTriangle(Vector3 begin, HexCell beginCell, Vector3 left, HexCell leftCell,
        Vector3 boundary, Color boundaryColor)
    {
        var v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, 1));
        var c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, 1);

        Terrain.AddTriangleUnperturbed(HexMetrics.Perturb(begin), v2, boundary);
        Terrain.AddTriangleColor(beginCell.Color, c2, boundaryColor);

        for (int i = 2; i < HexMetrics.TerraceSteps; i++)
        {
            var v1 = v2;
            var c1 = c2;
            v2 = HexMetrics.Perturb(HexMetrics.TerraceLerp(begin, left, i));
            c2 = HexMetrics.TerraceLerp(beginCell.Color, leftCell.Color, i);
            Terrain.AddTriangleUnperturbed(v1, v2, boundary);
            Terrain.AddTriangleColor(c1, c2, boundaryColor);
        }

        Terrain.AddTriangleUnperturbed(v2, HexMetrics.Perturb(left), boundary);
        Terrain.AddTriangleColor(c2, leftCell.Color, boundaryColor);
    }

    private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, Color color)
    {
        Terrain.AddTriangle(center, edge.V1, edge.V2);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.V2, edge.V3);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.V3, edge.V4);
        Terrain.AddTriangleColor(color);
        Terrain.AddTriangle(center, edge.V4, edge.V5);
        Terrain.AddTriangleColor(color);
    }

    private void TriangulateEdgeStrip(EdgeVertices e1, Color c1, EdgeVertices e2, Color c2)
    {
        Terrain.AddQuad(e1.V1, e1.V2, e2.V1, e2.V2);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.V2, e1.V3, e2.V2, e2.V3);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.V3, e1.V4, e2.V3, e2.V4);
        Terrain.AddQuadColor(c1, c2);
        Terrain.AddQuad(e1.V4, e1.V5, e2.V4, e2.V5);
        Terrain.AddQuadColor(c1, c2);
    }

    private void TriangulateWithRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        Vector3 centerL, centerR;
        if (cell.HasRiverThroughEdge(direction.Opposite()))
        {
            centerL = center + HexMetrics.GetFirstSolidCorner(direction.Previous()) * 0.25f;
            centerR = center + HexMetrics.GetSecondSolidCorner(direction.Next()) * 0.25f;
        }
        else if (cell.HasRiverThroughEdge(direction.Next()))
        {
            centerL = center;
            centerR = Vector3.Lerp(center, e.V5, 2f / 3f);
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()))
        {
            centerL = Vector3.Lerp(center, e.V1, 2f / 3f);
            centerR = center;
        }
        else if (cell.HasRiverThroughEdge(direction.Next2()))
        {
            centerL = center;
            centerR = center + HexMetrics.GetSolidEdgeMiddle(direction.Next()) * (0.5f * HexMetrics.InnerToOuter);
        }
        else
        {
            centerL = center + HexMetrics.GetSolidEdgeMiddle(direction.Previous()) * (0.5f * HexMetrics.InnerToOuter);
            centerR = center;
        }

        center = Vector3.Lerp(centerL, centerR, 0.5f);

        var m = new EdgeVertices(
            Vector3.Lerp(centerL, e.V1, 0.5f),
            Vector3.Lerp(centerR, e.V5, 0.5f),
            1f / 6f
        );
        m.V3.y = center.y = e.V3.y;
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);

        Terrain.AddTriangle(centerL, m.V1, m.V2);
        Terrain.AddTriangleColor(cell.Color);
        Terrain.AddQuad(centerL, center, m.V2, m.V3);
        Terrain.AddQuadColor(cell.Color);
        Terrain.AddQuad(center, centerR, m.V3, m.V4);
        Terrain.AddQuadColor(cell.Color);
        Terrain.AddTriangle(centerR, m.V4, m.V5);
        Terrain.AddTriangleColor(cell.Color);

        var reversed = cell.IncomingRiver == direction;
        TriangulateRiverQuad(centerL, centerR, m.V2, m.V4, cell.RiverSurfaceY, 0.4f, reversed);
        TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);
    }

    private void TriangulateWithRiverBeginOrEnd(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        var m = new EdgeVertices(
            Vector3.Lerp(center, e.V1, 0.5f),
            Vector3.Lerp(center, e.V5, 0.5f)
        );
        m.V3.y = e.V3.y;
        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);

        var reversed = cell.HasIncomingRiver;
        TriangulateRiverQuad(m.V2, m.V4, e.V2, e.V4, cell.RiverSurfaceY, 0.6f, reversed);
        center.y = m.V2.y = m.V4.y = cell.RiverSurfaceY;
        Rivers.AddTriangle(center, m.V2, m.V4);
        if (reversed)
        {
            Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(1f, 0.2f), new Vector2(0f, 0.2f));
        }
        else
        {
            Rivers.AddTriangleUV(new Vector2(0.5f, 0.4f), new Vector2(0f, 0.6f), new Vector2(1f, 0.6f));
        }
    }

    private void TriangulateAdjacentToRiver(HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e)
    {
        if (cell.HasRiverThroughEdge(direction.Next()))
        {
            if (cell.HasRiverThroughEdge(direction.Previous()))
            {
                center += HexMetrics.GetSolidEdgeMiddle(direction) * (HexMetrics.InnerToOuter * 0.5f);
            }
            else if (cell.HasRiverThroughEdge(direction.Previous2()))
            {
                center += HexMetrics.GetFirstSolidCorner(direction) * 0.25f;
            }
        }
        else if (cell.HasRiverThroughEdge(direction.Previous()) && cell.HasRiverThroughEdge(direction.Next2()))
        {
            center += HexMetrics.GetSecondSolidCorner(direction) * 0.25f;
        }

        var m = new EdgeVertices(
            Vector3.Lerp(center, e.V1, 0.5f),
            Vector3.Lerp(center, e.V5, 0.5f)
        );

        TriangulateEdgeStrip(m, cell.Color, e, cell.Color);
        TriangulateEdgeFan(center, m, cell.Color);
    }

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y, float v, bool reversed)
    {
        TriangulateRiverQuad(v1, v2, v3, v4, y, y, v, reversed);
    }

    private void TriangulateRiverQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float y1, float y2, float v,
        bool reversed)
    {
        v1.y = v2.y = y1;
        v3.y = v4.y = y2;
        Rivers.AddQuad(v1, v2, v3, v4);
        if (reversed)
        {
            Rivers.AddQuadUV(1f, 0f, 0.8f - v, 0.6f - v);
        }
        else
        {
            Rivers.AddQuadUV(0f, 1f, v, v + 0.2f);
        }
    }
}