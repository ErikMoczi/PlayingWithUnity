using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private readonly List<PooledObject> _availableObjects = new List<PooledObject>();
    private PooledObject _prefab;

    public PooledObject GetObject()
    {
        PooledObject obj;
        var lastAvailablIndex = _availableObjects.Count - 1;
        if (lastAvailablIndex >= 0)
        {
            obj = _availableObjects[lastAvailablIndex];
            _availableObjects.RemoveAt(lastAvailablIndex);
            obj.gameObject.SetActive(true);
        }
        else
        {
            obj = Instantiate(_prefab);
            obj.transform.SetParent(transform, false);
            obj.Pool = this;
        }

        return obj;
    }

    public void AddObject(PooledObject obj)
    {
        obj.gameObject.SetActive(false);
        _availableObjects.Add(obj);
    }

    public static ObjectPool GetPool(PooledObject prefab)
    {
        GameObject obj;
        ObjectPool pool;
        if (Application.isEditor)
        {
            obj = GameObject.Find(prefab.name + " Pool");
            if (obj)
            {
                pool = obj.GetComponent<ObjectPool>();
                if (pool) return pool;
            }
        }

        obj = new GameObject(prefab.name + " Pool");
        DontDestroyOnLoad(obj);
        pool = obj.AddComponent<ObjectPool>();
        pool._prefab = prefab;
        return pool;
    }
}