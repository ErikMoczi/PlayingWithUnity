using System.Collections;
using UnityEngine;

public class Fractal : MonoBehaviour
{
    private static readonly Vector3[] ChildDirections =
    {
        Vector3.up,
        Vector3.right,
        Vector3.left,
        Vector3.forward,
        Vector3.back
    };

    private static readonly Quaternion[] ChildOrientations =
    {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f),
        Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f),
        Quaternion.Euler(-90f, 0f, 0f)
    };

    public Mesh[] Meshes;
    public Material Material;
    public float ChildScale = 0.5f;
    public int MaxDepth = 4;
    public float SpawnProbability = 0.7f;
    public float MaxRotationSpeed = 60f;
    public float MaxTwist = 20f;

    private int _depth;
    private Material[,] _materials;
    private float _rotationSpeed;

    private void Start()
    {
        if (_materials == null)
        {
            InitializeMaterials();
        }

        _rotationSpeed = Random.Range(-MaxRotationSpeed, MaxRotationSpeed);
        transform.Rotate(Random.Range(-MaxTwist, MaxTwist), 0f, 0f);

        gameObject.AddComponent<MeshFilter>().mesh = Meshes[Random.Range(0, Meshes.Length)];
        gameObject.AddComponent<MeshRenderer>().material = _materials[_depth, Random.Range(0, 2)];
        if (_depth < MaxDepth) StartCoroutine(CreateChildren());
    }

    private void Update()
    {
        transform.Rotate(0f, _rotationSpeed * Time.deltaTime, 0f);
    }

    private void InitializeMaterials()
    {
        _materials = new Material[MaxDepth + 1, 2];
        for (int i = 0; i <= MaxDepth; i++)
        {
            var t = i / (MaxDepth - 1f);
            t *= t;
            _materials[i, 0] = new Material(Material) {color = Color.Lerp(Color.white, Color.yellow, t)};
            _materials[i, 1] = new Material(Material) {color = Color.Lerp(Color.white, Color.cyan, t)};
        }

        _materials[MaxDepth, 0].color = Color.magenta;
        _materials[MaxDepth, 1].color = Color.red;
    }

    private IEnumerator CreateChildren()
    {
        for (int i = 0; i < ChildDirections.Length; i++)
        {
            if (Random.value < SpawnProbability)
            {
                yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
                new GameObject("Fractal Gild").AddComponent<Fractal>().Initialize(this, i);
            }
        }
    }

    private void Initialize(Fractal parent, int childIndex)
    {
        Meshes = parent.Meshes;
        _materials = parent._materials;
//        Material = parent.Material;
        MaxDepth = parent.MaxDepth;
        _depth = parent._depth + 1;
        ChildScale = parent.ChildScale;
        SpawnProbability = parent.SpawnProbability;
        MaxRotationSpeed = parent.MaxRotationSpeed;
        MaxTwist = parent.MaxTwist;
        transform.parent = parent.transform;
        transform.localScale = Vector3.one * ChildScale;
        transform.localPosition = ChildDirections[childIndex] * (0.5f + 0.5f * ChildScale);
        transform.localRotation = ChildOrientations[childIndex];
    }
}