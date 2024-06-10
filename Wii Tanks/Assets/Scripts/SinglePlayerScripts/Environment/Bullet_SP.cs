using System.Collections;
using Graphics;
using UnityEngine;

namespace SinglePlayerScripts.Environment
{
    public abstract class Bullet_SP : MonoBehaviour
    {
        public float moveSpeed;

        [SerializeField] [Tooltip("Unstoppable bullets are not destroyed when colliding with tanks or other bullets")]
        protected bool isUnstoppable;


        public Collider owningCollider;

        protected bool isDespawning;

        protected Rigidbody rigidBody;

        public int ChargeTimeToAdd { protected get; set; }


        private void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if (isDespawning)
            {
                BulletGraphics.DecreaseBulletLightIntensity(gameObject.GetComponent<Light>());
            }
        }

        protected virtual void OnEnable()
        {
            rigidBody.velocity = Vector3.zero;
            gameObject.GetComponent<TrailRenderer>().Clear();
            rigidBody.AddForce(transform.forward * moveSpeed, ForceMode.Impulse);
            GetComponent<SphereCollider>().enabled = true;
            BulletGraphics.SetBulletLightIntensity(gameObject.GetComponent<Light>());
            isDespawning = false;
        }

        private void OnDisable()
        {
            rigidBody.velocity = Vector3.zero;

            if (owningCollider)
                Physics.IgnoreCollision(gameObject.GetComponent<SphereCollider>(), owningCollider, false);
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
}