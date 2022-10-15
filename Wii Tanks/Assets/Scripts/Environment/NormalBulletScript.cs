using FishNet.Connection;
using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public sealed class NormalBulletScript : Bullet
{
    [SerializeField]
    private int ricochetCount;

    [SerializeField, Tooltip("Unblockable bullets are used by Destroyers' special move, those overpenetrate targets")]
    private bool isUnblockable;

    private bool canDamageSelf;

    private Vector3 currentVelocity, currentPosition;

    //Bullet used by Medium Tanks and Destroyers


    //Bullet's stats are saved in the FixedUpdate, so that the bullet will not slow down after hitting the wall

    private void FixedUpdate()
    {
        currentVelocity = rigidBody.velocity;
        currentPosition = transform.position;
    }


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
                            GameMode.Instance.PointScored(player.color, 1);
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
