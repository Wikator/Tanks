using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public abstract class Bullet : NetworkBehaviour
{
    [SerializeField]
    protected float moveSpeed;

    [HideInInspector]
    public PlayerNetworking player;

    protected Rigidbody rigidBody;

    protected Vector3 currentVelocity, currentPosition;

    [SerializeField, Tooltip("Unstoppable bullets are not destroyed when colliding with tanks or other bullets")]
    protected bool isUnstoppable;

    [SyncVar]
    private bool isDespawning = false;

    public int ChargeTimeToAdd { protected get; set; }


    private const float LIGHT_INTENSITY = 0.5f;

    //Once spawn, bullet will be given force, and cannot be slown down by any means, unless destroyed

    [Server]
    public void AfterSpawning(Transform bulletSpawn, int angle)
    {
        rigidBody = GetComponent<Rigidbody>();
        transform.SetPositionAndRotation(bulletSpawn.position, bulletSpawn.rotation);
        transform.Rotate(new Vector3(0f, angle, 0f));
        transform.GetChild(0).GetComponent<TrailRenderer>().Clear();
        rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        
        if (gameObject.GetComponent<NetworkObject>().GetDefaultDespawnType() == DespawnType.Pool)
        {
            StartCoroutine(TrailRendererMethod());
        }
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (gameObject.GetComponent<NetworkObject>().GetDefaultDespawnType() == DespawnType.Destroy)
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
        }

        isDespawning = false;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        transform.GetChild(1).GetComponent<HDAdditionalLightData>().intensity = LIGHT_INTENSITY;
    }


    //Bullet's stats are saved in the FixedUpdate, so that the bullet will not slow down after hitting the wall

    //[Server(Logging = FishNet.Managing.Logging.LoggingType.Off)]
    private void FixedUpdate()
    {
        if (IsServer)
        {
            currentVelocity = rigidBody.velocity;
            currentPosition = transform.position;
        }

        if (isDespawning)
            TurnOffLight();
    }

    // Thanks to this method, bullet's trail will linger for a bit after the bullet despawns

    [Client]
    private void TurnOffLight()
    {
        transform.GetChild(1).GetComponent<HDAdditionalLightData>().intensity -= 0.015f;
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


    private IEnumerator TrailRendererMethod()
    {
        transform.GetChild(0).GetComponent<TrailRenderer>().emitting = false;
        for (int i = 0; i < 4; i++)
        {
            transform.GetChild(0).GetComponent<TrailRenderer>().Clear();
            yield return new WaitForFixedUpdate();
        }
        GetComponent<SphereCollider>().enabled = true;
        transform.GetChild(0).GetComponent<TrailRenderer>().emitting = true;
        transform.GetChild(0).GetComponent<TrailRenderer>().Clear();
    }
}

