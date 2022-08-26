using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public sealed class ScoutBulletScript : NetworkBehaviour
{
    [SerializeField]
    private float moveSpeed;

    [HideInInspector]
    public PlayerNetworking player;

    private Rigidbody rigidBody;


    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        rigidBody.velocity = transform.forward * moveSpeed;
    }

    /*private void Update()
    {
        if (rigidBody.velocity == Vector3.zero)
            return;

        Debug.LogWarning("Despawn");

        Despawn();
    }*/



    [Server]
    private IEnumerator DespawnItself()
    {
        rigidBody.velocity = Vector3.zero;
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.6f);
        Despawn();
    }

    [Server(Logging = LoggingType.Off)]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tank"))
        {
            if (player != null)
            {
                if (player.controlledPawn != null)
                {
                    if (GameManager.Instance.gameMode == "Deathmatch")
                        FindObjectOfType<GameMode>().PointScored(player.controlledPawn.controllingPlayer, 1);
                }
            }

            other.gameObject.GetComponent<Tank>().GameOver();

            Despawn();
        }

        if ((other.CompareTag("Bullet") && other.GetComponent<ScoutBulletScript>().player != player) || other.CompareTag("Arena"))
        {
            StartCoroutine(DespawnItself());
        }
    }
}
