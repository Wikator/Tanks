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

        NetworkObject bulletInstance = NetworkManager.GetPooledInstantiated(specialBullet, true);
        bulletInstance.transform.SetParent(bulletEmpty);
        bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
        Spawn(bulletInstance);
        bulletInstance.GetComponent<Bullet>().AfterSpawning(bulletSpawn, 0);

        NetworkObject flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
        flashInstance.transform.SetParent(muzzleFlashEmpty);
        flashInstance.transform.SetPositionAndRotation(muzzleFlashSpawn.position, muzzleFlashSpawn.rotation);
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
