using UnityEngine;
using UnityEngine.AddressableAssets;
using ObjectPoolManager;

public class EnemyScoutTank : EnemyAI
{
	protected override void Fire()
	{
		for (int i = -3; i <= 3; i += 3)
		{
			GameObject bulletInstance = ObjectPoolManager_SP.GetPooledInstantiated(Addressables.LoadAssetAsync<GameObject>(color + "ScoutBulletSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation * Quaternion.Euler(Vector3.up * i), GameObject.Find("Bullets").transform);

			Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
			bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

			GameObject flashInstance = ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation * Quaternion.Euler(Vector3.up * i), GameObject.Find("MuzzleFlashes").transform);
		}
	}


	protected override void ChasePlayer()
	{
		Vector3 distanceToWalkPoint = transform.position - walkPoint;

		if (!walkPointSet || agent.velocity.magnitude == 0 || distanceToWalkPoint.magnitude < 1f)
		{
			float randomZ = Random.Range(-2f, 2f);
			float randomX = Random.Range(-2f, 2f);

			randomZ = Mathf.Max(randomZ, Mathf.Sign(randomZ) * 0.25f);
			randomX = Mathf.Max(randomX, Mathf.Sign(randomX) * 0.25f);

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
