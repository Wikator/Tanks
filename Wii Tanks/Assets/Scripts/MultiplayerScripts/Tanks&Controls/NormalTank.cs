using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class NormalTank : Tank
{
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

            NetworkObject flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
            flashInstance.transform.SetParent(muzzleFlashEmpty);
            flashInstance.transform.SetPositionAndRotation(muzzleFlashSpawn.position, muzzleFlashSpawn.rotation);
            Spawn(flashInstance);

            yield return new WaitForSeconds(0.2f);
        }

        if (IsSpawned)
        {
            routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
        }
    }
}
