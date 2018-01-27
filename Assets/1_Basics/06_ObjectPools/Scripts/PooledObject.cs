using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PooledObject : MonoBehaviour
{
    [NonSerialized] private ObjectPool _poolInstanceForPrefab;
    public ObjectPool Pool { get; set; }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReturnToPool();
    }

    public void ReturnToPool()
    {
        if (Pool)
            Pool.AddObject(this);
        else
            Destroy(gameObject);
    }

    public T GetPooledInstance<T>() where T : PooledObject
    {
        if (!_poolInstanceForPrefab) _poolInstanceForPrefab = ObjectPool.GetPool(this);

        return (T) _poolInstanceForPrefab.GetObject();
    }
}