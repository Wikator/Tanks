using FishNet.Object;
using FishNet.Object.Synchronizing;
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
        scores.OnChange += OnScoreChange;
    }

    private void OnDestroy()
    {
        scores.OnChange -= OnScoreChange;
    }

    private void OnScoreChange(SyncDictionaryOperation op, string key, int value, bool asServer)
    {
        if (!MainView.Instance)
            return;

        if (op == SyncDictionaryOperation.Set)
        {
            MainView.Instance.UpdateScore(key, value);
        }
    }



    //Those methods need to be abstract, so that they can be called when referencing this class, rather than its subclasses

    public abstract void OnKilled(PlayerNetworking player);

    public abstract Vector3 FindSpawnPosition(string color);

    public void PointScored(string team, int numberOfPoints) => scores[team] += numberOfPoints;
}
