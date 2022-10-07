using FishNet.Object;
using FishNet.Object.Synchronizing;
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


    //Those methods need to be abstract, so that they can be called when referencing this class, rather than its subclasses

    public abstract void OnKilled(PlayerNetworking controllingLayer);

    public abstract Vector3 FindSpawnPosition(string color);

    public void PointScored(PlayerNetworking controllingPlayer, int numberOfPoints) => controllingPlayer.score += numberOfPoints;
}
