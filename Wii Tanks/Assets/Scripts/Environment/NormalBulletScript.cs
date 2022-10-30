using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public sealed class NormalBulletScript : Bullet
{
    [SerializeField]
    private int ricochetCount;

    //Bullet used by Medium Tanks and Destroyers


    //After hitting the wall, bullet will reflect its direction in relation to the wall, and also change its speed so that the impact will not slow it down
    //If a tank is hit, the bullet will destroy it and give a score to the bullet's owner, if the game mode is set to Deathmatch

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
                rigidBody.velocity = currentVelocity;
                transform.position = currentPosition;
                rigidBody.velocity = Vector3.Reflect(-collision.relativeVelocity, collision.contacts[0].normal).normalized * moveSpeed;
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), player.controlledPawn.GetComponent<BoxCollider>(), false);
            }
        }

        if (collision.gameObject.CompareTag("Tank"))
        {
            if (player)
            {
                if (player.controlledPawn)
                {
                    if (collision.gameObject != player.controlledPawn.gameObject)
                    {
                        if (GameManager.Instance.gameMode == "Deathmatch")
                        {
                            GameMode.Instance.PointScored(player.color, 1);
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
