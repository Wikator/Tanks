using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

public class EliminationGameMode : GameMode
{
    //Each player will be added to the HashSet appropriote to their chosen team

    [SyncObject]
    public readonly SyncList<PlayerNetworking> greenTeam = new();

    [SyncObject]
    public readonly SyncList<PlayerNetworking> redTeam = new();

    [HideInInspector]
    public bool waitingForNewRound = true;

    private Transform bulletEmpty;

    [SyncVar, SerializeField]
    private int pointsToWin;


    //Before start of the game, this script finds and saves all possible spawn points
    //There are different spawns for different teams, so they are seperated inside a dictionary

    public override void OnStartServer()
    {
        base.OnStartServer();

        bulletEmpty = GameObject.Find("Bullets").transform;

        spawns["Green"] = GameObject.Find("GreenSpawns").GetComponentsInChildren<Spawn>();
        spawns["Red"] = GameObject.Find("RedSpawns").GetComponentsInChildren<Spawn>();

        scores["Green"] = 0;
        scores["Red"] = 0;
    }

    //When a team has no players left, the round ends, points are given, and a new round starts

    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {

        if (waitingForNewRound || !GameManager.Instance.GameInProgress)
            return;

        switch (playerNetworking.color)
        {
            case "Green":
                if (greenTeam.All(player => !player.ControlledPawn) && greenTeam.Count != 0)
                {
                    PointScored("Red", 1);

                    if (scores["Red"] == pointsToWin)
                    {
                        GameManager.Instance.EndGame();
                    }
                    else
                    {
                        StartCoroutine(NewRound());
                    }
                }
                break;
            case "Red":
                if (redTeam.All(player => !player.ControlledPawn) && redTeam.Count != 0)
                {
                    PointScored("Green", 1);

                    if (scores["Green"] == pointsToWin)
                    {
                        GameManager.Instance.EndGame();
                    }
                    else
                    {
                        StartCoroutine(NewRound());
                    }
                }
                break;
        }
    }


    [Server]
    public override Vector3 FindSpawnPosition(string color)
    {
        Spawn spawn;

        IEnumerable<Spawn> avaibleSpawns = spawns[color].Where(s => !s.isOccupied);

        int avaibleSpawnsCount = avaibleSpawns.Count();

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


    [Server]
    protected virtual void StartNewRound(GameManager gameManager)
    {
        foreach (PlayerNetworking player in gameManager.players)
        {
            player.SpawnTank();
        }

        if (FindObjectOfType<GameMode>().TryGetComponent(out EliminationGameMode eliminationGameMode))
        {
            eliminationGameMode.waitingForNewRound = false;
        }
    }



    //Each player and bullet needs to be destroyed before the new round

    [Server]
    private IEnumerator NewRound()
    {
        waitingForNewRound = true;

        yield return new WaitForSeconds(3.5f);

        GameManager.Instance.KillAllPlayers();

        foreach (Transform child in bulletEmpty)
        {
            if (child.GetComponent<Bullet>().IsSpawned)
                child.GetComponent<Bullet>().Despawn();
        }

        yield return new WaitForSeconds(3.0f);

        StartNewRound(GameManager.Instance);
    }
}
