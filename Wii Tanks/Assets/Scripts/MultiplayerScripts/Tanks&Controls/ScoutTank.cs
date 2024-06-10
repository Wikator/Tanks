using System.Collections;
using FishNet.Object;
using UnityEngine;

public class ScoutTank : Tank
{
    [SerializeField] private Stats fastModeStats;

    [SerializeField] private float fastModeDuration;

    [SerializeField] private int spreadAngle;


    // Scout's fire is very different than the other type's, so this method needs to be overriden

    [ServerRpc]
    protected override void Fire()
    {
        if (ammoCount <= 0 || !IsSpawned)
            return;

        for (var angle = -spreadAngle; angle < spreadAngle * 2; angle += spreadAngle)
        {
            var bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation,
                SceneManagerScript.BulletEmpty);
            bulletInstance.transform.Rotate(new Vector3(0f, angle, 0f));
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            bulletInstance.GetComponent<Bullet>().ChargeTimeToAdd = stats.onKillSuperCharge;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(),
                gameObject.GetComponent<BoxCollider>(), true);
            Spawn(bulletInstance);


            var flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
            flashInstance.transform.SetParent(SceneManagerScript.MuzzleFlashEmpty);
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
        if (!canUseSuper || !IsSpawned)
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
        var savedStats = stats;

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