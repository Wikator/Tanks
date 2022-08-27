using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ScoutTank : Tank
{
    [ServerRpc]
    protected override void Fire()
    {
        StopAllCoroutines();
        for (int i = -3; i < 6; i+=3)
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.transform.Rotate(new Vector3(0.0f, i, 0.0f));
            Spawn(bulletInstance);
            bulletInstance.GetComponent<ScoutBulletScript>().player = controllingPlayer;
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
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "ScoutBullet").WaitForCompletion();
    }
}
