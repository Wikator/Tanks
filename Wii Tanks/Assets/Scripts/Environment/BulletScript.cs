using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public sealed class BulletScript : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed;

    [SerializeField]
    private int ricochetCount;

    [SerializeField]
    private bool isUnblockable;

    private bool canDamageSelf;

    [HideInInspector]
    public PlayerNetworking player;

    private Rigidbody rigidBody;

    private Vector3 currentVelocity;
    private Vector3 currentPosition;
    private Vector3 angularVelocity;

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        rigidBody.velocity = transform.forward * moveSpeed;
        canDamageSelf = false;
    }

    private void Update()
    {
        if (ricochetCount >= 0 || rigidBody.velocity == Vector3.zero)
            return;

        Despawn();
    }

    private void FixedUpdate()
    {
        currentVelocity = rigidBody.velocity;
        angularVelocity = rigidBody.angularVelocity;
        currentPosition = transform.position;
    }


    [Server]
    private IEnumerator DespawnItself()
    {
        rigidBody.velocity = Vector3.zero;
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.6f);
        Despawn();
    }

    [Server(Logging = LoggingType.Off)]
    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Arena"))
        {
            ricochetCount--;
            rigidBody.velocity = Vector3.Reflect(-collision.relativeVelocity, collision.contacts[0].normal).normalized * moveSpeed;
            canDamageSelf = true;
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
                            FindObjectOfType<GameMode>().PointScored(player.controlledPawn.controllingPlayer, 1);
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
                rigidBody.angularVelocity = angularVelocity;
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
                rigidBody.angularVelocity = angularVelocity;
                transform.position = currentPosition;
            }
        }
    }
}
