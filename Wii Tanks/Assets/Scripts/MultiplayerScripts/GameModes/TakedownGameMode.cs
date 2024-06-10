using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using UnityEngine;

public sealed class TakedownGameMode : EliminationGameMode
{
    [SerializeField] private float originalRespawnTime;

    [SerializeField] private float respawnTimeMultiplier;

    private readonly Dictionary<string, float> respawnTime = new();

    //Before start of the game, this script finds and saves all possible spawn points
    //There are different spawns for different teams, so they are seperated inside a dictionary

    public override void OnStartServer()
    {
        base.OnStartServer();

        spawns["Respawn"] = GameObject.Find("DeathmatchSpawns").GetComponentsInChildren<Spawn>();

        respawnTime["Green"] = originalRespawnTime;
        respawnTime["Red"] = originalRespawnTime;
    }

    // When all tanks in a team are down, the round ends, points are given, and a new round starts
    // Each time a tank tries to respawn, the time will increase for the next respawn for any tank in the team

    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {
        if (waitingForNewRound || !GameManager.Instance.GameInProgress)
            return;

        base.OnKilled(playerNetworking);

        StartCoroutine(Respawn(playerNetworking, respawnTime[playerNetworking.color]));
        respawnTime[playerNetworking.color] *= respawnTimeMultiplier;
    }


    // This recursive method tried to find an avaible spawn
    // If none are avaible, StackOverflowException is cought, so the tank needs to spawn in random spawn regardless if it's avaible or not
    // If this is a round start, a base function is returned, so that all tanks begin a game as if it was an elimination match
    // Else, any spawn on the arena will be found

    [Server]
    public override Vector3 FindSpawnPosition(string color)
    {
        if (waitingForNewRound) return base.FindSpawnPosition(color);

        color = "Respawn";

        Spawn spawn;

        var avaibleSpawns = spawns[color].Where(spawn => !spawn.isOccupied);

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

    // All respawn times need to be reset before the start of a new round

    [Server]
    protected override void StartNewRound(GameManager gameManager)
    {
        base.StartNewRound(gameManager);

        StopAllCoroutines();
        respawnTime["Green"] = originalRespawnTime;
        respawnTime["Red"] = originalRespawnTime;
    }

    private IEnumerator Respawn(PlayerNetworking controllingPLayer, float time)
    {
        yield return new WaitForSeconds(time);
        if (!waitingForNewRound && GameManager.Instance.GameInProgress)
            controllingPLayer.SpawnTank();
    }
}