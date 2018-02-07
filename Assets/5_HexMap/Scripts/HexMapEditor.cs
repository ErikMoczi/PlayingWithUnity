using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] Colors;
    public HexGrid HexGrid;

    private Color _activeColor;
    private int _activeElevation;
    private bool _applyColor;
    private bool _applyElevation = true;
    private int _brushSize;
    private bool _isDrag;
    private HexDirection _dragDirection;
    private HexCell _previousCell;

    private enum OptionalToggle
    {
        Ignore,
        Yes,
        No
    }

    private OptionalToggle _riverMode;

    private void Awake()
    {
        SelectColor(0);
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            HandleInput();
        }
        else
        {
            _previousCell = null;
        }
    }

    public void SelectColor(int index)
    {
        _applyColor = index >= 0;
        if (_applyColor)
        {
            _activeColor = Colors[index];
        }
    }

    public void SetElevation(float elevation)
    {
        _activeElevation = (int) elevation;
    }

    public void SetApplyElevation(bool toogle)
    {
        _applyElevation = toogle;
    }

    public void SetBrushSize(float size)
    {
        _brushSize = (int) size;
    }

    public void ShowUI(bool visible)
    {
        HexGrid.ShowUI(visible);
    }

    public void SetRiverMode(int mode)
    {
        _riverMode = (OptionalToggle) mode;
    }

    private void HandleInput()
    {
        var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            var currentCell = HexGrid.GetCell(hit.point);
            if (_previousCell && _previousCell != currentCell)
            {
                ValidateDrag(currentCell);
            }
            else
            {
                _isDrag = false;
            }

            EditCells(currentCell);
            _previousCell = currentCell;
            _isDrag = true;
        }
        else
        {
            _previousCell = null;
        }
    }

    private void EditCells(HexCell center)
    {
        var centerX = center.Coordinates.X;
        var centerZ = center.Coordinates.Z;
        for (int r = 0, z = centerZ - _brushSize; z <= centerZ; r++, z++)
        {
            for (int x = centerX - r; x <= centerX + _brushSize; x++)
            {
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++)
        {
            for (int x = centerX - _brushSize; x <= centerX + r; x++)
            {
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    private void EditCell(HexCell cell)
    {
        if (cell)
        {
            if (_applyColor)
            {
                cell.Color = _activeColor;
            }

            if (_applyElevation)
            {
                cell.Elevation = _activeElevation;
            }

            if (_riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }
            else if (_isDrag && _riverMode == OptionalToggle.Yes)
            {
                var otherCell = cell.GetNeighbor(_dragDirection.Opposite());
                if (otherCell)
                {
                    otherCell.SetOutgoingRiver(_dragDirection);
                }
            }
        }
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for (_dragDirection = HexDirection.NE; _dragDirection <= HexDirection.NW; _dragDirection++)
        {
            if (_previousCell.GetNeighbor(_dragDirection) == currentCell)
            {
                _isDrag = true;
                return;
            }
        }

        _isDrag = false;
    }
}