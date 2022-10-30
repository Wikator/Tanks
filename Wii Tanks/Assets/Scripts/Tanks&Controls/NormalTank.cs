using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class NormalTank : Tank
{
    [ServerRpc]
    protected override void SpecialMove()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ammoCount = 0;
        StartCoroutine(Barrage());
    }

    [Server]
    private IEnumerator Barrage()
    {
        for (int i = 0; i < 20; i++)
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);

            Spawn(bulletInstance);

            GameObject flashInstance = Instantiate(muzzleFlash, muzzleFlashEmpty.position, muzzleFlashEmpty.rotation, muzzleFlashEmpty);
            Spawn(flashInstance);

            yield return new WaitForSeconds(0.2f);
        }
        routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "MediumTankBullet").WaitForCompletion();
    }
}
