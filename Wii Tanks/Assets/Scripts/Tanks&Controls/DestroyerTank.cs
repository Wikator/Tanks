using FishNet.Object;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class DestroyerTank : Tank
{
    [SerializeField]
    private GameObject specialBullet;

    [ServerRpc]
    protected override void SpecialMove()
    {
        StopAllCoroutines();
        ammoCount = 0;
        GameObject bulletInstance = Instantiate(specialBullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
        Spawn(bulletInstance);
        bulletInstance.GetComponent<Bullet>().player = controllingPlayer;
        StartCoroutine(AddAmmo(timeToReload, timeToAddAmmo));
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerBullet").WaitForCompletion();
        specialBullet = Addressables.LoadAssetAsync<GameObject>(color + "DestroyerSpecialBullet").WaitForCompletion();
    }
}
