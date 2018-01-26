using System;
using UnityEngine;

public class Graph : MonoBehaviour
{
    private const float Pi = Mathf.PI;

    private static readonly GraphFunction[] GraphFunction =
    {
        SineFunction,
        Sine2DFunction,
        MultiSineFunction,
        MultiSine2DFunction,
        Ripple,
        Cylinder,
        WobblyCylinder,
        TwistingCylinder,
        AlmostSphere,
        Sphere,
        PulsingSphere,
        SpindleTorus,
        HornTorus,
        Torus,
        TwistingTorus
    };

    private Transform[] _points;
    public GraphFunctionName FunctionType;
    public Transform PointPrefab;
    [Range(10, 100)] public int Resolution = 50;
    [Range(1, 100)] public int TimeSlow = 1;

    private void Awake()
    {
        var step = 2f / Resolution;
        var scale = Vector3.one * step;
        _points = new Transform[Resolution * Resolution];

        for (var i = 0; i < _points.Length; i++)
        {
            var point = Instantiate(PointPrefab);
            point.localScale = scale;
            point.SetParent(transform, false);
            _points[i] = point;
        }
    }

    private void Update()
    {
        var time = Time.time / TimeSlow;
        var graphFunction = GraphFunction[(int) FunctionType];
        var step = 2f / Resolution;
        for (int i = 0, z = 0; z < Resolution; z++)
        {
            var v = (z + 0.5f) * step - 1f;
            for (var x = 0; x < Resolution; x++, i++)
            {
                var u = (x + 0.5f) * step - 1f;
                _points[i].localPosition = graphFunction(u, v, time);
            }
        }
    }

    private static Vector3 SineFunction(float x, float z, float time)
    {
        Vector3 point;
        point.x = x;
        point.y = Mathf.Sin(Pi * (x + time));
        point.z = z;
        return point;
    }

    private static Vector3 Sine2DFunction(float x, float z, float time)
    {
        Vector3 point;
        point.x = x;
        point.y = Mathf.Sin(Pi * (x + z + time));
        point.y += Mathf.Sin(Pi * (z + time)) * 0.5f;
        point.z = z;
        return point;
    }

    private static Vector3 MultiSineFunction(float x, float z, float time)
    {
        Vector3 point;
        point.x = x;
        point.y = 0f;
        point.z = z;
        point.y = Mathf.Sin(Pi * (x + time));
        point.y += Mathf.Sin(2f * Pi * (x + 2f * time)) * 0.5f;
        point.y *= 2f / 3f;
        return point;
    }

    private static Vector3 MultiSine2DFunction(float x, float z, float time)
    {
        Vector3 point;
        point.x = x;
        point.y = 4f * Mathf.Sin(Pi * (x + z + time * 0.5f));
        point.y += Mathf.Sin(Pi * (x + time));
        point.y += Mathf.Sin(2f * Pi * (z + 2f * time)) * 0.5f;
        point.y *= 1f / 5.5f;
        point.z = z;
        return point;
    }

    private static Vector3 Ripple(float x, float z, float time)
    {
        Vector3 point;
        var d = Mathf.Sqrt(x * x + z * z);
        point.x = x;
        point.y = Mathf.Sin(4f * (Pi * d - time));
        point.y /= 1f + 10f * d;
        point.z = z;
        return point;
    }

    private static Vector3 Cylinder(float u, float v, float time)
    {
        Vector3 point;
        point.x = Mathf.Sin(Pi * u);
        point.y = v;
        point.z = Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 WobblyCylinder(float u, float v, float time)
    {
        Vector3 point;
        var radius = 1f + Mathf.Sin(6f * Pi * u) * 0.2f;
        point.x = radius * Mathf.Sin(Pi * u);
        point.y = v;
        point.z = radius * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 TwistingCylinder(float u, float v, float time)
    {
        Vector3 point;
        var radius = 0.8f + Mathf.Sin(Pi * (6f * u + 2f * v + time)) * 0.2f;
        point.x = radius * Mathf.Sin(Pi * u);
        point.y = v;
        point.z = radius * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 AlmostSphere(float u, float v, float time)
    {
        Vector3 point;
        var radius = Mathf.Cos(Pi * 0.5f * v);
        point.x = radius * Mathf.Sin(Pi * u);
        point.y = v;
        point.z = radius * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 Sphere(float u, float v, float time)
    {
        Vector3 point;
        var radius = Mathf.Cos(Pi * 0.5f * v);
        point.x = radius * Mathf.Sin(Pi * u);
        point.y = Mathf.Sin(Pi * 0.5f * v);
        point.z = radius * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 PulsingSphere(float u, float v, float time)
    {
        Vector3 point;
        var radius = 0.8f + Mathf.Sin(Pi * (6f * u + time)) * 0.1f;
        radius += Mathf.Sin(Pi * (4f * v + time)) * 0.1f;
        var s = (float) (radius * Math.Cos(Pi * 0.5f * v));
        point.x = s * Mathf.Sin(Pi * u);
        point.y = radius * Mathf.Sin(Pi * 0.5f * v);
        point.z = s * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 SpindleTorus(float u, float v, float time)
    {
        Vector3 point;
        var s = (float) Math.Cos(Pi * v) + 0.5f;
        point.x = s * Mathf.Sin(Pi * u);
        point.y = Mathf.Sin(Pi * v);
        point.z = s * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 HornTorus(float u, float v, float time)
    {
        Vector3 point;
        var r1 = 1f;
        var s = (float) Math.Cos(Pi * v) + r1;
        point.x = s * Mathf.Sin(Pi * u);
        point.y = Mathf.Sin(Pi * v);
        point.z = s * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 Torus(float u, float v, float time)
    {
        Vector3 point;
        var r1 = 1f;
        var r2 = 0.5f;
        var s = (float) (r2 * Math.Cos(Pi * v) + r1);
        point.x = s * Mathf.Sin(Pi * u);
        point.y = r2 * Mathf.Sin(Pi * v);
        point.z = s * Mathf.Cos(Pi * u);
        return point;
    }

    private static Vector3 TwistingTorus(float u, float v, float time)
    {
        Vector3 point;
        var r1 = 0.65f + Mathf.Sin(Pi * (6f * u + time)) * 0.1f;
        var r2 = 0.2f + Mathf.Sin(Pi * (4f * v + time)) * 0.05f;
        var s = (float) (r2 * Math.Cos(Pi * v) + r1);
        point.x = s * Mathf.Sin(Pi * u);
        point.y = r2 * Mathf.Sin(Pi * v);
        point.z = s * Mathf.Cos(Pi * u);
        return point;
    }
}