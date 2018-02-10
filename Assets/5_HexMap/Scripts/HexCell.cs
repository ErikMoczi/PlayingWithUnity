﻿using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates Coordinates;
    public RectTransform UIRect;
    public HexGridChunk Chunk;

    [SerializeField] private HexCell[] _neighbors;

    private int _elevation = int.MinValue;

    public int Elevation
    {
        get { return _elevation; }
        set
        {
            if (_elevation == value)
            {
                return;
            }

            _elevation = value;
            var position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
            transform.localPosition = position;

            var uiPosition = UIRect.localPosition;
            uiPosition.z = -position.y;
            UIRect.localPosition = uiPosition;

            ValidateRivers();

            for (var i = 0; i < _roads.Length; i++)
            {
                if (_roads[i] && GetElevationDifference((HexDirection) i) > 1)
                {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    private Color _color;

    public Color Color
    {
        get { return _color; }
        set
        {
            if (_color == value)
            {
                return;
            }

            _color = value;
            Refresh();
        }
    }

    public Vector3 Position
    {
        get { return transform.localPosition; }
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return _neighbors[(int) direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        _neighbors[(int) direction] = cell;
        cell._neighbors[(int) direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(_elevation, _neighbors[(int) direction]._elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(_elevation, otherCell._elevation);
    }

    public int GetElevationDifference(HexDirection direction)
    {
        var difference = _elevation - GetNeighbor(direction)._elevation;
        return difference >= 0 ? difference : -difference;
    }

    #region River

    #region Attributes

    private bool _hasIncomingRiver, _hasOutgoingRiver;
    private HexDirection _incomingRiver, _outgoingRiver;

    #endregion

    #region Properties

    public bool HasIncomingRiver
    {
        get { return _hasIncomingRiver; }
    }

    public bool HasOutgoingRiver
    {
        get { return _hasOutgoingRiver; }
    }

    public HexDirection IncomingRiver
    {
        get { return _incomingRiver; }
    }

    public HexDirection OutgoingRiver
    {
        get { return _outgoingRiver; }
    }

    public bool HasRiver
    {
        get { return _hasIncomingRiver || _hasOutgoingRiver; }
    }

    public bool HasRiverBeginOrEnd
    {
        get { return _hasIncomingRiver != _hasOutgoingRiver; }
    }

    public float StreamBedY
    {
        get { return (_elevation + HexMetrics.StreamBedElevationOffset) * HexMetrics.ElevationStep; }
    }

    public HexDirection RiverBeginOrEndDirection
    {
        get { return HasIncomingRiver ? IncomingRiver : OutgoingRiver; }
    }

    #endregion

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return _hasIncomingRiver && _incomingRiver == direction || _hasOutgoingRiver && _outgoingRiver == direction;
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void RemoveOutgoingRiver()
    {
        if (!_hasOutgoingRiver)
        {
            return;
        }

        _hasOutgoingRiver = false;
        RefreshSelfOnly();

        var neighbor = GetNeighbor(_outgoingRiver);
        neighbor._hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver()
    {
        if (!_hasIncomingRiver)
        {
            return;
        }

        _hasIncomingRiver = false;
        RefreshSelfOnly();

        var neighbor = GetNeighbor(_incomingRiver);
        neighbor._hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (_hasOutgoingRiver && _outgoingRiver == direction)
        {
            return;
        }

        var neighbor = GetNeighbor(direction);
        if (!IsValidRiverDestination(neighbor))
        {
            return;
        }

        RemoveOutgoingRiver();
        if (_hasIncomingRiver && _incomingRiver == direction)
        {
            RemoveIncomingRiver();
        }

        _hasOutgoingRiver = true;
        _outgoingRiver = direction;

        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();

        SetRoad((int) direction, false);
    }

    public float RiverSurfaceY
    {
        get { return (_elevation + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep; }
    }

    private bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (_elevation >= neighbor._elevation || _waterLevel == neighbor._elevation);
    }

    private void ValidateRivers()
    {
        if (HasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(OutgoingRiver)))
        {
            RemoveOutgoingRiver();
        }

        if (HasIncomingRiver && !GetNeighbor(IncomingRiver).IsValidRiverDestination(this))
        {
            RemoveIncomingRiver();
        }
    }

    #endregion

    #region Road

    #region Attributes

    [SerializeField] private bool[] _roads;

    #endregion

    #region Properties

    public bool HasRoads
    {
        get
        {
            for (int i = 0; i < _roads.Length; i++)
            {
                if (_roads[i])
                {
                    return true;
                }
            }

            return false;
        }
    }

    #endregion

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return _roads[(int) direction];
    }


    public void RemoveRoads()
    {
        for (int i = 0; i < _neighbors.Length; i++)
        {
            if (_roads[i])
            {
                _roads[i] = false;
                _neighbors[i]._roads[(int) ((HexDirection) i).Opposite()] = false;
                _neighbors[i].RefreshSelfOnly();
                RefreshSelfOnly();
            }
        }
    }

    public void AddRoad(HexDirection direction)
    {
        if (!_roads[(int) direction] && !HasRiverThroughEdge(direction) && GetElevationDifference(direction) <= 1)
        {
            SetRoad((int) direction, true);
        }
    }

    private void SetRoad(int index, bool state)
    {
        _roads[index] = state;
        _neighbors[index]._roads[(int) ((HexDirection) index).Opposite()] = state;
        _neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    #endregion

    #region Water

    #region Attributes

    private int _waterLevel;

    #endregion

    #region Properties

    public int WaterLevel
    {
        get { return _waterLevel; }
        set
        {
            if (_waterLevel == value)
            {
                return;
            }

            _waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderwater
    {
        get { return _waterLevel > _elevation; }
    }

    public float WaterSurfaceY
    {
        get { return (_waterLevel + HexMetrics.WaterElevationOffset) * HexMetrics.ElevationStep; }
    }

    #endregion

    #endregion

    #region Refresh

    private void Refresh()
    {
        if (Chunk)
        {
            Chunk.Refresh();
            for (int i = 0; i < _neighbors.Length; i++)
            {
                var neighbor = _neighbors[i];
                if (neighbor != null && neighbor.Chunk != Chunk)
                {
                    neighbor.Chunk.Refresh();
                }
            }
        }
    }

    private void RefreshSelfOnly()
    {
        Chunk.Refresh();
    }

    #endregion
}