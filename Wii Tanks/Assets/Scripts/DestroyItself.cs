using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public sealed class DestroyItself : NetworkBehaviour
{
    [SerializeField]
    private float timeToDestroy;

    [SerializeField]
    private bool destroyOnSpawn;


    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        if (destroyOnSpawn) StartCoroutine(DespawnItselfDeleyed());
    }

    public IEnumerator DespawnItselfDeleyed()
    {
        yield return new WaitForSeconds(timeToDestroy);
        Despawn();
    }


    public void DespawnItself() => Despawn();
}
