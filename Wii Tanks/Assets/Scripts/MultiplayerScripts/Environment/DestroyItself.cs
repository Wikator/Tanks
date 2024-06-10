using System.Collections;
using FishNet.Object;
using UnityEngine;

public sealed class DestroyItself : NetworkBehaviour
{
    [SerializeField] private float timeToDestroy;

    [SerializeField] private bool destroyOnSpawn;


    public override void OnStartServer()
    {
        base.OnStartServer();
        if (destroyOnSpawn)
            StartCoroutine(DespawnItselfDeleyed());
    }

    [Server]
    public IEnumerator DespawnItselfDeleyed()
    {
        yield return new WaitForSeconds(timeToDestroy);
        Despawn();
    }


    public void DespawnItself()
    {
        Despawn();
    }
}