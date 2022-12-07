using FishNet;
using FishNet.Managing.Logging;
using FishNet.Object;
using UnityEngine;

public sealed class ScoutBulletScript : Bullet
{
    //Bullet used by Scouts


    //This bullet cannot ricochet

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tank"))
        {
            if (player)
            {
                if (InstanceFinder.IsServer)
                {
                    if (other.GetComponent<Tank>().IsSpawned)
                    {
                        player.superCharge += ChargeTimeToAdd;

                        if (GameManager.Instance.gameMode == "Deathmatch" || GameManager.Instance.gameMode == "StockBattle" || GameManager.Instance.gameMode == "Mayhem")
                        {
                            GameMode.Instance.PointScored(player.color, 1);
                        }

                        other.GetComponent<Tank>().GameOver();
                    }
                }
            }
            else
            {
                if (InstanceFinder.IsServer)
                {
                    if (other.GetComponent<Tank>().IsSpawned)
                    {
                        other.GetComponent<Tank>().GameOver();
                    }
                }
            }

            if (!isUnstoppable)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), other);
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
            }
        }

        if(other.gameObject.CompareTag("Arena"))
        {
            StartCoroutine(DespawnItself());
        }
    }
}
