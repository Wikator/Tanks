using UnityEngine;

[CreateAssetMenu(fileName = "New Stats", menuName = "Stats")]
public class Stats : ScriptableObject
{
    [Tooltip("How fast this tank will move")]
    public float moveSpeed;

    [Tooltip("How quickly this tank will rotate")]
    public float rotateSpeed;

    [Tooltip("How long this tank will need to wait to start reloading after the last shot")]
    public float timeToReload;

    [Tooltip("How quickly this tank will reload each bullet after the first one")]
    public float timeToAddAmmo;

    [Tooltip("Maximum ammo capacity")] public int maxAmmo;

    [Tooltip("Super charge this tank will need to activate its Super")]
    public int requiredSuperCharge;

    [Tooltip("How much charge this tank will get on each kill")]
    public int onKillSuperCharge;
}