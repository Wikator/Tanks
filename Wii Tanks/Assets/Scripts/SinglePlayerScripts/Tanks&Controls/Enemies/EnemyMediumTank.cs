using System.Collections.Generic;
using UnityEngine;
using ObjectPoolManager;
using Graphics;

public class EnemyMediumTank : EnemyAI
{
	protected override void OnEnable()
	{
		base.OnEnable();

		graphics = new TankGraphics(
			color,
			gameObject.GetComponent<Light>(),
			transform.GetChild(0).gameObject.GetComponent<MeshRenderer>(),
			turret.GetChild(0).gameObject.GetComponent<MeshRenderer>(),
			transform.GetChild(0).GetChild(1).gameObject.GetComponent<MeshRenderer>(),
			transform.GetChild(0).GetChild(2).gameObject.GetComponent<MeshRenderer>()
			);

		Dictionary<string, GameObject> prefabs = TankGraphics.ChangePrefabsColours(color, "Singleplayer", "MediumTank");

		explosion = prefabs["Explosion"];
		muzzleFlash = prefabs["MuzzleFlash"];
		bullet = prefabs["Bullet"];
	}
	protected override void Fire()
	{
        GameObject bulletInstance = ObjectPoolManager_SP.GetPooledInstantiated(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
		Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
		bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

        ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation, muzzleFlashEmpty);
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


		if (agent.velocity.normalized != Vector3.zero)
		{
			transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);
		}
	}
}
