using System.Collections;
using UnityEngine;
using Graphics;

public abstract class Bullet_SP : MonoBehaviour
{
    public float moveSpeed;

    private bool isDespawning;

    protected Rigidbody rigidBody;

    protected Vector3 currentVelocity, currentPosition;

    [SerializeField, Tooltip("Unstoppable bullets are not destroyed when colliding with tanks or other bullets")]
    protected bool isUnstoppable;

    public int ChargeTimeToAdd { protected get; set; }


    public Collider owningCollider;



    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }

    private void OnDisable()
    {
        rigidBody.velocity = Vector3.zero;
    }

    protected virtual void OnEnable()
    {
        rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
        GetComponent<SphereCollider>().enabled = true;
        gameObject.GetComponent<TrailRenderer>().Clear();
        BulletGraphics.SetBulletLightIntensity(gameObject.GetComponent<Light>());
        isDespawning = false;
    }

    private void FixedUpdate()
    {
        currentVelocity = rigidBody.velocity;
        currentPosition = transform.position;

        if (isDespawning)
            BulletGraphics.DecreaseBulletLightIntensity(gameObject.GetComponent<Light>());
    }

    //Thanks to this method, bullet's trail will linger for a bit after the bullet despawns

    public IEnumerator DespawnItself()
    {
        isDespawning = true;
        rigidBody.velocity = Vector3.zero;
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.6f);
        gameObject.SetActive(false);
    }
}

