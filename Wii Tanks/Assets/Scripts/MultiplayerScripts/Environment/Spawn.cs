using UnityEngine;

public class Spawn : MonoBehaviour
{
    public bool isOccupied;

    private Collider other;

    private void Update()
    {
        if (isOccupied && other == null) isOccupied = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (isOccupied)
        {
            isOccupied = false;
            this.other = null;
        }
    }


    //Each spawn checks whether anything is in its collider, so that nothing will be able to spawn inside different objects

    private void OnTriggerStay(Collider other)
    {
        isOccupied = true;
        this.other = other;
    }
}