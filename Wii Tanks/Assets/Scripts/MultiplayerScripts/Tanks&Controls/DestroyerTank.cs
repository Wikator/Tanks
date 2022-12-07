using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class DestroyerTank : Tank
{
    private GameObject specialBullet;

    [Client]
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

        AmmoCount = 0;

        if (ammoCount <= 0)
            return;

        Vector3 position = bulletSpawn.position;
        Vector3 direction = bulletSpawn.forward;

        SpawnProjectile(position, direction, 0f);

        SuperServerFire(position, direction, TimeManager.Tick);

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        AmmoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));

        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }

    [ServerRpc]
    private void SuperServerFire(Vector3 position, Vector3 direction, uint tick)
    {
        //if (IsClient)
        //return;
        /*
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        AmmoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));
        */
        SuperObserversFire(position, direction, tick);
    }

    [ObserversRpc(IncludeOwner = false)]
    private void SuperObserversFire(Vector3 position, Vector3 direction, uint tick)
    {
        float passedTime = (float)TimeManager.TimePassed(tick, false);

        passedTime = Mathf.Min(MAX_PASSED_TIME, passedTime);

        SpawnSuperProjectile(position, direction, passedTime);

    }



    private void SpawnSuperProjectile(Vector3 position, Vector3 direction, float passedTime)
    {
        GameObject bulletInstance = ObjectPoolManager.GetPooledInstantiated(specialBullet, position, Quaternion.identity, bulletEmpty);
        bulletInstance.GetComponent<Bullet>().Initialize(direction, passedTime, controllingPlayer, stats.onKillSuperCharge, gameObject.GetComponent<BoxCollider>());


        ObjectPoolManager.GetPooledInstantiated(muzzleFlash, muzzleFlashSpawn.position, muzzleFlashSpawn.rotation, muzzleFlashEmpty);
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerBullet").WaitForCompletion();
        specialBullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerSpecialBullet").WaitForCompletion();
    }
}
