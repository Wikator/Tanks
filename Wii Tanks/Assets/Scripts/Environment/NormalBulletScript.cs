using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public sealed class NormalBulletScript : Bullet
{
    [SerializeField]
    private int ricochetCount;

    [SerializeField]
    private bool isUnblockable;

    private bool canDamageSelf;

    private Vector3 currentVelocity, currentPosition;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        rigidBody.velocity = transform.forward * moveSpeed;
    }


    private void FixedUpdate()
    {
        currentVelocity = rigidBody.velocity;
        currentPosition = transform.position;
    }


    [Server(Logging = LoggingType.Off)]
    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Arena"))
        {
            ricochetCount--;
            if (ricochetCount < 0)
            {
                Despawn();
            }
            else
            {
                rigidBody.velocity = Vector3.Reflect(-collision.relativeVelocity, collision.contacts[0].normal).normalized * moveSpeed;
                transform.position = currentPosition;
                canDamageSelf = true;
            }
        }

        if (collision.gameObject.CompareTag("Tank"))
        {
            if (player != null)
            {
                if (player.controlledPawn != null)
                {
                    if (collision.gameObject != player.controlledPawn.gameObject)
                    {
                        if (GameManager.Instance.gameMode == "Deathmatch")
                        {
                            FindObjectOfType<GameMode>().PointScored(player.controlledPawn.controllingPlayer, 1);
                        }
                    }
                    else
                    {
                        if (!canDamageSelf)
                            return;
                    }
                }
            }

            collision.gameObject.GetComponent<Tank>().GameOver();

            if (!isUnblockable)
            {
                Despawn();
            }
            else
            {
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), collision.collider);
                rigidBody.velocity = currentVelocity;
                transform.position = currentPosition;
            }
        }

        if (collision.gameObject.CompareTag("Bullet"))
        {
            if (!isUnblockable)
            {
                StartCoroutine(DespawnItself());
            }
            else
            {
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), collision.collider);
                rigidBody.velocity = currentVelocity;
                transform.position = currentPosition;
            }
        }
    }
}
