using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class DestroyerTank : Tank
{
    private GameObject specialBullet;

    public override void OnStartServer()
    {
        base.OnStartServer();
        specialBullet = Addressables.LoadAssetAsync<GameObject>(controllingPlayer.color + "DestroyerSpecialBullet").WaitForCompletion();
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

        ammoCount = 0;

        GameObject bulletInstance = Instantiate(specialBullet, bulletSpawn.position, bulletSpawn.rotation, SceneManagerScript.BulletEmpty);
        bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
        Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);
        Spawn(bulletInstance);

        NetworkObject flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
        flashInstance.transform.SetParent(SceneManagerScript.MuzzleFlashEmpty);
        flashInstance.transform.SetPositionAndRotation(muzzleFlashSpawn.position, muzzleFlashSpawn.rotation);
        Spawn(flashInstance);

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }
}
