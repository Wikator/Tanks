using FishNet.Object;
using System.Collections;
using UnityEngine;

public class ScoutTank : Tank
{
    [SerializeField]
    private Stats fastModeStats;

    [SerializeField]
    private float fastModeDuration;

    [SerializeField]
    private int spreadAngle;


    [ServerRpc]
    protected override void Fire()
    {
        if (ammoCount <= 0)
            return;

        for (int angle = -spreadAngle; angle < spreadAngle*2; angle += spreadAngle)
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.transform.Rotate(new Vector3(0f, angle, 0f));
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            bulletInstance.GetComponent<Bullet>().ChargeTimeToAdd = stats.onKillSuperCharge;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
            Spawn(bulletInstance);


            NetworkObject flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
            flashInstance.transform.SetParent(muzzleFlashEmpty);
            flashInstance.transform.SetPositionAndRotation(muzzleFlashSpawn.position, muzzleFlashSpawn.rotation);
            flashInstance.transform.Rotate(new Vector3(0f, angle, 0f));
            Spawn(flashInstance);
        }


        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ammoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }

    [ServerRpc]
    protected override void SpecialMove()
    {
        if (!canUseSuper)
            return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        controllingPlayer.superCharge = 0;

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
