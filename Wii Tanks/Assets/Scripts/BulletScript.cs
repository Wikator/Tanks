using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public sealed class BulletScript : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private int ricochetCount;

    [HideInInspector]
    public PlayerNetworking player;

    private Rigidbody rigidBody;


    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.velocity = transform.forward * moveSpeed;
    }

    void Update()
    {
        if (ricochetCount > 0)
            return;

        Despawn();
    }

    [Server(Logging = LoggingType.Off)]
    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Arena"))
        {
            ricochetCount--;
            rigidBody.velocity = Vector3.Reflect(-collision.relativeVelocity, collision.contacts[0].normal).normalized * moveSpeed;
        }

        if (collision.gameObject.CompareTag("Tank"))
        {
            if (player != null)
            {
                if (collision.gameObject != player.controlledPawn.gameObject)
                {
                    player.PointScored(1);
                }
            }

            collision.gameObject.GetComponent<Tank>().GameOver();
            Despawn();
        }

        if (collision.gameObject.CompareTag("Bullet"))
        {
            Despawn();
        }
    }
}
