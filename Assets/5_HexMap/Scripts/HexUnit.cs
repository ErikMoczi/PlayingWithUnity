using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public static HexUnit UnitPrefab;

    private const float TravelSpeed = 4f;
    private const float RotationSpeed = 180f;

    public int VisionRange
    {
        get { return 3; }
    }

    private HexCell _location, _currentTravelLocation;

    public HexCell Location
    {
        get { return _location; }
        set
        {
            if (_location)
            {
                Grid.DecreaseVisibility(_location, VisionRange);
                _location.Unit = null;
            }

            _location = value;
            value.Unit = this;
            Grid.IncreaseVisibility(value, VisionRange);
            transform.localPosition = value.Position;
        }
    }

    private float _orientation;

    public float Orientation
    {
        get { return _orientation; }
        set
        {
            _orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    public int Speed
    {
        get { return 24; }
    }

    public HexGrid Grid { get; set; }

    private List<HexCell> _pathToTravel;

    private void OnEnable()
    {
        if (_location)
        {
            transform.localPosition = _location.Position;
            if (_currentTravelLocation)
            {
                Grid.IncreaseVisibility(_location, VisionRange);
                Grid.DecreaseVisibility(_currentTravelLocation, VisionRange);
                _currentTravelLocation = null;
            }
        }
    }

    public static void Load(BinaryReader reader, HexGrid grid)
    {
        var coordinates = HexCoordinates.Load(reader);
        var orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(UnitPrefab), grid.GetCell(coordinates), orientation);
    }

    public void ValidateLocation()
    {
        transform.localPosition = _location.Position;
    }

    public void Die()
    {
        if (_location)
        {
            Grid.DecreaseVisibility(_location, VisionRange);
        }

        _location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        _location.Coordinates.Save(writer);
        writer.Write(_orientation);
    }

    public bool IsValidDestination(HexCell cell)
    {
        return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
    }

    public void Travel(List<HexCell> path)
    {
        _location.Unit = null;
        _location = path[path.Count - 1];
        _location.Unit = this;
        _pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    public int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction)
    {
        if (!IsValidDestination(toCell))
        {
            return -1;
        }

        var edgeType = fromCell.GetEdgeType(toCell);
        if (edgeType == HexEdgeType.Cliff)
        {
            return -1;
        }

        int moveCost;
        if (fromCell.HasRoadThroughEdge(direction))
        {
            moveCost = 1;
        }
        else if (fromCell.Walled != toCell.Walled)
        {
            return -1;
        }
        else
        {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        }

        return moveCost;
    }

    private IEnumerator TravelPath()
    {
        Vector3 a, b, c = _pathToTravel[0].Position;
        yield return LookAt(_pathToTravel[1].Position);
        Grid.DecreaseVisibility(_currentTravelLocation ? _currentTravelLocation : _pathToTravel[0], VisionRange);

        var t = Time.deltaTime * TravelSpeed;
        for (var i = 1; i < _pathToTravel.Count; i++)
        {
            _currentTravelLocation = _pathToTravel[i];
            a = c;
            b = _pathToTravel[i - 1].Position;
            c = (b + _currentTravelLocation.Position) * 0.5f;
            Grid.IncreaseVisibility(_pathToTravel[i], VisionRange);
            for (; t < 1f; t += Time.deltaTime * TravelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                var d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }

            Grid.DecreaseVisibility(_pathToTravel[i], VisionRange);
            t -= 1f;
        }

        _currentTravelLocation = null;

        a = c;
        b = _location.Position;
        c = b;
        Grid.IncreaseVisibility(_location, VisionRange);
        for (; t < 1f; t += Time.deltaTime * TravelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            var d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }

        transform.localPosition = _location.Position;
        _orientation = transform.localRotation.eulerAngles.y;
        ListPool<HexCell>.Add(_pathToTravel);
        _pathToTravel = null;
    }

    private IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;
        var fromRotation = transform.localRotation;
        var toRotation = Quaternion.LookRotation(point - transform.localPosition);
        var angle = Quaternion.Angle(fromRotation, toRotation);
        if (angle > 0f)
        {
            var speed = RotationSpeed / angle;
            for (var t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        _orientation = transform.localRotation.eulerAngles.y;
    }
}