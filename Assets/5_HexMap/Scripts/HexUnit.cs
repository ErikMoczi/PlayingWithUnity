using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public static HexUnit UnitPrefab;

    private const float TravelSpeed = 4f;
    private const float RotationSpeed = 180f;

    private HexCell _location;

    public HexCell Location
    {
        get { return _location; }
        set
        {
            if (_location)
            {
                _location.Unit = null;
            }

            _location = value;
            value.Unit = this;
            transform.localPosition = value.Position;
        }
    }

    private float orientation;

    public float Orientation
    {
        get { return orientation; }
        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0f, value, 0f);
        }
    }

    private List<HexCell> _pathToTravel;

    private void OnEnable()
    {
        if (_location)
        {
            transform.localPosition = _location.Position;
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
        _location.Unit = null;
        Destroy(gameObject);
    }

    public void Save(BinaryWriter writer)
    {
        _location.Coordinates.Save(writer);
        writer.Write(orientation);
    }

    public bool IsValidDestination(HexCell cell)
    {
        return !cell.IsUnderwater && !cell.Unit;
    }

    public void Travel(List<HexCell> path)
    {
        Location = path[path.Count - 1];
        _pathToTravel = path;
        StopAllCoroutines();
        StartCoroutine(TravelPath());
    }

    private IEnumerator TravelPath()
    {
        Vector3 a, b, c = _pathToTravel[0].Position;
        transform.localPosition = c;
        yield return LookAt(_pathToTravel[1].Position);

        var t = Time.deltaTime * TravelSpeed;
        for (int i = 1; i < _pathToTravel.Count; i++)
        {
            a = c;
            b = _pathToTravel[i - 1].Position;
            c = (b + _pathToTravel[i].Position) * 0.5f;
            for (; t < 1f; t += Time.deltaTime * TravelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                var d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }

            t -= 1f;
        }

        a = c;
        b = _pathToTravel[_pathToTravel.Count - 1].Position;
        c = b;
        for (; t < 1f; t += Time.deltaTime * TravelSpeed)
        {
            transform.localPosition = Bezier.GetPoint(a, b, c, t);
            var d = Bezier.GetDerivative(a, b, c, t);
            d.y = 0f;
            transform.localRotation = Quaternion.LookRotation(d);
            yield return null;
        }

        transform.localPosition = _location.Position;
        orientation = transform.localRotation.eulerAngles.y;
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
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }
}