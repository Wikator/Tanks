using UnityEngine;
using UnityEngine.AddressableAssets;
using ObjectPoolManager;

public sealed class DestroyerTank_SP : Tank_SP
{
    private GameObject specialBullet;

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

        GameObject bulletInstance = ObjectPoolManager.ObjectPoolManager.GetPooledInstantiated(specialBullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);

        ObjectPoolManager.ObjectPoolManager.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation, muzzleFlashEmpty);

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerBulletSP").WaitForCompletion();
        specialBullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerSpecialBulletSP").WaitForCompletion();
    }
}
