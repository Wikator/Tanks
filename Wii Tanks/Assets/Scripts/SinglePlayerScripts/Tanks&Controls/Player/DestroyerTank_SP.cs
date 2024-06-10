using ObjectPoolManager;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class DestroyerTank_SP : Tank_SP
{
    private GameObject specialBullet;

    private void Awake()
    {
        specialBullet = Addressables.LoadAssetAsync<GameObject>(Player.Instance.color + "DestroyerSpecialBulletSP")
            .WaitForCompletion();
    }

    protected override void SpecialMove()
    {
        if (!canUseSuper)
            return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        Player.Instance.superCharge = 0;

        ammoCount = 0;

        var bulletInstance = ObjectPoolManager_SP.GetPooledInstantiated(specialBullet, bulletSpawn.position,
            bulletSpawn.rotation, bulletEmpty);
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(),
            true);

        ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation,
            muzzleFlashEmpty);

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }
}