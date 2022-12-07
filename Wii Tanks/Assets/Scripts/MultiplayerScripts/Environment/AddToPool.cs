using System.Collections;
using UnityEngine;

public class AddToPool : MonoBehaviour
{
    [SerializeField]
    private float timeToDestroy;

    [SerializeField]
    private bool destroyOnSpawn;


    private void OnEnable()
    {
        if (destroyOnSpawn)
            StartCoroutine(DespawnItselfDeleyed());
    }

    public IEnumerator DespawnItselfDeleyed()
    {
        yield return new WaitForSeconds(timeToDestroy);
        gameObject.SetActive(false);
    }


    public void DespawnItself() => gameObject.SetActive(false);
}
