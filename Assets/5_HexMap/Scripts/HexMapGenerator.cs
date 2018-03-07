using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HexMapGenerator : MonoBehaviour
{
    public HexGrid Grid;
    public int Seed;
    public bool UseFixedSeed;

    [Range(0f, 0.5f)] public float JitterProbability = 0.25f;
    [Range(20, 200)] public int ChunkSizeMin = 30;
    [Range(20, 200)] public int ChunkSizeMax = 100;
    [Range(0f, 1f)] public float HighRiseProbability = 0.25f;
    [Range(0f, 0.4f)] public float SinkProbability = 0.2f;
    [Range(5, 95)] public int LandPercentage = 50;
    [Range(1, 5)] public int WaterLevel = 3;
    [Range(-4, 0)] public int ElevationMinimum = -2;
    [Range(6, 10)] public int ElevationMaximum = 8;
    [Range(0, 10)] public int MapBorderX = 5;
    [Range(0, 10)] public int MapBorderZ = 5;
    [Range(0, 10)] public int RegionBorder = 5;
    [Range(1, 4)] public int RegionCount = 1;
    [Range(0, 100)] public int ErosionPercentage = 50;

    private int _cellCount;
    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;

    private struct MapRegion
    {
        public int XMin, XMax, ZMin, ZMax;
    }

    private List<MapRegion> _regions;

    public void GenerateMap(int x, int z)
    {
        var originalRandomState = Random.state;
        if (!UseFixedSeed)
        {
            Seed = Random.Range(0, int.MaxValue);
            Seed ^= (int) DateTime.Now.Ticks;
            Seed ^= (int) Time.unscaledTime;
            Seed &= int.MaxValue;
        }

        Random.InitState(Seed);

        _cellCount = x * z;
        Grid.CreateMap(x, z);
        if (_searchFrontier == null)
        {
            _searchFrontier = new HexCellPriorityQueue();
        }

        for (var i = 0; i < _cellCount; i++)
        {
            Grid.GetCell(i).WaterLevel = WaterLevel;
        }

        CreateRegions();
        CreateLand();
        ErodeLand();
        SetTerrainType();

        for (var i = 0; i < _cellCount; i++)
        {
            Grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
    }

    private void CreateRegions()
    {
        if (_regions == null)
        {
            _regions = new List<MapRegion>();
        }
        else
        {
            _regions.Clear();
        }

        MapRegion region;
        switch (RegionCount)
        {
            default:
            {
                region.XMin = MapBorderX;
                region.XMax = Grid.CellCountX - MapBorderX;
                region.ZMin = MapBorderZ;
                region.ZMax = Grid.CellCountZ - MapBorderZ;
                _regions.Add(region);
                break;
            }
            case 2:
            {
                if (Random.value < 0.5f)
                {
                    region.XMin = MapBorderX;
                    region.XMax = Grid.CellCountX / 2 - RegionBorder;
                    region.ZMin = MapBorderZ;
                    region.ZMax = Grid.CellCountZ - MapBorderZ;
                    _regions.Add(region);
                    region.XMin = Grid.CellCountX / 2 + RegionBorder;
                    region.XMax = Grid.CellCountX - MapBorderX;
                    _regions.Add(region);
                }
                else
                {
                    region.XMin = MapBorderX;
                    region.XMax = Grid.CellCountX - MapBorderX;
                    region.ZMin = MapBorderZ;
                    region.ZMax = Grid.CellCountZ / 2 - RegionBorder;
                    _regions.Add(region);
                    region.ZMin = Grid.CellCountZ / 2 + RegionBorder;
                    region.ZMax = Grid.CellCountZ - MapBorderZ;
                    _regions.Add(region);
                }

                break;
            }
            case 3:
            {
                region.XMin = MapBorderX;
                region.XMax = Grid.CellCountX / 3 - RegionBorder;
                region.ZMin = MapBorderZ;
                region.ZMax = Grid.CellCountZ - MapBorderZ;
                _regions.Add(region);
                region.XMin = Grid.CellCountX / 3 + RegionBorder;
                region.XMax = Grid.CellCountX * 2 / 3 - RegionBorder;
                _regions.Add(region);
                region.XMin = Grid.CellCountX * 2 / 3 + RegionBorder;
                region.XMax = Grid.CellCountX - MapBorderX;
                _regions.Add(region);
                break;
            }
            case 4:
            {
                region.XMin = MapBorderX;
                region.XMax = Grid.CellCountX / 2 - RegionBorder;
                region.ZMin = MapBorderZ;
                region.ZMax = Grid.CellCountZ / 2 - RegionBorder;
                _regions.Add(region);
                region.XMin = Grid.CellCountX / 2 + RegionBorder;
                region.XMax = Grid.CellCountX - MapBorderX;
                _regions.Add(region);
                region.ZMin = Grid.CellCountZ / 2 + RegionBorder;
                region.ZMax = Grid.CellCountZ - MapBorderZ;
                _regions.Add(region);
                region.XMin = MapBorderX;
                region.XMax = Grid.CellCountX / 2 - RegionBorder;
                _regions.Add(region);
                break;
            }
        }
    }

    private void CreateLand()
    {
        var landBudget = Mathf.RoundToInt(_cellCount * LandPercentage * 0.01f);
        for (var guard = 0; guard < 10000; guard++)
        {
            var sink = Random.value < SinkProbability;
            for (var i = 0; i < _regions.Count; i++)
            {
                var region = _regions[i];
                var chunkSize = Random.Range(ChunkSizeMin, ChunkSizeMax - 1);
                if (sink)
                {
                    landBudget = SinkTerrain(chunkSize, landBudget, region);
                }
                else
                {
                    landBudget = RaiseTerrain(chunkSize, landBudget, region);
                    if (landBudget == 0)
                    {
                        return;
                    }
                }
            }
        }

        if (landBudget > 0)
        {
            Debug.LogWarning("Failed to use up " + landBudget + " land budget.");
        }
    }

    private int RaiseTerrain(int chunkSize, int budget, MapRegion region)
    {
        _searchFrontierPhase += 1;
        var firstCell = GetRandomCell(region);
        firstCell.SearchPhase = _searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        _searchFrontier.Enqueue(firstCell);
        var center = firstCell.Coordinates;

        var rise = Random.value < HighRiseProbability ? 2 : 1;
        var size = 0;
        while (size < chunkSize && _searchFrontier.Count > 0)
        {
            var current = _searchFrontier.Dequeue();
            var originalElevation = current.Elevation;
            var newElevation = originalElevation + rise;
            if (newElevation > ElevationMaximum)
            {
                continue;
            }

            current.Elevation = newElevation;
            if (originalElevation < WaterLevel && newElevation >= WaterLevel && --budget == 0)
            {
                break;
            }

            size += 1;

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < _searchFrontierPhase)
                {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < JitterProbability ? 1 : 0;
                    _searchFrontier.Enqueue(neighbor);
                }
            }
        }

        _searchFrontier.Clear();

        return budget;
    }

    private int SinkTerrain(int chunkSize, int budget, MapRegion region)
    {
        _searchFrontierPhase += 1;
        var firstCell = GetRandomCell(region);
        firstCell.SearchPhase = _searchFrontierPhase;
        firstCell.Distance = 0;
        firstCell.SearchHeuristic = 0;
        _searchFrontier.Enqueue(firstCell);
        var center = firstCell.Coordinates;

        var sink = Random.value < HighRiseProbability ? 2 : 1;
        var size = 0;
        while (size < chunkSize && _searchFrontier.Count > 0)
        {
            var current = _searchFrontier.Dequeue();
            var originalElevation = current.Elevation;
            var newElevation = current.Elevation - sink;
            if (newElevation < ElevationMinimum)
            {
                continue;
            }

            current.Elevation = newElevation;
            if (originalElevation >= WaterLevel && newElevation < WaterLevel)
            {
                budget += 1;
            }

            size += 1;

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = current.GetNeighbor(d);
                if (neighbor && neighbor.SearchPhase < _searchFrontierPhase)
                {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = neighbor.Coordinates.DistanceTo(center);
                    neighbor.SearchHeuristic = Random.value < JitterProbability ? 1 : 0;
                    _searchFrontier.Enqueue(neighbor);
                }
            }
        }

        _searchFrontier.Clear();

        return budget;
    }

    private void ErodeLand()
    {
        var erodibleCells = ListPool<HexCell>.Get();
        for (var i = 0; i < _cellCount; i++)
        {
            var cell = Grid.GetCell(i);
            if (IsErodible(cell))
            {
                erodibleCells.Add(cell);
            }
        }

        var targetErodibleCount = (int) (erodibleCells.Count * (100 - ErosionPercentage) * 0.01f);

        while (erodibleCells.Count > targetErodibleCount)
        {
            var index = Random.Range(0, erodibleCells.Count);
            var cell = erodibleCells[index];
            var targetCell = GetErosionTarget(cell);

            cell.Elevation -= 1;
            targetCell.Elevation += 1;

            if (!IsErodible(cell))
            {
                erodibleCells[index] = erodibleCells[erodibleCells.Count - 1];
                erodibleCells.RemoveAt(erodibleCells.Count - 1);
            }

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = cell.GetNeighbor(d);
                if (neighbor && neighbor.Elevation == cell.Elevation + 2 && !erodibleCells.Contains(neighbor))
                {
                    erodibleCells.Add(neighbor);
                }
            }

            if (IsErodible(targetCell) && !erodibleCells.Contains(targetCell))
            {
                erodibleCells.Add(targetCell);
            }

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = targetCell.GetNeighbor(d);
                if (neighbor && neighbor != cell && neighbor.Elevation == targetCell.Elevation + 1 &&
                    !IsErodible(neighbor))
                {
                    erodibleCells.Remove(neighbor);
                }
            }
        }

        ListPool<HexCell>.Add(erodibleCells);
    }

    private void SetTerrainType()
    {
        for (var i = 0; i < _cellCount; i++)
        {
            var cell = Grid.GetCell(i);
            if (!cell.IsUnderwater)
            {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }

    private bool IsErodible(HexCell cell)
    {
        var erodibleElevation = cell.Elevation - 2;
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            var neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                return true;
            }
        }

        return false;
    }

    private HexCell GetErosionTarget(HexCell cell)
    {
        var candidates = ListPool<HexCell>.Get();
        var erodibleElevation = cell.Elevation - 2;
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            var neighbor = cell.GetNeighbor(d);
            if (neighbor && neighbor.Elevation <= erodibleElevation)
            {
                candidates.Add(neighbor);
            }
        }

        var target = candidates[Random.Range(0, candidates.Count)];
        ListPool<HexCell>.Add(candidates);
        return target;
    }

    private HexCell GetRandomCell(MapRegion region)
    {
        return Grid.GetCell(Random.Range(region.XMin, region.XMax), Random.Range(region.ZMin, region.ZMax));
    }
}