using System;
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

    private int _cellCount;
    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;

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

        CreateLand();
        SetTerrainType();

        for (var i = 0; i < _cellCount; i++)
        {
            Grid.GetCell(i).SearchPhase = 0;
        }

        Random.state = originalRandomState;
    }

    private void CreateLand()
    {
        var landBudget = Mathf.RoundToInt(_cellCount * LandPercentage * 0.01f);
        while (landBudget > 0)
        {
            var chunkSize = Random.Range(ChunkSizeMin, ChunkSizeMax - 1);
            if (Random.value < SinkProbability)
            {
                landBudget = SinkTerrain(chunkSize, landBudget);
            }
            else
            {
                landBudget = RaiseTerrain(chunkSize, landBudget);
            }
        }
    }

    private int RaiseTerrain(int chunkSize, int budget)
    {
        _searchFrontierPhase += 1;
        var firstCell = GetRandomCell();
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

    private int SinkTerrain(int chunkSize, int budget)
    {
        _searchFrontierPhase += 1;
        var firstCell = GetRandomCell();
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

    private void SetTerrainType()
    {
        for (int i = 0; i < _cellCount; i++)
        {
            var cell = Grid.GetCell(i);
            if (!cell.IsUnderwater)
            {
                cell.TerrainTypeIndex = cell.Elevation - cell.WaterLevel;
            }
        }
    }

    private HexCell GetRandomCell()
    {
        return Grid.GetCell(Random.Range(0, _cellCount));
    }
}