using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] UrbanCollections, FarmCollections, PlantCollections;
    public HexMesh Walls;

    private Transform _container;

    public void Clear()
    {
        if (_container)
        {
            Destroy(_container.gameObject);
        }

        _container = new GameObject("Features Container").transform;
        _container.SetParent(transform, false);
        Walls.Clear();
    }

    public void Apply()
    {
        Walls.Apply();
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        var hash = HexMetrics.SampleHashGrid(position);
        var prefab = PickPrefab(UrbanCollections, cell.UrbanLevel, hash.A, hash.D);
        var otherPrefab = PickPrefab(FarmCollections, cell.FarmLevel, hash.B, hash.D);

        float usedHash = hash.A;
        if (prefab)
        {
            if (otherPrefab && hash.B < hash.A)
            {
                prefab = otherPrefab;
                usedHash = hash.B;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.B;
        }

        otherPrefab = PickPrefab(PlantCollections, cell.PlantLevel, hash.C, hash.D);
        if (prefab)
        {
            if (otherPrefab && hash.C < usedHash)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }

        var instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.E, 0f);
        instance.SetParent(_container, false);
    }

    public void AddWall(EdgeVertices near, HexCell nearCell, EdgeVertices far, HexCell farCell, bool hasRiver,
        bool hasRoad)
    {
        if (nearCell.Walled != farCell.Walled && !nearCell.IsUnderwater && !farCell.IsUnderwater &&
            nearCell.GetEdgeType(farCell) != HexEdgeType.Cliff)
        {
            AddWallSegment(near.V1, far.V1, near.V2, far.V2);
            if (hasRiver || hasRoad)
            {
                AddWallCap(near.V2, far.V2);
                AddWallCap(far.V4, near.V4);
            }
            else
            {
                AddWallSegment(near.V2, far.V2, near.V3, far.V3);
                AddWallSegment(near.V3, far.V3, near.V4, far.V4);
            }

            AddWallSegment(near.V4, far.V4, near.V5, far.V5);
        }
    }

    public void AddWall(Vector3 c1, HexCell cell1, Vector3 c2, HexCell cell2, Vector3 c3, HexCell cell3)
    {
        if (cell1.Walled)
        {
            if (cell2.Walled)
            {
                if (!cell3.Walled)
                {
                    AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
            else
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
        }
        else if (cell2.Walled)
        {
            if (cell3.Walled)
            {
                AddWallSegment(c1, cell1, c2, cell2, c3, cell3);
            }
            else
            {
                AddWallSegment(c2, cell2, c3, cell3, c1, cell1);
            }
        }
        else if (cell3.Walled)
        {
            AddWallSegment(c3, cell3, c1, cell1, c2, cell2);
        }
    }

    private void AddWallSegment(Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight)
    {
        nearLeft = HexMetrics.Perturb(nearLeft);
        farLeft = HexMetrics.Perturb(farLeft);
        nearRight = HexMetrics.Perturb(nearRight);
        farRight = HexMetrics.Perturb(farRight);

        var left = HexMetrics.WallLerp(nearLeft, farLeft);
        var right = HexMetrics.WallLerp(nearRight, farRight);

        var leftThicknessOffset = HexMetrics.WallThicknessOffset(nearLeft, farLeft);
        var rightThicknessOffset = HexMetrics.WallThicknessOffset(nearRight, farRight);

        var leftTop = left.y + HexMetrics.WallHeight;
        var rightTop = right.y + HexMetrics.WallHeight;

        Vector3 v1, v2, v3, v4;
        v1 = v3 = left - leftThicknessOffset;
        v2 = v4 = right - rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        Walls.AddQuadUnperturbed(v1, v2, v3, v4);

        Vector3 t1 = v3, t2 = v4;

        v1 = v3 = left + leftThicknessOffset;
        v2 = v4 = right + rightThicknessOffset;
        v3.y = leftTop;
        v4.y = rightTop;
        Walls.AddQuadUnperturbed(v2, v1, v4, v3);

        Walls.AddQuadUnperturbed(t1, t2, v3, v4);
    }

    private void AddWallSegment(Vector3 pivot, HexCell pivotCell, Vector3 left, HexCell leftCell, Vector3 right,
        HexCell rightCell)
    {
        if (pivotCell.IsUnderwater)
        {
            return;
        }

        var hasLeftWall = !leftCell.IsUnderwater && pivotCell.GetEdgeType(leftCell) != HexEdgeType.Cliff;
        var hasRighWall = !rightCell.IsUnderwater && pivotCell.GetEdgeType(rightCell) != HexEdgeType.Cliff;

        if (hasLeftWall)
        {
            if (hasRighWall)
            {
                AddWallSegment(pivot, left, pivot, right);
            }
            else if (leftCell.Elevation < rightCell.Elevation)
            {
                AddWallWedge(pivot, left, right);
            }
            else
            {
                AddWallCap(pivot, left);
            }
        }
        else if (hasRighWall)
        {
            if (rightCell.Elevation < leftCell.Elevation)
            {
                AddWallWedge(right, pivot, left);
            }
            else
            {
                AddWallCap(right, pivot);
            }
        }
    }

    private void AddWallCap(Vector3 near, Vector3 far)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);

        var center = HexMetrics.WallLerp(near, far);
        var thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = center.y + HexMetrics.WallHeight;
        Walls.AddQuadUnperturbed(v1, v2, v3, v4);
    }

    private void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
    {
        near = HexMetrics.Perturb(near);
        far = HexMetrics.Perturb(far);
        point = HexMetrics.Perturb(point);

        var center = HexMetrics.WallLerp(near, far);
        var thickness = HexMetrics.WallThicknessOffset(near, far);

        Vector3 v1, v2, v3, v4;
        var pointTop = point;
        point.y = center.y;

        v1 = v3 = center - thickness;
        v2 = v4 = center + thickness;
        v3.y = v4.y = pointTop.y = center.y + HexMetrics.WallHeight;

        Walls.AddQuadUnperturbed(v1, point, v3, pointTop);
        Walls.AddQuadUnperturbed(point, v2, pointTop, v4);
        Walls.AddTriangleUnperturbed(pointTop, v3, v4);
    }

    private Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
    {
        if (level > 0)
        {
            var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }

        return null;
    }
}