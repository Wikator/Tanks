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

    // Update is called once per frame
    void Update()
    {
		if (!GameObject.Find("MediumTankSP"))
			return;
		
		if (Random.Range(0, 1000) < 1)
		{

			Spawn_SP chosenSpawn = spawns[Random.Range(0, numberOfSpawns)];

			if (chosenSpawn.isOccupied)
				return;


			switch (Random.Range(0, 3))
			{
				case 0:
					Instantiate(Addressables.LoadAssetAsync<GameObject>("EnemyNormalTankPawnSP").WaitForCompletion(), chosenSpawn.transform.position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
				case 1:
					Instantiate(Addressables.LoadAssetAsync<GameObject>("EnemyDestroyerPawnSP").WaitForCompletion(), chosenSpawn.transform.position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
				case 2:
					Instantiate(Addressables.LoadAssetAsync<GameObject>("EnemyScoutPawnSP").WaitForCompletion(), chosenSpawn.transform.position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
			}
			
		}
	}
}
