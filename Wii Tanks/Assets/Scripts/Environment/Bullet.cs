using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Object.Synchronizing;
using System.Collections;
using UnityEngine;

public abstract class Bullet : NetworkBehaviour
{
    public struct ReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;
        public ReconcileData(Vector3 position, Quaternion rotation, Vector3 velocity, Vector3 angularVelocity)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            AngularVelocity = angularVelocity;
        }
    }

    [SerializeField]
    public float moveSpeed;

    [HideInInspector]
    public PlayerNetworking player;

    public Rigidbody rigidBody;

    protected Vector3 currentVelocity, currentPosition;

    [SerializeField, Tooltip("Unstoppable bullets are not destroyed when colliding with tanks or other bullets")]
    protected bool isUnstoppable;

    //Once spawn, bullet will be given force, and cannot be slown down by any means, unless destroyed


    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>();
    }



    [Server]
    public void AfterSpawning(Transform bulletSpawn, int angle)
    {
        transform.SetPositionAndRotation(bulletSpawn.position, bulletSpawn.rotation);
        transform.Rotate(new Vector3(0f, angle, 0f));
        transform.GetChild(0).GetComponent<TrailRenderer>().Clear();
        ClearTrail(bulletSpawn, angle);
        rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
    }

    [ObserversRpc(RunLocally = true)]
    private void ClearTrail(Transform bulletSpawn, int angle)
    {
        Debug.LogWarning("Cleared");
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
            rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
        }
    }


    //Bullet's stats are saved in the FixedUpdate, so that the bullet will not slow down after hitting the wall

    private void FixedUpdate()
    {
        currentVelocity = rigidBody.velocity;
        currentPosition = transform.position;
    }

    [Server]
    public IEnumerator DespawnItself()
    {
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
        AddForce();
        transform.GetChild(0).GetComponent<TrailRenderer>().emitting = true;
        transform.GetChild(0).GetComponent<TrailRenderer>().Clear();
    }

    [Server]
    private void AddForce()
    {
        GetComponent<SphereCollider>().enabled = true;
        //rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
    }
}

