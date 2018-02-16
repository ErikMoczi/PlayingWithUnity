using System.IO;
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

            _elevation = value;
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
                Refresh();
            }
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
        writer.Write((byte) _elevation);
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
        for (int i = 0; i < _roads.Length; i++)
        {
            if (_roads[i])
            {
                roadFlags |= 1 << i;
            }
        }

        writer.Write((byte) roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        _terrainTypeIndex = reader.ReadByte();
        _elevation = reader.ReadByte();
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

        var roadFlags = reader.ReadByte();
        for (int i = 0; i < _roads.Length; i++)
        {
            _roads[i] = (roadFlags & (1 << i)) != 0;
        }
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
        set
        {
            _distance = value;
            UpdateDistanceLabel();
        }
    }

    #endregion

    private void UpdateDistanceLabel()
    {
        var label = UIRect.GetComponent<Text>();
        label.text = _distance == int.MaxValue ? "" : _distance.ToString();
    }

    #endregion
}