using UnityEngine;

public class NucleonSpawner : MonoBehaviour
{
    public float TimeBetweenSpawns = 0.05f;
    public float SpawnDistance = 15f;
    public Nucleon[] NucleonPrefabs;

    private float _timeSinceLastSpawn;

    private void FixedUpdate()
    {
        _timeSinceLastSpawn += Time.deltaTime;
        if (_timeSinceLastSpawn >= TimeBetweenSpawns)
        {
            _timeSinceLastSpawn -= TimeBetweenSpawns;
            SpawnNucleon();
        }
    }

    private void SpawnNucleon()
    {
        Nucleon prefab = NucleonPrefabs[Random.Range(0, NucleonPrefabs.Length)];
        Nucleon spawn = Instantiate<Nucleon>(prefab);
        spawn.transform.localPosition = Random.onUnitSphere * SpawnDistance;
    }
}