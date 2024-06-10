using System.Collections;
using FishNet.Object;
using UnityEngine;

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
        for (var i = 0; i < 20; i++)
        {
            var bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation,
                SceneManagerScript.BulletEmpty);
            bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(),
                gameObject.GetComponent<BoxCollider>(), true);
            Spawn(bulletInstance);

            var flashInstance = NetworkManager.GetPooledInstantiated(muzzleFlash, true);
            flashInstance.transform.SetParent(SceneManagerScript.MuzzleFlashEmpty);
            flashInstance.transform.SetPositionAndRotation(muzzleFlashSpawn.position, muzzleFlashSpawn.rotation);
            Spawn(flashInstance);

            yield return new WaitForSeconds(0.2f);
        }

        if (IsSpawned) routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
    }
}