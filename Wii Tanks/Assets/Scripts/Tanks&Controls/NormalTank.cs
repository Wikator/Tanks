using FishNet.Object;
using UnityEngine;

public sealed class NormalTank : Tank
{
    private void Update()
    {
        if (!IsOwner)
            return;

        if (Input.GetMouseButtonDown(0) && ammoCount > 0)
            Fire();
    }

    [ServerRpc]
    private void Fire()
    {
        StopAllCoroutines();
        GameObject bulletInstance = Instantiate(bullet, bulletSpawn.position, bulletSpawn.rotation, bulletEmpty.transform);
        Spawn(bulletInstance);
        bulletInstance.GetComponent<BulletScript>().player = controllingPlayer;
        ammoCount--;
        StartCoroutine(AddAmmo(1.5f));
    }
}
