using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class NormalTank : Tank
{
    [Client]
    protected override void SpecialMove()
    {
        Debug.Log(1);

        if (!canUseSuper)
            return;

        Debug.Log(2);

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        controllingPlayer.superCharge = 0;

        AmmoCount = 0;
        StartCoroutine(Barrage());
    }


    private IEnumerator Barrage()
    {
        for (int i = 0; i < 20; i++)
        {
            SuperClientFire();

            yield return new WaitForSeconds(0.2f);
        }
        routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
    }

    private void SuperClientFire()
    {

        Vector3 position = bulletSpawn.position;
        Vector3 direction = bulletSpawn.forward;

        SpawnProjectile(position, direction, 0f);

        ServerFire(position, direction, TimeManager.Tick);
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "MediumTankBullet").WaitForCompletion();
    }
}
