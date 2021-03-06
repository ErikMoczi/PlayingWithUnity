﻿using System.IO;
using UnityEngine;
using UnityEngine.UI;

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

            var originalViewElevation = ViewElevation;
            _elevation = value;
            if (ViewElevation != originalViewElevation)
            {
                ShaderData.ViewElevationChanged();
            }

            RefreshPosition();
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

    private void RefreshPosition()
    {
        var position = transform.localPosition;
        position.y = _elevation * HexMetrics.ElevationStep;
        position.y += (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.ElevationPerturbStrength;
        transform.localPosition = position;

        var uiPosition = UIRect.localPosition;
        uiPosition.z = -position.y;
        UIRect.localPosition = uiPosition;
    }

    private int _terrainTypeIndex;

    public int TerrainTypeIndex
    {
        get { return _terrainTypeIndex; }
        set
        {
            if (_terrainTypeIndex != value)
            {
                _terrainTypeIndex = value;
                ShaderData.RefreshTerrain(this);
            }
        }
    }

    public int Index { get; set; }

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

    public int ColumnIndex { get; set; }

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
        SpecialIndex = 0;

        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();
        neighbor.SpecialIndex = 0;

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
            for (var i = 0; i < _roads.Length; i++)
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
        for (var i = 0; i < _neighbors.Length; i++)
        {
            if (_roads[i])
            {
                SetRoad(i, false);
            }
        }
    }

    public void AddRoad(HexDirection direction)
    {
        if (!_roads[(int) direction] && !HasRiverThroughEdge(direction) && !IsSpecial &&
            !GetNeighbor(direction).IsSpecial && GetElevationDifference(direction) <= 1)
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

            var originalViewElevation = ViewElevation;
            _waterLevel = value;
            if (ViewElevation != originalViewElevation)
            {
                ShaderData.ViewElevationChanged();
            }

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
            for (var i = 0; i < _neighbors.Length; i++)
            {
                var neighbor = _neighbors[i];
                if (neighbor != null && neighbor.Chunk != Chunk)
                {
                    neighbor.Chunk.Refresh();
                }
            }

            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }
    }

    private void RefreshSelfOnly()
    {
        Chunk.Refresh();
        if (Unit)
        {
            Unit.ValidateLocation();
        }
    }

    #endregion

    #region Features

    #region Attributes

    private int _urbanLevel, _farmLevel, _plantLevel;
    private bool _walled;
    private int _specialIndex;

    #endregion

    #region Properties

    public int UrbanLevel
    {
        get { return _urbanLevel; }
        set
        {
            if (_urbanLevel != value)
            {
                _urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get { return _farmLevel; }
        set
        {
            if (_farmLevel != value)
            {
                _farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get { return _plantLevel; }
        set
        {
            if (_plantLevel != value)
            {
                _plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public bool Walled
    {
        get { return _walled; }
        set
        {
            if (_walled != value)
            {
                _walled = value;
                Refresh();
            }
        }
    }

    public int SpecialIndex
    {
        get { return _specialIndex; }
        set
        {
            if (_specialIndex != value && !HasRiver)
            {
                _specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial
    {
        get { return _specialIndex > 0; }
    }

    #endregion

    #endregion

    #region SaveLoad

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte) _terrainTypeIndex);
        writer.Write((byte) (_elevation + 127));
        writer.Write((byte) _waterLevel);
        writer.Write((byte) _urbanLevel);
        writer.Write((byte) _farmLevel);
        writer.Write((byte) _plantLevel);
        writer.Write((byte) _specialIndex);
        writer.Write(_walled);

        if (_hasIncomingRiver)
        {
            writer.Write((byte) (_incomingRiver + 128));
        }
        else
        {
            writer.Write((byte) 0);
        }

        if (_hasOutgoingRiver)
        {
            writer.Write((byte) (_outgoingRiver + 128));
        }
        else
        {
            writer.Write((byte) 0);
        }

        var roadFlags = 0;
        for (var i = 0; i < _roads.Length; i++)
        {
            if (_roads[i])
            {
                roadFlags |= 1 << i;
            }
        }

        writer.Write((byte) roadFlags);
        writer.Write(IsExplored);
    }

    public void Load(BinaryReader reader, int header)
    {
        _terrainTypeIndex = reader.ReadByte();
        ShaderData.RefreshTerrain(this);
        _elevation = reader.ReadByte();
        if (header >= 4)
        {
            _elevation -= 127;
        }

        RefreshPosition();
        _waterLevel = reader.ReadByte();
        _urbanLevel = reader.ReadByte();
        _farmLevel = reader.ReadByte();
        _plantLevel = reader.ReadByte();
        _specialIndex = reader.ReadByte();
        _walled = reader.ReadBoolean();

        var riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            _hasIncomingRiver = true;
            _incomingRiver = (HexDirection) (riverData - 128);
        }
        else
        {
            _hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();
        if (riverData >= 128)
        {
            _hasOutgoingRiver = true;
            _outgoingRiver = (HexDirection) (riverData - 128);
        }
        else
        {
            _hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();
        for (var i = 0; i < _roads.Length; i++)
        {
            _roads[i] = (roadFlags & (1 << i)) != 0;
        }

        IsExplored = header >= 3 ? reader.ReadBoolean() : false;
        ShaderData.RefreshVisibility(this);
    }

    #endregion

    #region Distance

    #region Attributes

    private int _distance;

    #endregion

    #region Properties

    public int Distance
    {
        get { return _distance; }
        set { _distance = value; }
    }

    #endregion

    public void SetLabel(string text)
    {
        var label = UIRect.GetComponent<Text>();
        label.text = text;
    }

    #endregion

    #region Path

    #region Properties

    public HexCell PathFrom { get; set; }
    public int SearchHeuristic { get; set; }
    public HexCell NextWithSamePriority { get; set; }
    public int SearchPhase { get; set; }

    public int SearchPriority
    {
        get { return _distance + SearchHeuristic; }
    }

    #endregion

    public void DisableHighlight()
    {
        var highlight = UIRect.GetChild(0).GetComponent<Image>();
        highlight.enabled = false;
    }

    public void EnableHighlight(Color color)
    {
        var highlight = UIRect.GetChild(0).GetComponent<Image>();
        highlight.color = color;
        highlight.enabled = true;
    }

    #endregion

    #region Unit

    #region Properties

    public HexUnit Unit { get; set; }

    #endregion

    #endregion

    #region Shader

    #region Properties

    public HexCellShaderData ShaderData { get; set; }

    #endregion

    public void SetMapData(float data)
    {
        ShaderData.SetMapData(this, data);
    }

    #endregion

    #region Visibility

    #region Attributes

    private int _visibility;
    private bool _explored;

    #endregion

    #region Properties

    public bool IsVisible
    {
        get { return _visibility > 0 && Explorable; }
    }

    public bool IsExplored
    {
        get { return _explored && Explorable; }
        private set { _explored = value; }
    }

    public bool Explorable { get; set; }

    #endregion

    public void IncreaseVisibility()
    {
        _visibility += 1;
        if (_visibility == 1)
        {
            IsExplored = true;
            ShaderData.RefreshVisibility(this);
        }
    }

    public void DecreaseVisibility()
    {
        _visibility -= 1;
        if (_visibility == 0)
        {
            ShaderData.RefreshVisibility(this);
        }
    }

    public void ResetVisibility()
    {
        if (_visibility > 0)
        {
            _visibility = 0;
            ShaderData.RefreshVisibility(this);
        }
    }

    public int ViewElevation
    {
        get { return _elevation >= _waterLevel ? _elevation : _waterLevel; }
    }

    #endregion
}