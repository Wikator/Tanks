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
}
