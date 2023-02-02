using UnityEngine;
using ObjectPoolManager;
using Graphics;

public class EnemyScoutTank : EnemyAI
{
    protected override void OnEnable()
    {
        base.OnEnable();

		TankGet tankGet = new()
		{
			tankBody = transform.GetChild(0).gameObject,
			turretBody = turret.GetChild(0).gameObject,
			mainBody = gameObject,
			color = color
		};

		TankSet tankSet = TankGraphics.ChangeTankColours(tankGet, "Singleplayer");

		tankMaterial = tankSet.tankMaterial;
		turretMaterial = tankSet.turretMaterial;
		explosion = tankSet.explosion;
		muzzleFlash = tankSet.muzzleFlash;
		bullet = TankGraphics.ChangeBulletColour(color, "Scout", "Singleplayer");
	}


    protected override void Fire()
	{
		for (int i = -3; i <= 3; i += 3)
		{
            GameObject bulletInstance = ObjectPoolManager_SP.GetPooledInstantiated(bullet, bulletSpawn.position, bulletSpawn.rotation * Quaternion.Euler(Vector3.up * i), bulletEmpty);

			Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
			bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

            GameObject flashInstance = ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation * Quaternion.Euler(Vector3.up * i), muzzleFlashEmpty);
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
