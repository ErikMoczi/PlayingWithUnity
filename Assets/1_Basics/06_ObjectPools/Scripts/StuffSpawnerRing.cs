using UnityEngine;

public class StuffSpawnerRing : MonoBehaviour
{
    public int NumberOfSpawners = 20;
    public float Radius = 25f;
    public StuffSpawner SpawnerPrefab;
    public Material[] StuffMaterials;
    public float TiltAngle = -20f;

    private void Awake()
    {
        for (var i = 0; i < NumberOfSpawners; i++) CreateSpawner(i);
    }

    private void CreateSpawner(int index)
    {
        var rotator = new GameObject("Rotator").transform;
        rotator.SetParent(transform, false);
        rotator.localRotation = Quaternion.Euler(0f, index * 360f / NumberOfSpawners, 0f);

        var spawner = Instantiate(SpawnerPrefab);
        var spawnerTransform = spawner.transform;
        spawnerTransform.SetParent(rotator, false);
        spawnerTransform.localPosition = new Vector3(0f, 0f, Radius);
        spawnerTransform.localRotation = Quaternion.Euler(TiltAngle, 0f, 0f);
        spawner.StuffMaterial = StuffMaterials[index % StuffMaterials.Length];
    }
}