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
        GetComponent<SphereCollider>().enabled = true;
        transform.SetPositionAndRotation(bulletSpawn.position, bulletSpawn.rotation);
        transform.Rotate(new Vector3(0f, angle, 0f));
        rigidBody.velocity = transform.forward * moveSpeed;
        ClearTrail();

    }

    [ObserversRpc(RunLocally = true)]
    private void ClearTrail()
    {
        transform.GetChild(0).GetComponent<TrailRenderer>().Clear();
        Debug.LogWarning("Cleared");
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
}

