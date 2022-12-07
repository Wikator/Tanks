using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private static readonly HashSet<GameObject> pooledObjects = new();


    public static GameObject GetPooledInstantiated(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        foreach (GameObject gameObject in pooledObjects)
        {
            if (!gameObject.activeSelf && gameObject.name == prefab.name + "(Clone)")
            {
                gameObject.transform.SetPositionAndRotation(position, rotation);
                gameObject.SetActive(true);
                return gameObject;
            }
        }

        GameObject newObject = Instantiate(prefab, position, rotation, parent);
        pooledObjects.Add(newObject);
        return newObject;
    }
}
