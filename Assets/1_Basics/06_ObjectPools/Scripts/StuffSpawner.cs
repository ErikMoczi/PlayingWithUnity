using UnityEngine;

public class StuffSpawner : MonoBehaviour
{
    public FloatRange TimeBetweenSpawns;
    public FloatRange Scale;
    public FloatRange RandomVelocity;
    public FloatRange AngularVelocity;
    public float Velocity = 15f;
    public Stuff[] StuffPrefabs;
    public Material StuffMaterial;

    private float _timeSinceLastSpawn;
    private float _currentSpawnDelay;

    private void FixedUpdate()
    {
        _timeSinceLastSpawn += Time.deltaTime;
        if (_timeSinceLastSpawn >= _currentSpawnDelay)
        {
            _timeSinceLastSpawn -= _currentSpawnDelay;
            _currentSpawnDelay = TimeBetweenSpawns.RandomInRange;
            SpawnStuff();
        }
    }

    private void SpawnStuff()
    {
        var prefab = StuffPrefabs[Random.Range(0, StuffPrefabs.Length)];
        var spawn = prefab.GetPooledInstance<Stuff>();
        spawn.transform.localPosition = transform.position;
        spawn.transform.localScale = Vector3.one * Scale.RandomInRange;
        spawn.transform.localRotation = Random.rotation;
        spawn.Rigidbody.velocity = transform.up * Velocity + Random.onUnitSphere * RandomVelocity.RandomInRange;
        spawn.Rigidbody.angularVelocity = Random.onUnitSphere * AngularVelocity.RandomInRange;
        spawn.SetMaterial(StuffMaterial);
    }
}