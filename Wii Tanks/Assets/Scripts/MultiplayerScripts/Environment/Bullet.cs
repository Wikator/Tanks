using FishNet.Object;
using System.Collections;
using UnityEngine;

public abstract class Bullet : MonoBehaviour
{
    [SerializeField]
    protected float moveSpeed;

    [HideInInspector]
    public PlayerNetworking player;

    //protected Rigidbody rigidBody;

    [SerializeField, Tooltip("Unstoppable bullets are not destroyed when colliding with tanks or other bullets")]
    protected bool isUnstoppable;

    public float ChargeTimeToAdd { protected get; set; }

    protected Vector3 direction;

    private float passedTime = 0f;

    public void Initialize(Vector3 direction, float passedTime, PlayerNetworking player = null, float chargeTimeToAdd = 0f, Collider colliderToIgnore = null)
    {
        this.direction = direction;
        this.passedTime = passedTime;
        this.player = player;
        ChargeTimeToAdd = chargeTimeToAdd;
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), colliderToIgnore, true);
        GetComponent<SphereCollider>().enabled = true;
    }

    private void Update()
    {
        float passedTimeDelta = 0f;

        if (passedTime > 0f)
        {
            float step = (passedTime * 0.08f);
            passedTime -= step;

            if (passedTime <= (Time.deltaTime / 2f))
            {
                step += passedTime;
                passedTime = 0f;
            }
            passedTimeDelta = step;
        }

        transform.position += (Time.deltaTime + passedTimeDelta) * moveSpeed * direction;
    }


    public IEnumerator DespawnItself()
    {
        direction = Vector3.zero;
        GetComponent<SphereCollider>().enabled = false;
        yield return new WaitForSeconds(0.6f);
        gameObject.SetActive(false);
    }
}

