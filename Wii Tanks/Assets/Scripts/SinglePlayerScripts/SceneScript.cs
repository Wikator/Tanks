using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SceneScript : MonoBehaviour
{
	private List<Spawn_SP> spawns = new();

	private int numberOfSpawns;

    void Start()
    {
		numberOfSpawns = 0;
		
		foreach (Transform spawn in GameObject.Find("DeathmatchSpawns").transform)
		{
			spawns.Add(spawn.GetComponent<Spawn_SP>());
			numberOfSpawns++;
		}
	}


    private void FixedUpdate()
    {
		if (!GameObject.Find("MediumTankSP"))
			return;
		
		if (Random.Range(0, 300) < 1)
		{

			Spawn_SP chosenSpawn = spawns[Random.Range(0, numberOfSpawns)];

			if (chosenSpawn.isOccupied)
				return;


			switch (Random.Range(0, 3))
			{
				case 0:
					ObjectPoolManager_SP.GetPooledInstantiated(Addressables.LoadAssetAsync<GameObject>("EnemyNormalTankPawnSP").WaitForCompletion(), chosenSpawn.transform.position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
				case 1:
					ObjectPoolManager_SP.GetPooledInstantiated(Addressables.LoadAssetAsync<GameObject>("EnemyDestroyerPawnSP").WaitForCompletion(), chosenSpawn.transform.position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
				case 2:
					ObjectPoolManager_SP.GetPooledInstantiated(Addressables.LoadAssetAsync<GameObject>("EnemyScoutPawnSP").WaitForCompletion(), chosenSpawn.transform.position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
			}
			
		}
	}
}
