using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public sealed class StockBattleGameMode : GameMode
{
    [HideInInspector] public static List<PlayerNetworking> defeatedPlayers = new();

    [SerializeField] private float respawnTime;

    [SerializeField] private int playerLife;

    [SyncObject] public readonly SyncDictionary<string, int> lifeRemaining = new();


    //Before start of the game, this script finds and saves all possible spawn points

    public override void OnStartServer()
    {
        base.OnStartServer();

        spawns["NoTeams"] = GameObject.Find("DeathmatchSpawns").GetComponentsInChildren<Spawn>();

        scores["Green"] = 0;
        scores["Red"] = 0;
        scores["Cyan"] = 0;
        scores["Purple"] = 0;
        scores["Yellow"] = 0;
        scores["Blue"] = 0;

        lifeRemaining["Green"] = playerLife;
        lifeRemaining["Red"] = playerLife;
        lifeRemaining["Cyan"] = playerLife;
        lifeRemaining["Purple"] = playerLife;
        lifeRemaining["Yellow"] = playerLife;
        lifeRemaining["Blue"] = playerLife;
    }

    // Tank will respawn only if it has lives remaining
    // If there is only one tank that still has lives, the game ends

    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {
        lifeRemaining[playerNetworking.color] -= 1;

        if (lifeRemaining[playerNetworking.color] == 0)
        {
            defeatedPlayers.Add(playerNetworking);

            if (defeatedPlayers.Count == GameManager.Instance.players.Count - 1) GameManager.Instance.EndGame();
        }
        else
        {
            StartCoroutine(Respawn(playerNetworking, respawnTime));
        }
    }


    //Color variable is unnecessary, but still needs to be here because it's used by the abstract method

    [Server]
    public override Vector3 FindSpawnPosition(string color)
    {
        color = "NoTeams";

        Spawn spawn;

        var avaibleSpawns = spawns[color].Where(s => !s.isOccupied);

        var avaibleSpawnsCount = avaibleSpawns.Count();

        if (avaibleSpawnsCount == 0)
        {
            spawn = spawns[color][Random.Range(0, spawns[color].Length)];
            spawn.isOccupied = true;
            return spawn.transform.position;
        }

        spawn = avaibleSpawns.ElementAt(Random.Range(0, avaibleSpawnsCount));
        spawn.isOccupied = true;
        return spawn.transform.position;
    }

    private IEnumerator Respawn(PlayerNetworking controllingPLayer, float time)
    {
        yield return new WaitForSeconds(time);
        controllingPLayer.SpawnTank();
    }
}