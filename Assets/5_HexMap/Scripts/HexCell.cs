using UnityEngine;

public class HexCell : MonoBehaviour
{
    public HexCoordinates Coordinates;
    public Color Color;
    public RectTransform UIRect;

    [SerializeField] private HexCell[] _neighbors;

    public int Elevation
    {
        get { return _elevation; }
        set
        {
            _elevation = value;
            var position = transform.localPosition;
            position.y = value * HexMetrics.ElevationStep;
            transform.localPosition = position;

            var uiPosition = UIRect.localPosition;
            uiPosition.z = _elevation * -HexMetrics.ElevationStep;
            UIRect.localPosition = uiPosition;
        }
    }

    private int _elevation;

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
}