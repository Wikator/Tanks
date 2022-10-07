using FishNet.Connection;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public abstract class Bullet : NetworkBehaviour
{
    [SerializeField]
    protected float moveSpeed;

    [HideInInspector]
    public PlayerNetworking player;

    protected Rigidbody rigidBody;


    //Once spawn, bullet will be given force, and cannot be slown down by any means, unless destroyed

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        rigidBody.velocity = transform.forward * moveSpeed;
    }

    [Server]
    public IEnumerator DespawnItself()
    {
        rigidBody.velocity = Vector3.zero;
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.6f);
        Despawn();
    }
}

