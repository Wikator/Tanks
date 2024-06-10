using ObjectPoolManager;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class BigTeamBattleSpawnManager_SP : SpawnManager_SP
{
    public int color;

    private readonly GameObject[] enemyTanks = new GameObject[3];

    private readonly int spawnDistance = 9;
    private Spawn[] spawns;
    private readonly int tankAmount = 100;

    private void Start()
    {
        spawns = GameObject.Find("DeathmatchSpawns").GetComponentsInChildren<Spawn>();

        enemyTanks[0] = Addressables.LoadAssetAsync<GameObject>("EnemyNormalTankPawnSP").WaitForCompletion();
        enemyTanks[1] = Addressables.LoadAssetAsync<GameObject>("EnemyDestroyerPawnSP").WaitForCompletion();
        enemyTanks[2] = Addressables.LoadAssetAsync<GameObject>("EnemyScoutPawnSP").WaitForCompletion();

        StartNewRound();
    }

    public override Vector3 FindEnemySpawn()
    {
        Spawn spawn;

        var offset = new Vector3(Random.Range(-spawnDistance, spawnDistance), 0,
            Random.Range(-spawnDistance, spawnDistance));

        spawn = spawns[color];
        spawn.isOccupied = true;
        return spawn.transform.position + offset;
    }

    public override Vector3 FindPlayerSpawn()
    {
        Spawn spawn;

        var offset = new Vector3(Random.Range(-spawnDistance, spawnDistance), 0,
            Random.Range(-spawnDistance, spawnDistance));

        spawn = spawns[color];
        return spawn.transform.position + offset;
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
        color = 0;
        //Player.Instance.SpawnTank();
        for (var i = 0; i < tankAmount; i++)
            ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
                Quaternion.identity, GameObject.Find("Enemies").transform);
        color = 1;
        for (var i = 0; i < tankAmount; i++)
            ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
                Quaternion.identity, GameObject.Find("Enemies").transform);
        color = 2;
        for (var i = 0; i < tankAmount; i++)
            ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
                Quaternion.identity, GameObject.Find("Enemies").transform);
        color = 3;
        for (var i = 0; i < tankAmount; i++)
            ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
                Quaternion.identity, GameObject.Find("Enemies").transform);
        color = 4;
        for (var i = 0; i < tankAmount; i++)
            ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
                Quaternion.identity, GameObject.Find("Enemies").transform);
        color = 5;
        for (var i = 0; i < tankAmount; i++)
            ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
                Quaternion.identity, GameObject.Find("Enemies").transform);
    }
}