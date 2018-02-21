using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    public static HexUnit UnitPrefab;

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
}