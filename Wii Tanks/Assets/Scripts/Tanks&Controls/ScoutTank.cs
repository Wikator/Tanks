using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ScoutTank : Tank
{
    [SerializeField]
    private TankStats fastModeStats;

    [SerializeField]
    private float fastModeDuration;

    [SerializeField]
    private int spreadAngle;

    [ServerRpc]
    protected override void Fire()
    {
        for (int angle = -spreadAngle; angle < spreadAngle*2; angle += spreadAngle)
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.transform.Rotate(new Vector3(0.0f, angle, 0.0f));
            bulletInstance.GetComponent<ScoutBulletScript>().player = controllingPlayer;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
            Spawn(bulletInstance);

            GameObject flashInstance = Instantiate(muzzleFlash, muzzleFlashEmpty.position, muzzleFlashEmpty.rotation, muzzleFlashEmpty);
            flashInstance.transform.Rotate(new Vector3(0.0f, angle, 0.0f));
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
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        StartCoroutine(FastMode());
    }

    private IEnumerator FastMode()
    {
        TankStats savedStats = stats;

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
     
    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "ScoutBullet").WaitForCompletion();
    }
}
