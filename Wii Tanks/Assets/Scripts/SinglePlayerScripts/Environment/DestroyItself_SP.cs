using System.Collections;
using UnityEngine;

public sealed class DestroyItself_SP : MonoBehaviour
{
    [SerializeField]
    private float timeToDestroy;

    [SerializeField]
    private bool destroyOnSpawn;


    private void Start()
    {
        if (destroyOnSpawn)
            StartCoroutine(DespawnItselfDeleyed());
    }

    public IEnumerator DespawnItselfDeleyed()
    {
        yield return new WaitForSeconds(timeToDestroy);
		Destroy(gameObject);
	}
}
