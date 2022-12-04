using UnityEngine;
using UnityEngine.AddressableAssets;

public class EnemyScoutTank : EnemyAI
{
	protected override void Fire()
	{
		for (int i = -3; i <= 3; i += 3)
		{
			GameObject bulletInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>("RedScoutBulletSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation);

			bulletInstance.transform.Rotate(0, i, 0);

			GameObject flashInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>("RedMuzzleFlashSP").WaitForCompletion(), turret.GetChild(0).GetChild(0).position, turret.GetChild(0).GetChild(0).rotation, GameObject.Find("MuzzleFlashes").transform);

			flashInstance.transform.Rotate(0, i, 0);
		}
	}
}
