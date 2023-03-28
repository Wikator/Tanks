using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ObjectPoolManager
{
    // This game features a lot of instantiated prefabs, like effects and bullets
    // Rather to delete prefabs after they are no longer needed, and then instantiating brand new ones, thanks to this scripts, they are simply turned off and then back on
    // This doesn't change the game by any way. It's only for optimization reasons

    public static class ObjectPoolManager_SP
    {
        // All prefabs are stored in a dictionary
        // Dictionary's keys are different prefabs, and elements are lists of objects that have been instantiated
        // Every time a bullet or an effect need to be created, this method first checks in the dictionary if there are any inactive ones
        // If one is found, it is simply turned on as active, and it's position and rotation will be set
        // If not, a brand new prefab needs to be instantiated

        private readonly static Dictionary<GameObject, ObjectPoolHashSet> pooledObjects = new();

        public static GameObject GetPooledInstantiated(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (pooledObjects.ContainsKey(prefab))
            {
                GameObject gameObject = pooledObjects[prefab].FirstOrDefault(x => !x.activeInHierarchy);

                if (gameObject)
                {
                    return pooledObjects[prefab].SetActive(gameObject, position, rotation);
                }

                return pooledObjects[prefab].CreateNewObject(prefab, position, rotation, parent);
            }

            pooledObjects[prefab] = new ObjectPoolHashSet();
            return pooledObjects[prefab].CreateNewObject(prefab, position, rotation, parent);
        }

        // Since this class is static, the dictionary is the same for all the scenes, even if its objects do not exist in a current scene
        // Because of that, the dictionary needs to be cleared each time a new scene is loaded

        public static void ResetObjectPool()
        {
            pooledObjects.Clear();
        }


        private class ObjectPoolHashSet : HashSet<GameObject>
        {
            public GameObject SetActive(GameObject gameObject, Vector3 position, Quaternion rotation, Transform parent = null)
            {
                gameObject.transform.SetPositionAndRotation(position, rotation);
                gameObject.SetActive(true);
                return gameObject;
            }

            public GameObject CreateNewObject(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
            {
                GameObject newObject = Object.Instantiate(prefab, position, rotation, parent);
                Add(newObject);
                return newObject;
            }
        }
    }
}

