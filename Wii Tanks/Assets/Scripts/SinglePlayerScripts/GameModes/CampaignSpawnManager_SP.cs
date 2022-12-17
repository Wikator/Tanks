using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class CampaignSpawnManager_SP : SpawnManager_SP
{
    public readonly List<EnemyAI> enemyTeam = new();

    private Transform bulletEmpty;


	public void Start()
    {
        bulletEmpty = GameObject.Find("Bullets").transform;
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    public override void OnKilled(GameObject killedTank)
    {
        if (!GameManager_SP.Instance.GameInProgress)
            return;

        switch (killedTank.tag)
        {
            case "Tank":
                //UIManager_SP.Instance.Show<GameOverView_SP>();
				//GameManager_SP.Instance.EndGame();
				break;
            case "Enemy":
                enemyTeam.Remove(killedTank.GetComponent<EnemyAI>());
                if (enemyTeam.Count == 0)
                {
                    Player.Instance.DespawnTank();

                    foreach (Transform child in bulletEmpty)
                    {
                        child.gameObject.SetActive(false);
                    }

                    CampaignModeManager_SP.NextMap();
                }
                break;
        }
    }




    public override Vector3 FindPlayerSpawn()
    {
        return CampaignModeManager_SP.playerSpawn.transform.position;
    }

    public override Vector3 FindEnemySpawn()
    {
        Spawn_SP chosenSpawn = CampaignModeManager_SP.enemySpawns[Random.Range(0, CampaignModeManager_SP.enemySpawns.Count)];

        if (chosenSpawn.isOccupied)
        {
            return FindEnemySpawn();
        }

        return chosenSpawn.transform.position;
    }


    public override void StartNewRound()
    {
		Player.Instance.SpawnTank();

        for (int i = 0; i < Random.Range(3, 8); i++)
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
