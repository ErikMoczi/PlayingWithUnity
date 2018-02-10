using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] UrbanCollections, farmCollections, plantCollections;

    private Transform _container;

    public void Clear()
    {
        if (_container)
        {
            Destroy(_container.gameObject);
        }

        _container = new GameObject("Features Container").transform;
        _container.SetParent(transform, false);
    }

    public void Apply()
    {
    }

    public void AddFeature(HexCell cell, Vector3 position)
    {
        var hash = HexMetrics.SampleHashGrid(position);
        var prefab = PickPrefab(UrbanCollections, cell.UrbanLevel, hash.A, hash.D);
        var otherPrefab = PickPrefab(farmCollections, cell.FarmLevel, hash.B, hash.D);

        float usedHash = hash.A;
        if (prefab)
        {
            if (otherPrefab && hash.B < hash.A)
            {
                prefab = otherPrefab;
                usedHash = hash.B;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
            usedHash = hash.B;
        }

        otherPrefab = PickPrefab(plantCollections, cell.PlantLevel, hash.C, hash.D);
        if (prefab)
        {
            if (otherPrefab && hash.C < usedHash)
            {
                prefab = otherPrefab;
            }
        }
        else if (otherPrefab)
        {
            prefab = otherPrefab;
        }
        else
        {
            return;
        }

        var instance = Instantiate(prefab);
        position.y += instance.localScale.y * 0.5f;
        instance.localPosition = HexMetrics.Perturb(position);
        instance.localRotation = Quaternion.Euler(0f, 360f * hash.E, 0f);
        instance.SetParent(_container, false);
    }

    private Transform PickPrefab(HexFeatureCollection[] collection, int level, float hash, float choice)
    {
        if (level > 0)
        {
            var thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (hash < thresholds[i])
                {
                    return collection[i].Pick(choice);
                }
            }
        }

        return null;
    }
}