using System.Collections;
using UnityEngine;
using ObjectPoolManager;

public class ScoutTank_SP : Tank_SP
{
    [SerializeField]
    private Stats fastModeStats;

    [SerializeField]
    private float fastModeDuration;

    [SerializeField]
    private int spreadAngle;


    protected override void Fire()
    {
        if (ammoCount <= 0)
            return;

        for (int angle = -spreadAngle; angle < spreadAngle * 2; angle += spreadAngle)
        {
            GameObject bulletInstance = ObjectPoolManager_SP.GetPooledInstantiated(bullet, bulletSpawn.position, bulletSpawn.rotation * Quaternion.Euler(Vector3.up * angle), bulletEmpty);
            bulletInstance.GetComponent<Bullet_SP>().ChargeTimeToAdd = stats.onKillSuperCharge;
			Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);

            ObjectPoolManager_SP.GetPooledInstantiated(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation * Quaternion.Euler(Vector3.up * angle), muzzleFlashEmpty);
		}


        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ammoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));
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

        StartCoroutine(FastMode());
    }

    private IEnumerator FastMode()
    {
        Stats savedStats = stats;

        stats = fastModeStats;

        routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));

        yield return new WaitForSeconds(fastModeDuration);

        stats = savedStats;

        if (ammoCount >= stats.maxAmmo)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            ammoCount = stats.maxAmmo;
        }
    }
}
