using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Graphics;
using UnityEngine;

public abstract class Bullet : NetworkBehaviour
{
    [SerializeField] [Tooltip("How fast this bullet will travel")]
    protected float moveSpeed;

    [SerializeField] [Tooltip("Unstoppable bullets are not destroyed when colliding with tanks or other bullets")]
    protected bool isUnstoppable;

    public PlayerNetworking player;

    private Light bulletLight;

    protected Vector3 currentVelocity, currentPosition;

    [SyncVar] private bool isDespawning;

    protected Rigidbody rigidBody;

    public int ChargeTimeToAdd { protected get; set; }


    //Bullet's stats are saved in the FixedUpdate, so that the bullet will not slow down after hitting the wall

    private void FixedUpdate()
    {
        if (IsServer && rigidBody)
        {
            currentVelocity = rigidBody.velocity;
            currentPosition = transform.position;
        }

        if (isDespawning && IsClient && bulletLight)
            BulletGraphics.DecreaseBulletLightIntensity(bulletLight);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        rigidBody = GetComponent<Rigidbody>();

        rigidBody = GetComponent<Rigidbody>();
        rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);

        isDespawning = false;
    }


    public override void OnStartClient()
    {
        base.OnStartClient();
        bulletLight = transform.GetChild(1).GetComponent<Light>();
        BulletGraphics.SetBulletLightIntensity(bulletLight);
    }


    [Server]
    public IEnumerator DespawnItself()
    {
        isDespawning = true;
        rigidBody.velocity = Vector3.zero;
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.6f);
        Despawn();
    }
}