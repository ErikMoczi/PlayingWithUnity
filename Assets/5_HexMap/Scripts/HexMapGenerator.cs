using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class HexMapGenerator : MonoBehaviour
{
    private static float[] _temperatureBands = {0.1f, 0.3f, 0.6f};
    private static float[] _moistureBands = {0.12f, 0.28f, 0.85f};

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
    [Range(0f, 1f)] public float StartingMoisture = 0.1f;
    [Range(0f, 1f)] public float EvaporationFactor = 0.5f;
    [Range(0f, 1f)] public float PrecipitationFactor = 0.25f;
    [Range(0f, 1f)] public float RunoffFactor = 0.25f;
    [Range(0f, 1f)] public float SeepageFactor = 0.125f;
    public HexDirection WindDirection = HexDirection.NW;
    [Range(1f, 10f)] public float WindStrength = 4f;
    [Range(0, 20)] public int RiverPercentage = 10;
    [Range(0f, 1f)] public float ExtraLakeProbability = 0.25f;
    [Range(0f, 1f)] public float LowTemperature = 0f;
    [Range(0f, 1f)] public float HighTemperature = 1f;

    public enum HemisphereMode
    {
        Both,
        North,
        South
    }

    public HemisphereMode Hemisphere;
    [Range(0f, 1f)] public float TemperatureJitter = 0.1f;

    private int _cellCount, _landCells;
    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;

    private struct MapRegion
    {
        public int XMin, XMax, ZMin, ZMax;
    }

    private List<MapRegion> _regions;

    private struct ClimateData
    {
        public float Clouds, Moisture;
    }

    private List<ClimateData> _climate = new List<ClimateData>();
    private List<ClimateData> _nextClimate = new List<ClimateData>();
    private List<HexDirection> _flowDirections = new List<HexDirection>();

    private int _temperatureJitterChannel;

    private struct Biome
    {
        public int Terrain, Plant;

        public Biome(int terrain, int plant)
        {
            Terrain = terrain;
            Plant = plant;
        }
    }

    private static Biome[] _biomes =
    {
        new Biome(0, 0), new Biome(4, 0), new Biome(4, 0), new Biome(4, 0),
        new Biome(0, 0), new Biome(2, 0), new Biome(2, 1), new Biome(2, 2),
        new Biome(0, 0), new Biome(1, 0), new Biome(1, 1), new Biome(1, 2),
        new Biome(0, 0), new Biome(1, 1), new Biome(1, 2), new Biome(1, 3)
    };

    public void GenerateMap(int x, int z, bool wrapping)
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
        Grid.CreateMap(x, z, wrapping);
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
        CreateClimate();
        CreateRivers();
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

        var borderX = Grid.Wrapping ? RegionBorder : MapBorderX;
        MapRegion region;
        switch (RegionCount)
        {
            default:
            {
                if (Grid.Wrapping)
                {
                    borderX = 0;
                }

                region.XMin = borderX;
                region.XMax = Grid.CellCountX - borderX;
                region.ZMin = MapBorderZ;
                region.ZMax = Grid.CellCountZ - MapBorderZ;
                _regions.Add(region);
                break;
            }
            case 2:
            {
                if (Random.value < 0.5f)
                {
                    region.XMin = borderX;
                    region.XMax = Grid.CellCountX / 2 - RegionBorder;
                    region.ZMin = MapBorderZ;
                    region.ZMax = Grid.CellCountZ - MapBorderZ;
                    _regions.Add(region);
                    region.XMin = Grid.CellCountX / 2 + RegionBorder;
                    region.XMax = Grid.CellCountX - borderX;
                    _regions.Add(region);
                }
                else
                {
                    if (Grid.Wrapping)
                    {
                        borderX = 0;
                    }

                    region.XMin = borderX;
                    region.XMax = Grid.CellCountX - borderX;
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
                region.XMin = borderX;
                region.XMax = Grid.CellCountX / 3 - RegionBorder;
                region.ZMin = MapBorderZ;
                region.ZMax = Grid.CellCountZ - MapBorderZ;
                _regions.Add(region);
                region.XMin = Grid.CellCountX / 3 + RegionBorder;
                region.XMax = Grid.CellCountX * 2 / 3 - RegionBorder;
                _regions.Add(region);
                region.XMin = Grid.CellCountX * 2 / 3 + RegionBorder;
                region.XMax = Grid.CellCountX - borderX;
                _regions.Add(region);
                break;
            }
            case 4:
            {
                region.XMin = borderX;
                region.XMax = Grid.CellCountX / 2 - RegionBorder;
                region.ZMin = MapBorderZ;
                region.ZMax = Grid.CellCountZ / 2 - RegionBorder;
                _regions.Add(region);
                region.XMin = Grid.CellCountX / 2 + RegionBorder;
                region.XMax = Grid.CellCountX - borderX;
                _regions.Add(region);
                region.ZMin = Grid.CellCountZ / 2 + RegionBorder;
                region.ZMax = Grid.CellCountZ - MapBorderZ;
                _regions.Add(region);
                region.XMin = borderX;
                region.XMax = Grid.CellCountX / 2 - RegionBorder;
                _regions.Add(region);
                break;
            }
        }
    }

    private void CreateLand()
    {
        var landBudget = Mathf.RoundToInt(_cellCount * LandPercentage * 0.01f);
        _landCells = landBudget;
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
            _landCells -= landBudget;
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

    private void CreateClimate()
    {
        _climate.Clear();
        _nextClimate.Clear();
        var initialData = new ClimateData();
        initialData.Moisture = StartingMoisture;
        var clearData = new ClimateData();
        for (var i = 0; i < _cellCount; i++)
        {
            _climate.Add(initialData);
            _nextClimate.Add(clearData);
        }

        for (var cycle = 0; cycle < 40; cycle++)
        {
            for (var i = 0; i < _cellCount; i++)
            {
                EvolveClimate(i);
            }

            var swap = _climate;
            _climate = _nextClimate;
            _nextClimate = swap;
        }
    }

    private void EvolveClimate(int cellIndex)
    {
        var cell = Grid.GetCell(cellIndex);
        var cellClimate = _climate[cellIndex];

        if (cell.IsUnderwater)
        {
            cellClimate.Moisture = 1f;
            cellClimate.Clouds += EvaporationFactor;
        }
        else
        {
            var evaporation = cellClimate.Moisture * EvaporationFactor;
            cellClimate.Moisture -= evaporation;
            cellClimate.Clouds += evaporation;
        }

        var precipitation = cellClimate.Clouds * PrecipitationFactor;
        cellClimate.Clouds -= precipitation;
        cellClimate.Moisture += precipitation;

        var cloudMaximum = 1f - cell.ViewElevation / (ElevationMaximum + 1f);
        if (cellClimate.Clouds > cloudMaximum)
        {
            cellClimate.Moisture += cellClimate.Clouds - cloudMaximum;
            cellClimate.Clouds = cloudMaximum;
        }

        var mainDispersalDirection = WindDirection.Opposite();
        var cloudDispersal = cellClimate.Clouds * (1f / (5f + WindStrength));
        var runoff = cellClimate.Moisture * RunoffFactor * (1f / 6f);
        var seepage = cellClimate.Moisture * SeepageFactor * (1f / 6f);
        for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            var neighbor = cell.GetNeighbor(d);
            if (!neighbor)
            {
                continue;
            }

            var neighborClimate = _nextClimate[neighbor.Index];
            if (d == mainDispersalDirection)
            {
                neighborClimate.Clouds += cloudDispersal * WindStrength;
            }
            else
            {
                neighborClimate.Clouds += cloudDispersal;
            }

            var elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
            if (elevationDelta < 0)
            {
                cellClimate.Moisture -= runoff;
                neighborClimate.Moisture += runoff;
            }
            else if (elevationDelta == 0)
            {
                cellClimate.Moisture -= seepage;
                neighborClimate.Moisture += seepage;
            }

            _nextClimate[neighbor.Index] = neighborClimate;
        }

        var nextCellClimate = _nextClimate[cellIndex];
        nextCellClimate.Moisture += cellClimate.Moisture;
        if (nextCellClimate.Moisture > 1f)
        {
            nextCellClimate.Moisture = 1f;
        }

        _nextClimate[cellIndex] = nextCellClimate;
        _climate[cellIndex] = new ClimateData();
    }

    private void CreateRivers()
    {
        var riverOrigins = ListPool<HexCell>.Get();
        for (var i = 0; i < _cellCount; i++)
        {
            var cell = Grid.GetCell(i);
            if (cell.IsUnderwater)
            {
                continue;
            }

            var data = _climate[i];
            var weight = data.Moisture * (cell.Elevation - WaterLevel) / (ElevationMaximum - WaterLevel);
            if (weight > 0.75f)
            {
                riverOrigins.Add(cell);
                riverOrigins.Add(cell);
            }

            if (weight > 0.5f)
            {
                riverOrigins.Add(cell);
            }

            if (weight > 0.25f)
            {
                riverOrigins.Add(cell);
            }
        }

        var riverBudget = Mathf.RoundToInt(_landCells * RiverPercentage * 0.01f);
        while (riverBudget > 0 && riverOrigins.Count > 0)
        {
            var index = Random.Range(0, riverOrigins.Count);
            var lastIndex = riverOrigins.Count - 1;
            var origin = riverOrigins[index];
            riverOrigins[index] = riverOrigins[lastIndex];
            riverOrigins.RemoveAt(lastIndex);

            if (!origin.HasRiver)
            {
                var isValidOrigin = true;
                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = origin.GetNeighbor(d);
                    if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater))
                    {
                        isValidOrigin = false;
                        break;
                    }
                }

                if (isValidOrigin)
                {
                    riverBudget -= CreateRiver(origin);
                }
            }
        }

        if (riverBudget > 0)
        {
            Debug.LogWarning("Failed to use up river budget.");
        }

        ListPool<HexCell>.Add(riverOrigins);
    }

    private int CreateRiver(HexCell origin)
    {
        var length = 1;
        var cell = origin;
        var direction = HexDirection.NE;
        while (!cell.IsUnderwater)
        {
            var minNeighborElevation = int.MaxValue;
            _flowDirections.Clear();
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = cell.GetNeighbor(d);
                if (!neighbor)
                {
                    continue;
                }

                if (neighbor.Elevation < minNeighborElevation)
                {
                    minNeighborElevation = neighbor.Elevation;
                }

                if (neighbor == origin || neighbor.HasIncomingRiver)
                {
                    continue;
                }

                var delta = neighbor.Elevation - cell.Elevation;
                if (delta > 0)
                {
                    continue;
                }

                if (neighbor.HasOutgoingRiver)
                {
                    cell.SetOutgoingRiver(d);
                    return length;
                }

                if (delta < 0)
                {
                    _flowDirections.Add(d);
                    _flowDirections.Add(d);
                    _flowDirections.Add(d);
                }

                if (
                    length == 1 ||
                    (d != direction.Next2() && d != direction.Previous2())
                )
                {
                    _flowDirections.Add(d);
                }

                _flowDirections.Add(d);
            }

            if (_flowDirections.Count == 0)
            {
                if (length == 1)
                {
                    return 0;
                }

                if (minNeighborElevation >= cell.Elevation)
                {
                    cell.WaterLevel = minNeighborElevation;
                    if (minNeighborElevation == cell.Elevation)
                    {
                        cell.Elevation = minNeighborElevation - 1;
                    }
                }

                break;
            }

            direction = _flowDirections[Random.Range(0, _flowDirections.Count)];
            cell.SetOutgoingRiver(direction);
            length += 1;

            if (minNeighborElevation >= cell.Elevation && Random.value < ExtraLakeProbability)
            {
                cell.WaterLevel = cell.Elevation;
                cell.Elevation -= 1;
            }

            cell = cell.GetNeighbor(direction);
        }

        return length;
    }

    private void SetTerrainType()
    {
        _temperatureJitterChannel = Random.Range(0, 4);
        var rockDesertElevation = ElevationMaximum - (ElevationMaximum - WaterLevel) / 2;
        for (var i = 0; i < _cellCount; i++)
        {
            var cell = Grid.GetCell(i);
            var temperature = DetermineTemperature(cell);
            var moisture = _climate[i].Moisture;
            if (!cell.IsUnderwater)
            {
                var t = 0;
                for (; t < _temperatureBands.Length; t++)
                {
                    if (temperature < _temperatureBands[t])
                    {
                        break;
                    }
                }

                var m = 0;
                for (; m < _moistureBands.Length; m++)
                {
                    if (moisture < _moistureBands[m])
                    {
                        break;
                    }
                }

                var cellBiome = _biomes[t * 4 + m];

                if (cellBiome.Terrain == 0)
                {
                    if (cell.Elevation >= rockDesertElevation)
                    {
                        cellBiome.Terrain = 3;
                    }
                }
                else if (cell.Elevation == ElevationMaximum)
                {
                    cellBiome.Terrain = 4;
                }

                if (cellBiome.Terrain == 4)
                {
                    cellBiome.Plant = 0;
                }
                else if (cellBiome.Plant < 3 && cell.HasRiver)
                {
                    cellBiome.Plant += 1;
                }

                cell.TerrainTypeIndex = cellBiome.Terrain;
                cell.PlantLevel = cellBiome.Plant;
            }
            else
            {
                int terrain;
                if (cell.Elevation == WaterLevel - 1)
                {
                    int cliffs = 0, slopes = 0;
                    for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                    {
                        var neighbor = cell.GetNeighbor(d);
                        if (!neighbor)
                        {
                            continue;
                        }

                        var delta = neighbor.Elevation - cell.WaterLevel;
                        if (delta == 0)
                        {
                            slopes += 1;
                        }
                        else if (delta > 0)
                        {
                            cliffs += 1;
                        }
                    }

                    if (cliffs + slopes > 3)
                    {
                        terrain = 1;
                    }
                    else if (cliffs > 0)
                    {
                        terrain = 3;
                    }
                    else if (slopes > 0)
                    {
                        terrain = 0;
                    }
                    else
                    {
                        terrain = 1;
                    }
                }
                else if (cell.Elevation >= WaterLevel)
                {
                    terrain = 1;
                }
                else if (cell.Elevation < 0)
                {
                    terrain = 3;
                }
                else
                {
                    terrain = 2;
                }

                if (terrain == 1 && temperature < _temperatureBands[0])
                {
                    terrain = 2;
                }

                cell.TerrainTypeIndex = terrain;
            }
        }
    }

    private float DetermineTemperature(HexCell cell)
    {
        var latitude = (float) cell.Coordinates.Z / Grid.CellCountZ;
        if (Hemisphere == HemisphereMode.Both)
        {
            latitude *= 2f;
            if (latitude > 1f)
            {
                latitude = 2f - latitude;
            }
        }
        else if (Hemisphere == HemisphereMode.North)
        {
            latitude = 1f - latitude;
        }

        var temperature = Mathf.LerpUnclamped(LowTemperature, HighTemperature, latitude);
        temperature *= 1f - (cell.ViewElevation - WaterLevel) / (ElevationMaximum - WaterLevel + 1f);
        var jitter = HexMetrics.SampleNoise(cell.Position * 0.1f)[_temperatureJitterChannel];
        temperature += (jitter * 2f - 1f) * TemperatureJitter;
        return temperature;
    }

    private HexCell GetRandomCell(MapRegion region)
    {
        return Grid.GetCell(Random.Range(region.XMin, region.XMax), Random.Range(region.ZMin, region.ZMax));
    }
}