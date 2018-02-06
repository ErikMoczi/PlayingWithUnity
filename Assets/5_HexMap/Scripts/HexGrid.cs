using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int ChunkCountX = 4, ChunkCountZ = 3;
    public Color DefaultColor = Color.white;
    public HexCell CellPrefab;
    public Text CellLabelPrefab;
    public HexGridChunk ChunkPrefab;
    public Texture2D NoiseSource;

    private int _cellCountX;
    private int _cellCountZ;
    private HexCell[] _cells;
    private HexGridChunk[] _chunks;

    private void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;

        _cellCountX = ChunkCountX * HexMetrics.ChunkSizeX;
        _cellCountZ = ChunkCountZ * HexMetrics.ChunkSizeZ;

        CreateChunks();
        CreateCells();
    }

    private void OnEnable()
    {
        HexMetrics.NoiseSource = NoiseSource;
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < _chunks.Length; i++)
        {
            _chunks[i].ShowUI(visible);
        }
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        var coordinates = HexCoordinates.FromPosition(position);
        var index = coordinates.X + coordinates.Z * _cellCountX + coordinates.Z / 2;
        return _cells[index];
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;
        if (z < 0 || z >= _cellCountZ)
        {
            return null;
        }

        var x = coordinates.X + z / 2;
        if (x < 0 || x >= _cellCountX)
        {
            return null;
        }

        return _cells[x + z * _cellCountX];
    }

    private void CreateChunks()
    {
        _chunks = new HexGridChunk[ChunkCountX * ChunkCountZ];
        for (int z = 0, i = 0; z < ChunkCountZ; z++)
        {
            for (int x = 0; x < ChunkCountX; x++)
            {
                var chunk = _chunks[i++] = Instantiate(ChunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    private void CreateCells()
    {
        _cells = new HexCell[_cellCountZ * _cellCountX];

        for (int z = 0, i = 0; z < _cellCountZ; z++)
        {
            for (int x = 0; x < _cellCountX; x++)
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
        cell.Color = DefaultColor;

        if (x > 0)
        {
            cell.SetNeighbor(HexDirection.W, _cells[i - 1]);
        }

        if (z > 0)
        {
            if ((z & 1) == 0)
            {
                cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - _cellCountX]);
                if (x < _cellCountX - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - _cellCountX + 1]);
                }
            }
        }

        CreateCellLabel(position, cell);
        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        var chunkX = x / HexMetrics.ChunkSizeX;
        var chunkZ = z / HexMetrics.ChunkSizeZ;
        var chunk = _chunks[chunkX + chunkZ * ChunkCountX];

        var localX = x - chunkX * HexMetrics.ChunkSizeX;
        var localZ = z - chunkZ * HexMetrics.ChunkSizeZ;
        chunk.AddCell(localX + localZ * HexMetrics.ChunkSizeX, cell);
    }

    private void CreateCellLabel(Vector3 position, HexCell cell)
    {
        var label = Instantiate<Text>(CellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.Coordinates.ToStringOnSeparateLines();
        cell.UIRect = label.rectTransform;
    }
}