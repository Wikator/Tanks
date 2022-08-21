using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public sealed class BulletScript : NetworkBehaviour
{
    public float moveSpeed;

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
        StartCoroutine(TurnOnCollider());
    }

    private void Update()
    {
        if (ricochetCount >= 0 || rigidBody.velocity == Vector3.zero)
            return;

        Despawn();
    }

    [Server]
    private IEnumerator TurnOnCollider()
    {
        yield return new WaitForSeconds(0.07f);
        gameObject.GetComponent<SphereCollider>().enabled = true;
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
                }
            }

            collision.gameObject.GetComponent<Tank>().GameOver();
            Despawn();
        }

        if (collision.gameObject.CompareTag("Bullet"))
        {
            StartCoroutine(DespawnItself());
        }
    }
}
