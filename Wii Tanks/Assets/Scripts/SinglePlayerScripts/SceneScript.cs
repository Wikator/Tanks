using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SceneScript : MonoBehaviour
{
    private Transform[] spawns = new Transform[8];

    void Start()
    {
		int i = 0;
		
		foreach (Transform spawn in GameObject.Find("DeathmatchSpawns").transform)
		{
			spawns[i] = spawn;
			i++;
		}
	}

    // Update is called once per frame
    void Update()
    {
		if (!GameObject.Find("MediumTankSP"))
			return;
		
		if (Random.Range(0, 1000) < 1)
		{

			switch (Random.Range(0, 3))
			{
				case 0:
					Instantiate(Addressables.LoadAssetAsync<GameObject>("EnemyNormalTankPawnSP").WaitForCompletion(), spawns[Random.Range(0, 8)].position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
				case 1:
					Instantiate(Addressables.LoadAssetAsync<GameObject>("EnemyDestroyerPawnSP").WaitForCompletion(), spawns[Random.Range(0, 8)].position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
				case 2:
					Instantiate(Addressables.LoadAssetAsync<GameObject>("EnemyScoutPawnSP").WaitForCompletion(), spawns[Random.Range(0, 8)].position, Quaternion.identity, GameObject.Find("Enemies").transform);
					break;
			}
			
		}
	}
}
