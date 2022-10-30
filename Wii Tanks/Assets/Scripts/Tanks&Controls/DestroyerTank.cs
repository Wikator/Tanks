using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class DestroyerTank : Tank
{
    private GameObject specialBullet;

    [ServerRpc]
    protected override void SpecialMove()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        ammoCount = 0;

        GameObject bulletInstance = Instantiate(specialBullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
        Spawn(bulletInstance);

        GameObject flashInstance = Instantiate(muzzleFlash, muzzleFlashEmpty.position, muzzleFlashEmpty.rotation, muzzleFlashEmpty);
        Spawn(flashInstance);

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerBullet").WaitForCompletion();
        specialBullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerSpecialBullet").WaitForCompletion();
    }
}
