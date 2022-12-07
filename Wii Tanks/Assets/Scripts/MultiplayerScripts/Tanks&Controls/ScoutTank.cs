using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ScoutTank : Tank
{
    [SerializeField]
    private Stats fastModeStats;

    [SerializeField]
    private float fastModeDuration;

    [SerializeField]
    private int spreadAngle;


    protected override void SpawnProjectile(Vector3 position, Vector3 direction, float passedTime)
    {
        for (int angle = -spreadAngle; angle < spreadAngle * 2; angle += spreadAngle)
        {
            GameObject bulletInstance = ObjectPoolManager.GetPooledInstantiated(bullet, position, Quaternion.identity, bulletEmpty);
            bulletInstance.GetComponent<Bullet>().Initialize(Quaternion.Euler(0, angle, 0) * direction, passedTime, controllingPlayer, stats.onKillSuperCharge, gameObject.GetComponent<BoxCollider>());

            ObjectPoolManager.GetPooledInstantiated(muzzleFlash, muzzleFlashSpawn.position, muzzleFlashSpawn.rotation, muzzleFlashEmpty);
        }
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

            AmmoCount = stats.maxAmmo;
        }
    }
     
    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "ScoutBullet").WaitForCompletion();
    }
}
