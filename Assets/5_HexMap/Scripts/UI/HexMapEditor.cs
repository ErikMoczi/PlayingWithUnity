using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid HexGrid;
    public Material TerrainMaterial;

    private HexDirection _dragDirection;
    private HexCell _previousCell;
    private bool _isDrag;

    private int _activeTerrainTypeIndex;

    private int _activeElevation;
    private bool _applyElevation;

    private int _brushSize;

    private int _activeWaterLevel;
    private bool _applyWaterLevel;

    private int _activeUrbanLevel, _activeFarmLevel, _activePlantLevel, _activeSpecialIndex;
    private bool _applyUrbanLevel, _applyFarmLevel, _applyPlantLevel, _applySpecialIndex;

    private bool _editMode;

    private enum OptionalToggle
    {
        Ignore,
        Yes,
        No
    }

    private OptionalToggle _riverMode, _roadMode, _walledMode;

    #region Unity

    private void Awake()
    {
        TerrainMaterial.DisableKeyword("GRID_ON");
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

    #endregion

    #region UIActions

    public void SetTerrainTypeIndex(int index)
    {
        _activeTerrainTypeIndex = index;
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

    public void SetRiverMode(int mode)
    {
        _riverMode = (OptionalToggle) mode;
    }

    public void SetRoadMode(int mode)
    {
        _roadMode = (OptionalToggle) mode;
    }

    public void SetApplyWaterLevel(bool toggle)
    {
        _applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float level)
    {
        _activeWaterLevel = (int) level;
    }

    public void SetApplyUrbanLevel(bool toggle)
    {
        _applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel(float level)
    {
        _activeUrbanLevel = (int) level;
    }

    public void SetApplyFarmLevel(bool toggle)
    {
        _applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level)
    {
        _activeFarmLevel = (int) level;
    }

    public void SetApplyPlantLevel(bool toggle)
    {
        _applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level)
    {
        _activePlantLevel = (int) level;
    }

    public void SetWalledMode(int mode)
    {
        _walledMode = (OptionalToggle) mode;
    }

    public void SetApplySpecialIndex(bool toggle)
    {
        _applySpecialIndex = toggle;
    }

    public void SetSpecialIndex(float index)
    {
        _activeSpecialIndex = (int) index;
    }

    public void ShowGrid(bool visible)
    {
        if (visible)
        {
            TerrainMaterial.EnableKeyword("GRID_ON");
        }
        else
        {
            TerrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    public void SetEditMode(bool toggle)
    {
        _editMode = toggle;
        HexGrid.ShowUI(!toggle);
    }

    #endregion

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

            if (_editMode)
            {
                EditCells(currentCell);
            }
            else
            {
                HexGrid.FindDistancesTo(currentCell);
            }

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
            if (_activeTerrainTypeIndex >= 0)
            {
                cell.TerrainTypeIndex = _activeTerrainTypeIndex;
            }

            if (_applyElevation)
            {
                cell.Elevation = _activeElevation;
            }

            if (_applyWaterLevel)
            {
                cell.WaterLevel = _activeWaterLevel;
            }

            if (_applySpecialIndex)
            {
                cell.SpecialIndex = _activeSpecialIndex;
            }

            if (_applyUrbanLevel)
            {
                cell.UrbanLevel = _activeUrbanLevel;
            }

            if (_applyFarmLevel)
            {
                cell.FarmLevel = _activeFarmLevel;
            }

            if (_applyPlantLevel)
            {
                cell.PlantLevel = _activePlantLevel;
            }

            if (_riverMode == OptionalToggle.No)
            {
                cell.RemoveRiver();
            }

            if (_roadMode == OptionalToggle.No)
            {
                cell.RemoveRoads();
            }

            if (_walledMode != OptionalToggle.Ignore)
            {
                cell.Walled = _walledMode == OptionalToggle.Yes;
            }

            if (_isDrag)
            {
                var otherCell = cell.GetNeighbor(_dragDirection.Opposite());
                if (otherCell)
                {
                    if (_riverMode == OptionalToggle.Yes)
                    {
                        otherCell.SetOutgoingRiver(_dragDirection);
                    }

                    if (_roadMode == OptionalToggle.Yes)
                    {
                        otherCell.AddRoad(_dragDirection);
                    }
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