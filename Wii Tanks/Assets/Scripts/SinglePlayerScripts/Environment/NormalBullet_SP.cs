using SinglePlayerScripts.Environment;
using UnityEngine;

public sealed class NormalBullet_SP : Bullet_SP
{
    [SerializeField] private int ricochetCount;

    private int ricochetsLeft;

    //Bullet used by Medium Tanks and Destroyers


    protected override void OnEnable()
    {
        ricochetsLeft = ricochetCount;
        base.OnEnable();
    }


    //After hitting the wall, bullet will reflect its direction in relation to the wall, and also change its speed so that the impact will not slow it down
    //If a tank is hit, the bullet will destroy it and give a score to the bullet's owner, if the game mode is set to Deathmatch

    private void OnCollisionEnter(Collision collision)
    {
        if (isDespawning)
            return;


        if (collision.gameObject.CompareTag("Arena"))
        {
            ricochetsLeft--;
            if (ricochetsLeft < 0)
            {
                rigidBody.velocity = Vector3.zero;
                StartCoroutine(DespawnItself());
            }
            else
            {
                rigidBody.velocity = Vector3.zero;
                //  transform.position = currentPosition;
                //rigidBody.AddForce(Vector3.Reflect(-collision.relativeVelocity, collision.contacts[0].normal).normalized * moveSpeed, ForceMode.Impulse);
                rigidBody.velocity =
                    Vector3.Reflect(-collision.relativeVelocity, collision.contacts[0].normal).normalized * moveSpeed;

                if (owningCollider)
                    Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), owningCollider, false);
            }
        }

        if (collision.gameObject.CompareTag("Tank"))
        {
            if (collision.gameObject != Player.Instance.ControlledPawn.gameObject)
                Player.Instance.superCharge += ChargeTimeToAdd;

            collision.gameObject.GetComponent<Tank_SP>().GameOver();


            if (!isUnstoppable)
                gameObject.SetActive(false);
            else
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), collision.collider);
            // rigidBody.velocity = currentVelocity;
            // transform.position = currentPosition;
        }

        if (collision.gameObject.CompareTag("Bullet"))
        {
            if (!isUnstoppable)
                StartCoroutine(DespawnItself());
            else
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), collision.collider);
            //rigidBody.velocity = currentVelocity;
            //transform.position = currentPosition;
        }

        if (collision.gameObject.tag.Contains("Enemy"))
        {
            Player.Instance.superCharge += ChargeTimeToAdd;
            collision.gameObject.GetComponent<EnemyAI>().GameOver();
            gameObject.SetActive(false);
            //StartCoroutine(DespawnItself());
        }
    }
}