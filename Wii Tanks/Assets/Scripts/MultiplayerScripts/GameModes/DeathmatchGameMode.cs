using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FishNet.Object.Synchronizing;
using FishNet.Object;

public class DeathmatchGameMode : GameMode
{
    [SyncObject]
    public readonly SyncTimer time = new();

    [SerializeField]
    private int matchLength;

    [SerializeField]
    private float respawnTime;


    // Before start of the game, this script finds and saves all possible spawn points

    public override void OnStartServer()
    {
        base.OnStartServer();

        time.StartTimer(matchLength, true);

        spawns["NoTeams"] = GameObject.Find("DeathmatchSpawns").GetComponentsInChildren<Spawn>();

        scores["Green"] = 0;
        scores["Red"] = 0;
        scores["Cyan"] = 0;
        scores["Purple"] = 0;
        scores["Yellow"] = 0;
        scores["Blue"] = 0;
    }


    private void Update()
    {
        if (GameManager.Instance.GameInProgress)
        {
            if (time.Remaining > 0)
            {
                time.Update(Time.deltaTime);
            }
            else
            {
                GameManager.Instance.EndGame();
            }
        }
    }

    [Server]
    public override void OnKilled(PlayerNetworking playerNetworking)
    {
        StartCoroutine(Respawn(playerNetworking, respawnTime));
        PointScored(playerNetworking.color, -1);
    }


    // Color variable is unnecessary here, but still needs to be here because it's used by the abstract method

    [Server]
    public override Vector3 FindSpawnPosition(string color)
    {
        color = "NoTeams";

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

    private IEnumerator Respawn(PlayerNetworking controllingPLayer, float time)
    {
        yield return new WaitForSeconds(time);
        controllingPLayer.SpawnTank();
    }
}
