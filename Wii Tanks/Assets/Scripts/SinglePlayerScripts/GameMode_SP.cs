using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class GameMode_SP : MonoBehaviour
{
    public static GameMode_SP Instance { get; private set; }





    public readonly List<EnemyAI> enemyTeam = new();


    private Transform bulletEmpty;
    private int enemySpawnCount;



	//Before start of the game, this script finds and saves all possible spawn points
	//There are different spawns for different teams, so they are seperated inside a dictionary


	private void Awake()
	{
        Instance = this;
	}

	public void Start()
    {

        bulletEmpty = GameObject.Find("Bullets").transform;

        Transform enemiesSpawnsParent = GameObject.Find("EnemySpawns").transform;

        enemySpawnCount = enemiesSpawnsParent.childCount;
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    public void OnKilled(bool isPlayer)
    {
        if (!GameManager_SP.Instance.GameInProgress)
            return;

        switch (isPlayer)
        {
            case true:
				GameManager_SP.Instance.EndGame();
				break;
            case false:
                if (enemyTeam.Count == 0)
                {
                    Player.Instance.DespawnTank();

                    foreach (Transform child in bulletEmpty)
                    {
                        child.gameObject.SetActive(false);
                    }

                    CampaignModeManager_SP.Instance.NextMap();
                }
                break;
        }
    }




    public Vector3 FindPlayerSpawn()
    {
        return CampaignModeManager_SP.Instance.playerSpawn.transform.position;
    }

    public Vector3 FindEnemySpawn()
    {
        Spawn_SP chosenSpawn = CampaignModeManager_SP.Instance.enemySpawns[Random.Range(0, CampaignModeManager_SP.Instance.enemySpawns.Count)];

        if (chosenSpawn.isOccupied)
        {
            return FindEnemySpawn();
        }

        return chosenSpawn.transform.position;
    }


    public void StartNewRound()
    {
		Player.Instance.SpawnTank();

        for (int i = 0; i < Random.Range(5, 16); i++)
        {
            switch (Random.Range(0, 3))
            {
                case 0:
                    enemyTeam.Add(ObjectPoolManager_SP.GetPooledInstantiated(Addressables.LoadAssetAsync<GameObject>("EnemyNormalTankPawnSP").WaitForCompletion(), FindEnemySpawn(), Quaternion.identity, GameObject.Find("Enemies").transform).GetComponent<EnemyAI>());
                    break;
                case 1:
                    enemyTeam.Add(ObjectPoolManager_SP.GetPooledInstantiated(Addressables.LoadAssetAsync<GameObject>("EnemyDestroyerPawnSP").WaitForCompletion(), FindEnemySpawn(), Quaternion.identity, GameObject.Find("Enemies").transform).GetComponent<EnemyAI>());
                    break;
                case 2:
                    enemyTeam.Add(ObjectPoolManager_SP.GetPooledInstantiated(Addressables.LoadAssetAsync<GameObject>("EnemyScoutPawnSP").WaitForCompletion(), FindEnemySpawn(), Quaternion.identity, GameObject.Find("Enemies").transform).GetComponent<EnemyAI>());
                    break;
            }
        }
    }
}
