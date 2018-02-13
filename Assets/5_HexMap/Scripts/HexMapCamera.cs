using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    public float StickMinZoom, StickMaxZoom;
    public float SwivelMinZoom, SwivelMaxZoom;
    public float MoveSpeedMinZoom, MoveSpeedMaxZoom;
    public float RotationSpeed;
    public HexGrid Grid;

    private Transform _swivel, _stick;
    private float _zoom = 1f;
    private float _rotationAngle;
    private static HexMapCamera _instance;

    private void Awake()
    {
        _instance = this;
        _swivel = transform.GetChild(0);
        _stick = _swivel.GetChild(0);
    }

    private void Update()
    {
        var zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        var rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        var xDelta = Input.GetAxis("Horizontal");
        var zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    public static bool Locked
    {
        set { _instance.enabled = !value; }
    }

    public static void ValidatePosition()
    {
        _instance.AdjustPosition(0f, 0f);
    }

    private void AdjustZoom(float delta)
    {
        _zoom = Mathf.Clamp01(_zoom + delta);

        var distance = Mathf.Lerp(StickMinZoom, StickMaxZoom, _zoom);
        _stick.localPosition = new Vector3(0f, 0f, distance);

        var angle = Mathf.Lerp(SwivelMinZoom, SwivelMaxZoom, _zoom);
        _swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }

    private void AdjustRotation(float delta)
    {
        _rotationAngle += delta * RotationSpeed * Time.deltaTime;
        if (_rotationAngle < 0f)
        {
            _rotationAngle += 360f;
        }
        else if (_rotationAngle >= 360f)
        {
            _rotationAngle -= 360f;
        }

        transform.localRotation = Quaternion.Euler(0f, _rotationAngle, 0f);
    }

    private void AdjustPosition(float xDelta, float zDelta)
    {
        var direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        var damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        var distance = Mathf.Lerp(MoveSpeedMinZoom, MoveSpeedMaxZoom, _zoom) * damping * Time.deltaTime;

        var position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        var xMax = (Grid.CellCountX - 0.5f) * (2f * HexMetrics.InnerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        var zMax = (Grid.CellCountZ - 1f) * (1.5f * HexMetrics.OuterRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }
}