using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class NormalTank_SP : Tank_SP
{
    protected override void SpecialMove()
    {
        if (!canUseSuper)
            return;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        Player.Instance.superCharge = 0;

        ammoCount = 0;
        StartCoroutine(Barrage());
    }

    private IEnumerator Barrage()
    {
        for (int i = 0; i < 20; i++)
        {

            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            Physics.IgnoreCollision(bulletInstance.GetComponent<SphereCollider>(), gameObject.GetComponent<BoxCollider>(), true);

            Instantiate(muzzleFlash, bulletSpawn.position, bulletSpawn.rotation, muzzleFlashEmpty);

            yield return new WaitForSeconds(0.2f);
        }
        routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));
    }

    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "MediumTankBulletSP").WaitForCompletion();
    }
}
