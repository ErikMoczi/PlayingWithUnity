using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public HexGrid Grid;

    private HexCell _currentCell;
    private HexUnit _selectedUnit;

    void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            if (Input.GetMouseButtonDown(0))
            {
                DoSelection();
            }
            else if (_selectedUnit)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    DoMove();
                }
                else
                {
                    DoPathfinding();
                }
            }
        }
    }

    public void SetEditMode(bool toggle)
    {
        enabled = !toggle;
        Grid.ShowUI(!toggle);
        Grid.ClearPath();
    }

    private bool UpdateCurrentCell()
    {
        var cell = Grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));
        if (cell != _currentCell)
        {
            _currentCell = cell;
            return true;
        }

        return false;
    }

    private void DoSelection()
    {
        Grid.ClearPath();
        UpdateCurrentCell();
        if (_currentCell)
        {
            _selectedUnit = _currentCell.Unit;
        }
    }

    private void DoPathfinding()
    {
        if (UpdateCurrentCell())
        {
            if (_currentCell && _selectedUnit.IsValidDestination(_currentCell))
            {
                Grid.FindPath(_selectedUnit.Location, _currentCell, 24);
            }
            else
            {
                Grid.ClearPath();
            }
        }
    }

    private void DoMove()
    {
        if (Grid.HasPath)
        {
            _selectedUnit.Travel(Grid.GetPath());
            Grid.ClearPath();
        }
    }
}