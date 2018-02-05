using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public int Width = 6;
    public int Height = 6;
    public Color DefaultColor = Color.white;
    public Color TouchedColor = Color.magenta;
    public HexCell CellPrefab;
    public Text CellLabelPrefab;
    public Texture2D NoiseSource;

    private HexCell[] _cells;
    private Canvas _gridCanvas;
    private HexMesh _hexMesh;

    private void Awake()
    {
        HexMetrics.NoiseSource = NoiseSource;
        _gridCanvas = GetComponentInChildren<Canvas>();
        _hexMesh = GetComponentInChildren<HexMesh>();
        _cells = new HexCell[Height * Width];

        for (int z = 0, i = 0; z < Height; z++)
        {
            for (int x = 0; x < Width; x++)
            {
                CreateCell(x, z, i++);
            }
        }
    }

    private void Start()
    {
        _hexMesh.Triangulate(_cells);
    }

    private void OnEnable()
    {
        HexMetrics.NoiseSource = NoiseSource;
    }

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        var coordinates = HexCoordinates.FromPosition(position);
        var index = coordinates.X + coordinates.Z * Width + coordinates.Z / 2;
        return _cells[index];
    }

    public void Refresh()
    {
        _hexMesh.Triangulate(_cells);
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.InnerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.OuterRadius * 1.5f);

        var cell = _cells[i] = Instantiate<HexCell>(CellPrefab);
        var cellTransform = cell.transform;
        cellTransform.SetParent(_hexMesh.transform, false);
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
                cell.SetNeighbor(HexDirection.SE, _cells[i - Width]);
                if (x > 0)
                {
                    cell.SetNeighbor(HexDirection.SW, _cells[i - Width - 1]);
                }
            }
            else
            {
                cell.SetNeighbor(HexDirection.SW, _cells[i - Width]);
                if (x < Width - 1)
                {
                    cell.SetNeighbor(HexDirection.SE, _cells[i - Width + 1]);
                }
            }
        }

        CreateCellLabel(position, cell);
        cell.Elevation = 0;
    }

    private void CreateCellLabel(Vector3 position, HexCell cell)
    {
        var label = Instantiate<Text>(CellLabelPrefab);
        label.rectTransform.SetParent(_gridCanvas.transform, false);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        label.text = cell.Coordinates.ToStringOnSeparateLines();
        cell.UIRect = label.rectTransform;
    }
}