using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameMode : NetworkBehaviour
{
    public static GameMode Instance { get; private set; }

    [SyncObject]
    public readonly SyncDictionary<string, Transform[]> spawns = new();

    private void Awake()
    {
        Instance = this;
    }

    public abstract void OnKilled(PlayerNetworking controllingLayer);

    public abstract Vector3 FindSpawnPosition(string color);

    public void PointScored(PlayerNetworking controllingPlayer, int numberOfPoints) => controllingPlayer.score += numberOfPoints;
}
