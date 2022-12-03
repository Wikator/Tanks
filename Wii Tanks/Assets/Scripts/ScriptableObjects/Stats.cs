using UnityEngine;


[CreateAssetMenu(fileName = "New Stats", menuName = "Stats")]
public class Stats : ScriptableObject
{
	public float moveSpeed;
	public float rotateSpeed;
	public float timeToReload;
	public float timeToAddAmmo;
	public int maxAmmo;
	public int requiredSuperCharge;
	public int onKillSuperCharge;
}
