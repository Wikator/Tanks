using UnityEngine;

public abstract class SpawnManager_SP : MonoBehaviour
{
    public static SpawnManager_SP Instance { get; private set; }

    protected void Awake()
    {
        Instance = this;
    }

    public abstract Vector3 FindPlayerSpawn();

    public abstract Vector3 FindEnemySpawn();

    public abstract void OnKilled(GameObject killedTank);

    public abstract void StartNewRound();
}