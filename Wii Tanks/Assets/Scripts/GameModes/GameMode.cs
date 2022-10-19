using FishNet;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameMode : NetworkBehaviour
{
    public static GameMode Instance { get; private set; }

    [SyncObject]
    public readonly SyncDictionary<string, Transform[]> spawns = new();

    [SyncObject]
    public readonly SyncDictionary<string, int> scores = new();

    private void Awake()
    {
        Instance = this;
    }

    /*public void LoadEndScene()
    {
        List<NetworkObject> movedObjects = new()
        {
            gameObject.GetComponent<NetworkObject>(),
        };

        LoadOptions loadOptions = new()
        {
            AutomaticallyUnload = true,
        };

        SceneLoadData sld = new("EndScreen")
        {
            MovedNetworkObjects = movedObjects.ToArray(),
            ReplaceScenes = ReplaceOption.All,
            Options = loadOptions
        };

        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }*/


    //Those methods need to be abstract, so that they can be called when referencing this class, rather than its subclasses

    public abstract void OnKilled(PlayerNetworking player);

    public abstract Vector3 FindSpawnPosition(string color);

    public void PointScored(string team, int numberOfPoints) => scores[team] += numberOfPoints;
}
