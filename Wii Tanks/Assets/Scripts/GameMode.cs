using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameMode : NetworkBehaviour
{
    [SyncObject]
    public readonly SyncDictionary<string, List<GameObject>> spawns = new();   

    public virtual void OnKilled(PlayerNetworking controllingLayer)
    {

    }

    public virtual GameObject FindSpawnPosition(string color)
    {
        return gameObject;
    }

    public void PointScored(PlayerNetworking controllingPlayer, int numberOfPoints) => controllingPlayer.score += numberOfPoints;
}
