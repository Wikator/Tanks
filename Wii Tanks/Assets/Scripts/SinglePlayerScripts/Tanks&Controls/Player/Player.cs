using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }


    public static Tank ControlledPawn;


	[HideInInspector]
    public string color;

    public string TankType { get; set; }

	
    public double superCharge;



	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
        color = "None";
        TankType = "None";
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void SpawnTank()
    {
        if (TankType == "None")
            return;

        GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(TankType + "PawnSP").WaitForCompletion(), GameMode.Instance.FindSpawnPosition(color), Quaternion.identity, transform);
        ControlledPawn = playerInstance.GetComponent<Tank>();
        playerInstance.transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled = false;
        playerInstance.transform.GetChild(0).GetChild(0).transform.GetChild(0).gameObject.GetComponent<MeshRenderer>().enabled = false;
    }


    //Method for when a tank needs to be killed in order to start a new round

    public void DespawnTank()
    {
        if (ControlledPawn)
        {
            ControlledPawn.GameOver();
        }
    }
}
