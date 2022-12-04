using UnityEngine;
using UnityEngine.AddressableAssets;

public class EnemyMediumTank : EnemyAI
{
	protected override void Fire()
	{
		Instantiate(Addressables.LoadAssetAsync<GameObject>("RedMediumTankBulletSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation);

		Instantiate(Addressables.LoadAssetAsync<GameObject>("RedMuzzleFlashSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation, GameObject.Find("MuzzleFlashes").transform);
	}
}
