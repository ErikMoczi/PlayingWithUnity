using UnityEngine;

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

            if (_hasOutgoingRiver && _elevation < GetNeighbor(_outgoingRiver)._elevation)
            {
                RemoveOutgoingRiver();
            }

            if (_hasIncomingRiver && _elevation > GetNeighbor(_incomingRiver)._elevation)
            {
                RemoveIncomingRiver();
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

    private bool _hasIncomingRiver, _hasOutgoingRiver;
    private HexDirection _incomingRiver, _outgoingRiver;

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
        if (!neighbor || _elevation < neighbor._elevation)
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
        RefreshSelfOnly();

        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();
        neighbor.RefreshSelfOnly();
    }

    public float RiverSurfaceY
    {
        get { return (_elevation + HexMetrics.RiverSurfaceElevationOffset) * HexMetrics.ElevationStep; }
    }

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
}