using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;


public class ObjectPoolManager : IManager
{
    private Dictionary<GameObject, ObjectPool<GameObject>> pools = new();

    public ObjectPool<GameObject> CreateObjectPool(GameObject pooledObject, Func<GameObject> createFunc, Action<GameObject> onGet = null, Action<GameObject> onRelease = null)
    {
        if (pools.ContainsKey(pooledObject))
        {
            return GetObjectPool(pooledObject);
        }

        ObjectPool<GameObject> pool = new
            (
                createFunc: createFunc, 
                actionOnGet: onGet,
                actionOnRelease: onRelease,
                defaultCapacity: 100,
                maxSize: 500
            );

        pools.Add(pooledObject, pool);
        return pool;
    }

    public ObjectPool<GameObject> GetObjectPool(GameObject pooledObject)
    {
        if (pools.TryGetValue(pooledObject, out ObjectPool<GameObject> pool))
        {
            return pool;
        }
        else
        {
            throw new KeyNotFoundException($"No pool found for type {pooledObject.name}.");
        }
    }

    public void Clear()
    {
        foreach (var pool in pools.Values)
            pool.Dispose();   // ObjectPool은 IDisposable — 씬 리로드가 인스턴스를 지워줘도 계약은 지킨다 (검수 v6)
        pools.Clear();
    }

    public void Initialize()
    {
       
    }
}
