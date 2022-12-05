using UnityEngine;
using UnityEngine.AddressableAssets;

public class EnemyDestroyerTank : EnemyAI
{
	protected override void Fire()
	{
		GameObject bulletInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(color + "DestroyerBulletSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation, GameObject.Find("Bullets").transform);
		Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
		bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

		Instantiate(muzzleFlash, turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation, GameObject.Find("MuzzleFlashes").transform);
	}

	protected override void ChasePlayer()
	{
		
		if (!walkPointSet)
		{
			float randomZ = Random.Range(-5, 5);
			float randomX = Random.Range(-5, 5);

			walkPoint = new Vector3(target.transform.position.x + randomX, transform.position.y, target.transform.position.x + randomZ);
			walkPointSet = true;
			if (!Physics.Raycast(walkPoint + new Vector3(0, 15, 0), -transform.up, 25f, whatIsWall))
				if (Physics.Raycast(walkPoint + new Vector3(0, 15, 0), -transform.up, 25f, whatIsGround))
					walkPointSet = true;
		}

		if (agent.velocity.magnitude == 0)
		{
			float randomZ = Random.Range(-5, 5);
			float randomX = Random.Range(-5, 5);

			walkPoint = new Vector3(target.transform.position.x + randomX, transform.position.y, target.transform.position.x + randomZ);
			walkPointSet = true;
			if (!Physics.Raycast(walkPoint + new Vector3(0, 15, 0), -transform.up, 25f, whatIsWall))
				if (Physics.Raycast(walkPoint + new Vector3(0, 15, 0), -transform.up, 25f, whatIsGround))
					walkPointSet = true;
		}


		if (walkPointSet)
		{
			Debug.Log(walkPoint);
			agent.SetDestination(walkPoint);
		}

		transform.rotation = Quaternion.LookRotation(agent.velocity.normalized);

		Vector3 distanceToWalkPoint = transform.position - walkPoint;

		if (distanceToWalkPoint.magnitude < 1f)
			walkPointSet = false;
	}
}
