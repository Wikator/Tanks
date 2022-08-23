using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class NormalTank : Tank
{
    [ServerRpc]
    protected override void SpecialMove()
    {
        StopAllCoroutines();
        ammoCount = 0;
        StartCoroutine(Barrage());
    }

    [Server]
    private IEnumerator Barrage()
    {
        for (int i = 0; i < 25; i++)
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            Spawn(bulletInstance);
            bulletInstance.GetComponent<BulletScript>().player = controllingPlayer;
            yield return new WaitForSeconds(0.1f);
        }
        StartCoroutine(AddAmmo(timeToReload, timeToAddAmmo));
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "MediumTankBullet").WaitForCompletion();
    }
}
