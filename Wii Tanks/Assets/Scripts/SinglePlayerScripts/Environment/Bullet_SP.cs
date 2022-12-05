using System.Collections;
using UnityEngine;

public abstract class Bullet_SP : MonoBehaviour
{
    [SerializeField]
    protected float moveSpeed;


    protected Rigidbody rigidBody;

    protected Vector3 currentVelocity, currentPosition;

    [SerializeField, Tooltip("Unstoppable bullets are not destroyed when colliding with tanks or other bullets")]
    protected bool isUnstoppable;

    public int ChargeTimeToAdd { protected get; set; }


    public Collider owningCollider;


    protected virtual void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        currentVelocity = rigidBody.velocity;
        currentPosition = transform.position;
    }

    //Thanks to this method, bullet's trail will linger for a bit after the bullet despawns

    public IEnumerator DespawnItself()
    {
        rigidBody.velocity = Vector3.zero;
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.6f);
        Destroy(gameObject);
    }
}

