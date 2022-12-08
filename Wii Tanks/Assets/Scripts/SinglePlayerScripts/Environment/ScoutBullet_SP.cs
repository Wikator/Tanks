using UnityEngine;

public sealed class ScoutBullet_SP : Bullet_SP
{
    //Bullet used by Scouts


    //This bullet cannot ricochet

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Tank"))
        {
            if (other.GetComponent<Tank_SP>() != Player.Instance.ControlledPawn)
            {
                //Player.Instance.superCharge += ChargeTimeToAdd;


                other.GetComponent<Tank_SP>().GameOver();

                if (!isUnstoppable)
                {
					gameObject.SetActive(false);
				}
                else
                {
                    Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), other);
                    rigidBody.velocity = currentVelocity;
                    transform.position = currentPosition;
                }
            }
            else
            {
                other.GetComponent<Tank_SP>().GameOver();

                if (!isUnstoppable)
                {
					gameObject.SetActive(false);
				}
                else
                {
                    Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), other);
                    rigidBody.velocity = currentVelocity;
                    transform.position = currentPosition;
                }
            }
        }

        if (other.CompareTag("Bullet"))
        {
            if (other.gameObject.name == gameObject.name)
            {
				Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), other);
                return;
			}

		    StartCoroutine(other.GetComponent<Bullet_SP>().DespawnItself());

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

        if (other.CompareTag("Arena"))
        {
            StartCoroutine(DespawnItself());
        }
    }
}
