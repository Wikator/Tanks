using UnityEngine;
using UnityEngine.AddressableAssets;

public sealed class Player : MonoBehaviour
{
    public static Player Instance { get; private set; }


    public Tank_SP ControlledPawn { get; private set; }


    [HideInInspector]
    public string color;

    public string TankType { get; set; }

	
    public double superCharge;

    public int score = 0;



	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
        color = "None";
        TankType = "MediumTank";

        SpawnTank();
    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }


        if (ControlledPawn)
        {
            if (superCharge < ControlledPawn.stats.requiredSuperCharge)
            {
                superCharge += Time.deltaTime;
            }
            else
            {
                ControlledPawn.canUseSuper = true;
            }
        }
    }

    public void SpawnTank()
    {
        if (TankType == "None" || ControlledPawn)
            return;

        GameObject playerInstance = Instantiate(Addressables.LoadAssetAsync<GameObject>(TankType + "PawnSP").WaitForCompletion(), GameMode_SP.Instance.FindPlayerSpawn(), Quaternion.identity, transform);
        ControlledPawn = playerInstance.GetComponent <Tank_SP>();
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
