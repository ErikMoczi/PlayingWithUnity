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

    private void Awake()
    {
        SelectColor(0);
    }

    private void Update()
    {
        if (
            Input.GetMouseButton(0) &&
            !EventSystem.current.IsPointerOverGameObject()
        )
        {
            HandleInput();
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

    private void HandleInput()
    {
        var inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit))
        {
            EditCells(HexGrid.GetCell(hit.point));
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
        }
    }
}