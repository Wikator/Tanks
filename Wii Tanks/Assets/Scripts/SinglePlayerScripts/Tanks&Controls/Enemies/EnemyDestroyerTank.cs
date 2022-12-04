using UnityEngine;
using UnityEngine.AddressableAssets;

public class EnemyDestroyerTank : EnemyAI
{
	protected override void Fire()
	{
		Instantiate(Addressables.LoadAssetAsync<GameObject>("RedDestroyerBulletSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation);

		Instantiate(Addressables.LoadAssetAsync<GameObject>("RedMuzzleFlashSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation, GameObject.Find("MuzzleFlashes").transform);
	}
}
