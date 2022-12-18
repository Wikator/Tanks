using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class EndlessSpawnManager_SP : SpawnManager_SP
{
	private readonly List<Spawn_SP> spawns = new();

	private int numberOfSpawns;

	private GameObject[] enemyTanks = new GameObject[3];

	void Start()
	{
		numberOfSpawns = 0;

		foreach (Transform spawn in GameObject.Find("DeathmatchSpawns").transform)
		{
			spawns.Add(spawn.GetComponent<Spawn_SP>());
			numberOfSpawns++;
		}

		enemyTanks[0] = Addressables.LoadAssetAsync<GameObject>("EnemyNormalTankPawnSP").WaitForCompletion();
		enemyTanks[1] = Addressables.LoadAssetAsync<GameObject>("EnemyDestroyerPawnSP").WaitForCompletion();
		enemyTanks[2] = Addressables.LoadAssetAsync<GameObject>("EnemyScoutPawnSP").WaitForCompletion();

		StartNewRound();
	}

	private void FixedUpdate()
	{

		if (!Player.Instance.ControlledPawn)
			return;

		if (Random.Range(0, 250) < 1)
		{
			ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 3)], FindEnemySpawn(), Quaternion.identity, GameObject.Find("Enemies").transform);
		}
	}

	public override Vector3 FindEnemySpawn()
	{
		Spawn_SP chosenSpawn = spawns[Random.Range(0, numberOfSpawns)];

		if (chosenSpawn.isOccupied)
		{
			return FindEnemySpawn();
		}

		return chosenSpawn.transform.position;
	}

	public override Vector3 FindPlayerSpawn()
	{
		Spawn_SP chosenSpawn = spawns[Random.Range(0, numberOfSpawns)];

		if (chosenSpawn.isOccupied)
		{
			return FindPlayerSpawn();
		}

		return chosenSpawn.transform.position;
	}

    public override void OnKilled(GameObject killedTank)
    {
		if (!GameManager_SP.Instance.GameInProgress)
			return;

		switch (killedTank.tag)
		{
			case "Tank":
				UIManager_SP.Instance.Show<GameOverView_SP>();
				GameManager_SP.Instance.EndGame();
				break;
			case "Enemy":
				Player.Instance.score += 1;
				MainView_SP.Instance.UpdateScore(Player.Instance.score);
				break;
		}
	}

    public override void StartNewRound()
    {
		Player.Instance.SpawnTank();

		ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 3)], FindEnemySpawn(), Quaternion.identity, GameObject.Find("Enemies").transform);
	}
}
