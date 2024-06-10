using System.Collections;
using System.Linq;
using ObjectPoolManager;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class EndlessSpawnManager_SP : SpawnManager_SP
{
    private readonly GameObject[] enemyTanks = new GameObject[3];
    private Spawn[] spawns;

    private void Start()
    {
        spawns = GameObject.Find("DeathmatchSpawns").GetComponentsInChildren<Spawn>();

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
            ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
                Quaternion.identity, GameObject.Find("Enemies").transform);
    }

    public override Vector3 FindEnemySpawn()
    {
        Spawn spawn;

        var avaibleSpawns = spawns.Where(s => !s.isOccupied);

        var avaibleSpawnsCount = avaibleSpawns.Count();

        if (avaibleSpawnsCount == 0)
        {
            spawn = spawns[Random.Range(0, spawns.Length)];
            spawn.isOccupied = true;
            return spawn.transform.position;
        }

        spawn = avaibleSpawns.ElementAt(Random.Range(0, avaibleSpawnsCount));
        spawn.isOccupied = true;
        return spawn.transform.position;
    }

    public override Vector3 FindPlayerSpawn()
    {
        Spawn spawn;

        var avaibleSpawns = spawns.Where(s => !s.isOccupied);

        var avaibleSpawnsCount = avaibleSpawns.Count();

        if (avaibleSpawnsCount == 0)
        {
            spawn = spawns[Random.Range(0, spawns.Length)];
            spawn.isOccupied = true;
            return spawn.transform.position;
        }

        spawn = avaibleSpawns.ElementAt(Random.Range(0, avaibleSpawnsCount));
        spawn.isOccupied = true;
        return spawn.transform.position;
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
        StartCoroutine(SpawnPlayerAndEnemy());
    }

    private IEnumerator SpawnPlayerAndEnemy()
    {
        Player.Instance.SpawnTank();

        yield return new WaitForEndOfFrame();

        ObjectPoolManager_SP.GetPooledInstantiated(enemyTanks[Random.Range(0, 2)], FindEnemySpawn(),
            Quaternion.identity, GameObject.Find("Enemies").transform);
    }
}