using FishNet;
using UnityEngine;

public sealed class NormalBulletScript : Bullet
{
    [SerializeField]
    private int ricochetCount;

    public int ricochetsLeft;

    //Bullet used by Medium Tanks and Destroyers



    private void OnEnable()
    {
        ricochetsLeft = ricochetCount;
    }


    //After hitting the wall, bullet will reflect its direction in relation to the wall, and also change its speed so that the impact will not slow it down
    //If a tank is hit, the bullet will destroy it and give a score to the bullet's owner, if the game mode is set to Deathmatch

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Arena"))
        {
            ricochetsLeft--;
            if (ricochetsLeft < 0)
            {
                StartCoroutine(DespawnItself());
            }
            else
            {
                direction = Vector3.Reflect(direction, collision.contacts[0].normal).normalized ;
                if (player.ControlledPawn)
                {
                    Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), player.ControlledPawn.GetComponent<BoxCollider>(), false);
                }
            }
        }

        if (collision.gameObject.CompareTag("Tank"))
        {

            if (InstanceFinder.IsServer)
            {
                if (player)
                {
                    if (player.ControlledPawn)
                    {
                        if (collision.gameObject != player.ControlledPawn.gameObject)
                        {
                            player.superCharge += ChargeTimeToAdd;

                            if (GameManager.Instance.gameMode == "Deathmatch" || GameManager.Instance.gameMode == "StockBattle" || GameManager.Instance.gameMode == "Mayhem")
                            {
                                GameMode.Instance.PointScored(player.color, 1);
                            }
                        }
                    }
                }

                collision.gameObject.GetComponent<Tank>().GameOver();
            }

            if (!isUnstoppable)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), collision.collider);
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
            }
        }
    }


}
