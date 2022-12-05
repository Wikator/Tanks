using UnityEngine;
using UnityEngine.AddressableAssets;

public class EnemyScoutTank : EnemyAI
{
	protected override void Fire()
	{
		for (int i = -3; i <= 3; i += 3)
		{
			GameObject bulletInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(color + "ScoutBulletSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation, GameObject.Find("Bullets").transform);

			bulletInstance.transform.Rotate(0, i, 0);

			Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
			bulletInstance.GetComponent<Bullet_SP>().owningCollider = gameObject.GetComponent<BoxCollider>();

			GameObject flashInstance = Instantiate(muzzleFlash, turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation, GameObject.Find("MuzzleFlashes").transform);

			flashInstance.transform.Rotate(0, i, 0);
		}
	}
}
