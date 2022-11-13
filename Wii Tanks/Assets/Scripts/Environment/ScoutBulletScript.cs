using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public sealed class ScoutBulletScript : Bullet
{
    //Bullet used by Scouts


    //This bullet cannot ricochet

    [Server(Logging = LoggingType.Off)]
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tank"))
        {
            if (player)
            {
                if (other.GetComponent<Tank>().controllingPlayer != player && other.GetComponent<Tank>().IsSpawned)
                {
                    player.superCharge += chargeTimeToAdd;

                    if (GameManager.Instance.gameMode == "Deathmatch")
                    {
                        GameMode.Instance.PointScored(player.Color, 1);
                    }

                    other.GetComponent<Tank>().GameOver();

                    if (!isUnstoppable)
                    {
                        Despawn();
                    }
                    else
                    {
                        Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), other);
                        rigidBody.velocity = currentVelocity;
                        transform.position = currentPosition;
                    }
                }
            }
            else
            {
                if (other.GetComponent<Tank>().IsSpawned)
                {
                    other.GetComponent<Tank>().GameOver();

                    if (!isUnstoppable)
                    {
                        Despawn();
                    }
                    else
                    {
                        Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), other);
                        rigidBody.velocity = currentVelocity;
                        transform.position = currentPosition;
                    }
                }
            }
        }

        if (other.CompareTag("Bullet") && other.GetComponent<Bullet>().player != player)
        {
            StartCoroutine(other.GetComponent<Bullet>().DespawnItself());

            if (!isUnstoppable)
            {
                StartCoroutine(DespawnItself());
            }
            else
            {
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), other);
                rigidBody.velocity = currentVelocity;
                transform.position = currentPosition;
            }
        }

        if(other.CompareTag("Arena"))
        {
            StartCoroutine(DespawnItself());
        }
    }
}
