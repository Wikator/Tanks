using UnityEngine;
using UnityEngine.AddressableAssets;
using ObjectPoolManager;

public class EnemyMediumTank : EnemyAI
{
    protected override void OnEnable()
    {
		base.OnEnable();
		normalBullet = Addressables.LoadAssetAsync<GameObject>(color + "MediumTankBulletSP").WaitForCompletion();
	}

    protected override void Fire()
	{
        GameObject bulletInstance = ObjectPoolManager.ObjectPoolManager.GetPooledInstantiated(normalBullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
		Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
		bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

        ObjectPoolManager.ObjectPoolManager.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation, muzzleFlashEmpty);
	}

	protected override void ChasePlayer()
	{

		Vector3 distanceToWalkPoint = transform.position - walkPoint;

		if (!walkPointSet || agent.velocity.magnitude == 0 || distanceToWalkPoint.magnitude < 1f)
		{
			float randomZ = Random.Range(-5f, 5f);
			float randomX = Random.Range(-5f, 5f);

			randomZ = Mathf.Max(randomZ, Mathf.Sign(randomZ) * 0.75f);
			randomX = Mathf.Max(randomX, Mathf.Sign(randomX) * 0.75f);

			walkPoint = new Vector3(target.transform.position.x + randomX, transform.position.y, target.transform.position.z + randomZ);
			walkPointSet = true;
			if (!Physics.Raycast(walkPoint + new Vector3(0, 15, 0), -transform.up, 25f, whatIsWall))
				if (Physics.Raycast(walkPoint + new Vector3(0, 15, 0), -transform.up, 25f, whatIsGround))
					walkPointSet = true;
		}


		if (walkPointSet)
		{
			agent.SetDestination(walkPoint);
		}

		transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
	}
}
