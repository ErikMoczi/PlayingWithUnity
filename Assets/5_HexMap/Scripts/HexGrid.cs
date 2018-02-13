using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int CellCountX = 20, CellCountZ = 15;
    public HexCell CellPrefab;
    public Text CellLabelPrefab;
    public HexGridChunk ChunkPrefab;
    public Texture2D NoiseSource;
    public int Seed;
    public Color[] Colors;

    private int _chunkCountX, _chunkCountZ;
    private HexCell[] _cells;
    private HexGridChunk[] _chunks;

    #region Unity

    private void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        HexMetrics.InitializeHashGrid(Seed);
        HexMetrics.Colors = Colors;

        CreateMap(CellCountX, CellCountZ);
    }

    private void OnEnable()
    {
        if (!HexMetrics.NoiseSource)
        {
            HexMetrics.NoiseSource = NoiseSource;
            HexMetrics.InitializeHashGrid(Seed);
            HexMetrics.Colors = Colors;
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
        label.text = cell.Coordinates.ToStringOnSeparateLines();
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
    }

    public void Load(BinaryReader reader, int header)
    {
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
    }

    #endregion
}