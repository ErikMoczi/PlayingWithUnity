using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int CellCountX = 20, CellCountZ = 15;
    public HexCell CellPrefab;
    public Text CellLabelPrefab;
    public HexGridChunk ChunkPrefab;
    public HexUnit UnitPrefab;
    public Texture2D NoiseSource;
    public int Seed;

    private int _chunkCountX, _chunkCountZ;
    private HexCell[] _cells;
    private HexGridChunk[] _chunks;

    #region Unity

    private void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        HexMetrics.InitializeHashGrid(Seed);
        HexUnit.UnitPrefab = UnitPrefab;

        CreateMap(CellCountX, CellCountZ);
    }

    private void OnEnable()
    {
        if (!HexMetrics.NoiseSource)
        {
            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitializeHashGrid(Seed);
            HexUnit.UnitPrefab = UnitPrefab;
        }
    }

    #endregion

    #region Chunk

    public bool CreateMap(int x, int z)
    {
        if (
            x <= 0 || x % HexMetrics.ChunkSizeX != 0 ||
            z <= 0 || z % HexMetrics.ChunkSizeZ != 0
        )
        {
            Debug.LogError("Unsupported map size.");
            return false;
        }

        ClearPath();
        ClearUnits();
        if (_chunks != null)
        {
            for (int i = 0; i < _chunks.Length; i++)
            {
                Destroy(_chunks[i].gameObject);
            }
        }

        CellCountX = x;
        CellCountZ = z;
        _chunkCountX = CellCountX / HexMetrics.ChunkSizeX;
        _chunkCountZ = CellCountZ / HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();

        return true;
    }

    private void CreateChunks()
    {
        _chunks = new HexGridChunk[_chunkCountX * _chunkCountZ];
        for (int z = 0, i = 0; z < _chunkCountZ; z++)
        {
            for (int x = 0; x < _chunkCountX; x++)
            {
                var chunk = _chunks[i++] = Instantiate(ChunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        var chunkX = x / HexMetrics.ChunkSizeX;
        var chunkZ = z / HexMetrics.ChunkSizeZ;
        var chunk = _chunks[chunkX + chunkZ * _chunkCountX];

        var localX = x - chunkX * HexMetrics.ChunkSizeX;
        var localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
    }

    #endregion

    #region Cell

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        var coordinates = HexCoordinates.FromPosition(position);
        var index = coordinates.X + coordinates.Z * CellCountX + coordinates.Z / 2;
        return _cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;
        if (z < 0 || z >= CellCountZ)
        {
            return null;
        }

        var x = coordinates.X + z / 2;
        if (x < 0 || x >= CellCountX)
        {
            return null;
        }

        return _cells[x + z * CellCountX];
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            return GetCell(hit.point);
        }

        return null;
    }

    private void CreateCells()
    {
        _cells = new HexCell[CellCountZ * CellCountX];

        for (int z = 0, i = 0; z < CellCountZ; z++)
        {
            for (int x = 0; x < CellCountX; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.OuterRadius * 1.5f);

        var cell = _cells[i] = Instantiate<HexCell>(CellPrefab);
        var cellTransform = cell.transform;
        cellTransform.localPosition = position;
        cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }

        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, _cells[i - CellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - CellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - CellCountX]);
                if (x < CellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - CellCountX + 1]);
                }
            }
        }

        CreateCellLabel(position, cell);
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    #endregion

    #region UI

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].ShowUI(visible);
        }
    }

    private void CreateCellLabel(Vector3 position, HexCell cell)
    {
        var label = Instantiate<Text>(CellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cell.UIRect = label.rectTransform;
    }

    #endregion

    #region SaveLoad

    public void Save(BinaryWriter writer)
    {
        writer.Write(CellCountX);
        writer.Write(CellCountZ);

        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].Save(writer);
        }

        writer.Write(_units.Count);
        for (int i = 0; i < _units.Count; i++)
        {
            _units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();
        int x = 20, z = 15;
        if (header >= 1)
        {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if (x != CellCountX || z != CellCountZ)
        {
            if (!CreateMap(x, z))
            {
                return;
            }
        }

        for (int i = 0; i < _cells.Length; i++)
        {
            _cells[i].Load(reader);
        }

        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].Refresh();
        }

        if (header >= 2)
        {
            var unitCount = reader.ReadInt32();
            for (int i = 0; i < unitCount; i++)
            {
                HexUnit.Load(reader, this);
            }
        }
    }

    #endregion

    #region Distance

    #region Attributes

    private HexCellPriorityQueue _searchFrontier;
    private int _searchFrontierPhase;
    private HexCell _currentPathFrom, _currentPathTo;
    private bool _currentPathExists;

    public bool HasPath
    {
        get { return _currentPathExists; }
    }

    #endregion

    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {
        ClearPath();
        _currentPathFrom = fromCell;
        _currentPathTo = toCell;
        _currentPathExists = Search(fromCell, toCell, speed);
        ShowPath(speed);
    }

    private bool Search(HexCell fromCell, HexCell toCell, int speed)
    {
        _searchFrontierPhase += 2;
        if (_searchFrontier == null)
        {
            _searchFrontier = new HexCellPriorityQueue();
        }
        else
        {
            _searchFrontier.Clear();
        }

        fromCell.SearchPhase = _searchFrontierPhase;
        fromCell.Distance = 0;
        _searchFrontier.Enqueue(fromCell);
        while (_searchFrontier.Count > 0)
        {
            var current = _searchFrontier.Dequeue();
            current.SearchPhase += 1;
            if (current == toCell)
            {
                return true;
            }

            var currentTurn = (current.Distance - 1) / speed;

            for (var direction = HexDirection.NE; direction <= HexDirection.NW; direction++)
            {
                var neighbor = current.GetNeighbor(direction);
                if (neighbor == null || neighbor.SearchPhase > _searchFrontierPhase)
                {
                    continue;
                }

                if (neighbor.IsUnderwater || neighbor.Unit)
                {
                    continue;
                }

                var edgeType = current.GetEdgeType(neighbor);
                if (edgeType == HexEdgeType.Cliff)
                {
                    continue;
                }

                int moveCost;
                if (current.HasRoadThroughEdge(direction))
                {
                    moveCost = 1;
                }
                else if (current.Walled != neighbor.Walled)
                {
                    continue;
                }
                else
                {
                    moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }

                var distance = current.Distance + moveCost;
                var turn = (distance - 1) / speed;
                if (turn > currentTurn)
                {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < _searchFrontierPhase)
                {
                    neighbor.SearchPhase = _searchFrontierPhase;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                    _searchFrontier.Enqueue(neighbor);
                }
                else if (distance < neighbor.Distance)
                {
                    var oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    _searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return false;
    }

    private void ShowPath(int speed)
    {
        if (_currentPathExists)
        {
            var current = _currentPathTo;
            while (current != _currentPathFrom)
            {
                var turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }

        _currentPathFrom.EnableHighlight(Color.blue);
        _currentPathTo.EnableHighlight(Color.red);
    }

    public void ClearPath()
    {
        if (_currentPathExists)
        {
            var current = _currentPathTo;
            while (current != _currentPathFrom)
            {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }

            current.DisableHighlight();
            _currentPathExists = false;
        }
        else if (_currentPathFrom)
        {
            _currentPathFrom.DisableHighlight();
            _currentPathTo.DisableHighlight();
        }

        _currentPathFrom = _currentPathTo = null;
    }

    public List<HexCell> GetPath()
    {
        if (!_currentPathExists)
        {
            return null;
        }

        var path = ListPool<HexCell>.Get();
        for (var c = _currentPathTo; c != _currentPathFrom; c = c.PathFrom)
        {
            path.Add(c);
        }

        path.Add(_currentPathFrom);
        path.Reverse();
        return path;
    }

    #endregion

    #region Units

    #region Attributes

    private List<HexUnit> _units = new List<HexUnit>();

    #endregion

    private void ClearUnits()
    {
        for (int i = 0; i < _units.Count; i++)
        {
            _units[i].Die();
        }

        _units.Clear();
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        _units.Add(unit);
        unit.transform.SetParent(transform, false);
        unit.Location = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        _units.Remove(unit);
        unit.Die();
    }

    #endregion
}