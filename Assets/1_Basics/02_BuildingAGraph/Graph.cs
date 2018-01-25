using UnityEngine;

public class Graph : MonoBehaviour
{
    public Transform PointPrefab;
    [Range(10, 100)] public int Resolution = 10;
    private Transform[] _points;

    private void Awake()
    {
        var step = 2f / Resolution;
        var scale = Vector3.one * step;
        var position = Vector3.one;
        _points = new Transform[Resolution];

        for (var i = 0; i < Resolution; i++)
        {
            var point = Instantiate(PointPrefab);
            _points[i] = point;
            position.x = (i + 0.5f) * step - 1f;
            point.localPosition = position;
            point.localScale = scale;
            point.SetParent(transform, false);
        }
    }

    private void Update()
    {
        foreach (var point in _points)
        {
            var position = point.localPosition;
            position.y = Mathf.Sin(Mathf.PI * (position.x + Time.time));
            point.localPosition = position;
        }
    }
}