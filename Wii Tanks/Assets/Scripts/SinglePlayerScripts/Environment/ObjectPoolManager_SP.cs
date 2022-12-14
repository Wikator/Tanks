using System.Collections.Generic;
using UnityEngine;

public sealed class ObjectPoolManager_SP : MonoBehaviour
{
    private readonly static Dictionary<GameObject, HashSet<GameObject>> pooledObjects = new();

    public static GameObject GetPooledInstantiated(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (pooledObjects.ContainsKey(prefab))
        {
            foreach (GameObject gameObject in pooledObjects[prefab])
            {
                if (!gameObject.activeInHierarchy)
                {
                    gameObject.transform.SetPositionAndRotation(position, rotation);
                    gameObject.SetActive(true);
                    return gameObject;
                }
            }

            return CreateNewObject(prefab, position, rotation, parent);
        }

        pooledObjects[prefab] = new HashSet<GameObject>();
        return CreateNewObject(prefab, position, rotation, parent);
    }


    private static GameObject CreateNewObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        GameObject newObject = Instantiate(prefab, position, rotation, parent);
        pooledObjects[prefab].Add(newObject);
        return newObject;
    }

    public static void ResetObjectPool()
    {
        pooledObjects.Clear();
    }
}

