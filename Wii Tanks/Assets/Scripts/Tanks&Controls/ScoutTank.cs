using FishNet.Object;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ScoutTank : Tank
{
    [System.Serializable]
    private struct FastModeStats
    {
        public float time;
        public float moveSpeed;
        public float rotateSpeed;
        public float timeToReload;
        public float timeToAddAmmo;
        public int maxAmmo;
    }

    [SerializeField]
    private FastModeStats fastModeStats;

    [ServerRpc]
    protected override void Fire()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        for (int i = -3; i < 6; i+=3)
        {
            GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty);
            bulletInstance.transform.Rotate(new Vector3(0.0f, i, 0.0f));
            Spawn(bulletInstance);
            bulletInstance.GetComponent<ScoutBulletScript>().player = controllingPlayer;
        }
        ammoCount--;
        routine = StartCoroutine(AddAmmo(stats.timeToReload));
    }

    [ServerRpc]
    protected override void SpecialMove()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        StartCoroutine(FastMode());
    }

    private IEnumerator FastMode()
    {
        TankStats savedStats = stats;

        stats.moveSpeed = fastModeStats.moveSpeed;
        stats.rotateSpeed = fastModeStats.rotateSpeed;
        stats.timeToReload = fastModeStats.timeToReload;
        stats.timeToAddAmmo = fastModeStats.timeToAddAmmo;
        stats.maxAmmo = fastModeStats.maxAmmo;

        routine = StartCoroutine(AddAmmo(stats.timeToAddAmmo));

        yield return new WaitForSeconds(fastModeStats.time);

        stats.moveSpeed = savedStats.moveSpeed;
        stats.rotateSpeed = savedStats.rotateSpeed;
        stats.timeToReload = savedStats.timeToReload;
        stats.timeToAddAmmo = savedStats.timeToAddAmmo;
        stats.maxAmmo = savedStats.maxAmmo;

        if (ammoCount >= stats.maxAmmo)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }

            ammoCount = stats.maxAmmo;
        }
    }
     
    public override void ChangeColours(string color)
    {
        base.ChangeColours(color);
        bullet = Addressables.LoadAssetAsync<GameObject>(color + "ScoutBullet").WaitForCompletion();
    }
}
