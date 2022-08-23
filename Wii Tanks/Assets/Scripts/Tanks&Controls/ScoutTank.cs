using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ScoutTank : Tank
{
    [ServerRpc]
    protected override void Fire()
    {
        for (int i = -3; i < 4; i++)
        {
            StopAllCoroutines();
            Debug.Log(bullet);
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.transform.Rotate(0.0f, i * 15, 0.0f, Space.Self);
            bulletInstance.transform.position += transform.forward * 3;
            Spawn(bulletInstance);
            bulletInstance.GetComponent<BulletScript>().player = controllingPlayer;
        }
        ammoCount--;
        StartCoroutine(AddAmmo(timeToReload, timeToAddAmmo));
    }

    [ServerRpc]
    protected override void SpecialMove()
    {
        return;
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "MediumTankBullet").WaitForCompletion();
    }
}
