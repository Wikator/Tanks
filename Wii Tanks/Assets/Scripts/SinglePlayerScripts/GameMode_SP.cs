using System.Collections.Generic;
using UnityEngine;

public abstract class GameMode_SP : MonoBehaviour
{
    public static GameMode_SP Instance { get; private set; }
	

    public readonly Dictionary<string, Transform[]> spawns = new();


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

        spawns["Player"] = new Transform[1];
        spawns["Red"] = new Transform[enemySpawnCount];

        spawns["Player"][0] = GameObject.Find("EnemySpawns").transform;

        for (int i = 0; i < enemySpawnCount; i++)
        {
            spawns["Red"][i] = enemiesSpawnsParent.GetChild(i).transform;
        }
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    public void OnKilled(bool isPlayer)
    {

        if (!GameManager.Instance.GameInProgress)
            return;

        switch (isPlayer)
        {
            case true:
				GameManager_SP.Instance.EndGame();
				break;
            case false:
                if (enemyTeam.Count != 0)
                {
					GameManager_SP.Instance.EndGame();
				}
                break;
        }
    }


    //This recursive method tried to find an avaible spawn
    //If none are avaible, StackOverflowException is cought, so the tank needs to spawn in random spawn regardless if it's avaible or not

    public Vector3 FindSpawnPosition(bool isPlayer)
    {

        if (isPlayer)
        {
            return spawns["Player"][0].position;
        }
        else
        {
            Transform randomSpawn = spawns["Enemies"][Random.Range(0, enemySpawnCount)];
			try
			{
				return randomSpawn.position;
			}
			catch (System.StackOverflowException)
			{
				return randomSpawn.position;
			}
		}
    }


    protected void StartNewRound(GameManager_SP gameManager)
    {
		Player.Instance.SpawnTank();
    }
}
