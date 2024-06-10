using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public abstract class GameMode : NetworkBehaviour
{
    [SyncObject] public readonly SyncDictionary<string, int> scores = new();

    public readonly Dictionary<string, Spawn[]> spawns = new();
    public static GameMode Instance { get; private set; }

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
        if (!MainView.Instance || !EndScreen.Instance)
            return;

        if (op == SyncDictionaryOperation.Set)
        {
            MainView.Instance.UpdateScore(key, value);
            EndScreen.Instance.UpdateScores();
        }
    }


    //Those methods need to be abstract, so that they can be called when referencing this class, rather than its subclasses

    public abstract void OnKilled(PlayerNetworking player);

    public abstract Vector3 FindSpawnPosition(string color);

    public void PointScored(string team, int numberOfPoints)
    {
        scores[team] += numberOfPoints;
    }
}