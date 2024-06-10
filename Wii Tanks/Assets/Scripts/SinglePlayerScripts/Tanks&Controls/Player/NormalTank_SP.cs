using System.Collections;
using ObjectPoolManager;
using UnityEngine;

public sealed class NormalTank_SP : Tank_SP
{
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
        canUseSuper = false;

        ammoCount = 0;
        StartCoroutine(Barrage());
    }

    private IEnumerator Barrage()
    {
        for (var i = 0; i < 20; i++)
        {
            var bulletInstance =
                ObjectPoolManager_SP.GetPooledInstantiated(bullet, bulletSpawn.position, bulletSpawn.rotation,
                    bulletEmpty);
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(),
                gameObject.GetComponent<BoxCollider>(), true);

            ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation,
                muzzleFlashEmpty);

            yield return new WaitForSeconds(0.2f);
        }

        routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
    }
}