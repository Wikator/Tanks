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
                    if (GameManager.Instance.gameMode == "Deathmatch")
                    {
                        GameMode.Instance.PointScored(player.color, 1);
                    }

                    other.GetComponent<Tank>().GameOver();
                    Despawn();
                }
            }
            else
            {
                if (other.GetComponent<Tank>().IsSpawned)
                {
                    if (GameManager.Instance.gameMode == "Deathmatch")
                    {
                        GameMode.Instance.PointScored(player.color, 1);
                    }

                    other.GetComponent<Tank>().GameOver();
                    Despawn();
                }
            }
        }

        if (other.CompareTag("Bullet") && other.GetComponent<Bullet>().player != player)
        {
            StartCoroutine(DespawnItself());
            StartCoroutine(other.GetComponent<Bullet>().DespawnItself());
        }

        if(other.CompareTag("Arena"))
        {
            StartCoroutine(DespawnItself());
        }
    }
}
