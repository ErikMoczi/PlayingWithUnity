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
}