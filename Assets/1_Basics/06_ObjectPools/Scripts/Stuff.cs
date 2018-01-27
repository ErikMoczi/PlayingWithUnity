using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Stuff : PooledObject
{
    private MeshRenderer[] MeshRenderers;
    public Rigidbody Rigidbody { get; private set; }

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody>();
        MeshRenderers = GetComponentsInChildren<MeshRenderer>();
        //FindObjectsOfType<Stuff>();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("KillZone")) ReturnToPool();
    }

    public void SetMaterial(Material material)
    {
        foreach (var meshRenderer in MeshRenderers) meshRenderer.material = material;
    }
}