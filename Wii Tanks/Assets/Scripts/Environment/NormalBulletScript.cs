using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public sealed class NormalBulletScript : Bullet
{
    [SerializeField]
    private int ricochetCount;

    private int ricochetsLeft;

    //Bullet used by Medium Tanks and Destroyers


    public override void OnSpawnServer(NetworkConnection connection)
    {
        base.OnSpawnServer(connection);
        ricochetsLeft = ricochetCount;
    }

    //After hitting the wall, bullet will reflect its direction in relation to the wall, and also change its speed so that the impact will not slow it down
    //If a tank is hit, the bullet will destroy it and give a score to the bullet's owner, if the game mode is set to Deathmatch

    [Server(Logging = LoggingType.Off)]
    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Arena"))
        {
            ricochetsLeft--;
            if (ricochetsLeft < 0)
            {
                rigidBody.velocity = Vector3.zero;
                Despawn();
            }
            else
            {
                rigidBody.velocity = currentVelocity;
                transform.position = currentPosition;
                rigidBody.velocity = Vector3.Reflect(-collision.relativeVelocity, collision.contacts[0].normal).normalized * moveSpeed;
                if (player.ControlledPawn)
                {
                    Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), player.ControlledPawn.GetComponent<BoxCollider>(), false);
                }
            }
        }

        if (collision.gameObject.CompareTag("Tank"))
        {
            if (player)
            {
                if (player.ControlledPawn)
                {
                    if (collision.gameObject != player.ControlledPawn.gameObject)
                    {
                        player.superCharge += chargeTimeToAdd;

                        if (GameManager.Instance.gameMode == "Deathmatch")
                        {
                            GameMode.Instance.PointScored(player.Color, 1);
                        }
                    }
                }
            }

            collision.gameObject.GetComponent<Tank>().GameOver();

            if (!isUnstoppable)
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
            if (!isUnstoppable)
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
