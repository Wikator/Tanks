using FishNet;
using System.Collections.Generic;
using UnityEngine;

public class ArenaSelectionScene : MonoBehaviour
{
    [SerializeField]
    private GameObject[] allArenasArray = new GameObject[5];

    [SerializeField]
	[ColorUsage(hdr: true, showAlpha: true)]
    private Color[] backgroundColors = new Color[5];

    [SerializeField]
    [ColorUsage(hdr: true, showAlpha: true)]
    private Color oldColor;

    private readonly Dictionary<GameObject, Vector3> allArenasDictionary = new();

    private bool rotating;

    private readonly Vector3[] arenaPositions = new Vector3[7];
	

    [SerializeField]
    private float rotateSpeed;

    [SerializeField]
    private bool testLocally;

    private Renderer backgroundRenderer;

    private float lerpValue;



	private void Start()
    {
        rotating = false;

        arenaPositions[0] = new Vector3(-345, -105, 210);
        arenaPositions[1] = new Vector3(-230, -70, 140);
        arenaPositions[2] = new Vector3(-115, -35, 70);
        arenaPositions[3] = Vector3.zero;
        arenaPositions[4] = new Vector3(115, -35, 70);
        arenaPositions[5] = new Vector3(230, -70, 140);
        arenaPositions[6] = new Vector3(345, -105, 210);

        for (int i = 0; i < 5; i++)
        {
            allArenasDictionary[allArenasArray[i]] = allArenasArray[i].transform.position;
        }

		backgroundRenderer = GameObject.Find("Plane").GetComponent<Renderer>();
	}



    private void Update()
    {
        if (rotating)
            return;


        if (Input.GetKeyDown(KeyCode.A))
        {
            rotating = true;

            lerpValue = 1 / 56f;

            oldColor = backgroundRenderer.material.color;

			for (int i = 0; i < 6; i++)
            {
                foreach (GameObject arena in allArenasArray)
                {
                    if (arena.transform.position == arenaPositions[i])
                    {
                        allArenasDictionary[arena] = arenaPositions[i+1];
                        break;
                    }
                }
            }

            foreach (GameObject arena in allArenasArray)
            {
                if (arena.transform.position == arenaPositions[5])
                {

                    arena.transform.position = arenaPositions[0];
                    allArenasDictionary[arena] = arenaPositions[1];
                    break;
                }
            }

            foreach (GameObject arena in allArenasArray)
            {
                if (arena.transform.position == arenaPositions[6])
                {

                    arena.transform.position = arenaPositions[1];
                    allArenasDictionary[arena] = arenaPositions[2];
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            rotating = true;

            lerpValue = 0f;

			oldColor = backgroundRenderer.material.color;

			for (int i = 6; i > 0; i--)
            {
                foreach (GameObject arena in allArenasArray)
                {
                    if (arena.transform.position == arenaPositions[i])
                    {
                        allArenasDictionary[arena] = arenaPositions[i-1];
                        break;
                    }
                }
            }

            foreach (GameObject arena in allArenasArray)
            {
                if (arena.transform.position == arenaPositions[1])
                {

                    arena.transform.position = arenaPositions[6];
                    allArenasDictionary[arena] = arenaPositions[5];
                    break;
                }
            }

            foreach (GameObject arena in allArenasArray)
            {
                if (arena.transform.position == arenaPositions[0])
                {

                    arena.transform.position = arenaPositions[5];
                    allArenasDictionary[arena] = arenaPositions[4];
                    break;
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            FindObjectOfType<DefaultScene>().SetOnlineScene(ChosenArena());

            if (testLocally)
            {
                InstanceFinder.ServerManager.StartConnection();
                InstanceFinder.ClientManager.StartConnection();
            }
            else
            {
                FindObjectOfType<SteamLobby>().HostLobby();
            }
		}
	}


    private void FixedUpdate()
    {
        if (rotating)
        {
            MoveAllArenas();

			// Slowly change back ground color to the color in array of same index as an arena that has a value in dictionary as vector3.zero
			for (int i = 0; i < 5; i++)
			{
				if (allArenasDictionary[allArenasArray[i]] == Vector3.zero)
				{
					backgroundRenderer.material.color = Color.Lerp(oldColor, backgroundColors[i], lerpValue);
					lerpValue += 1 / 56f;
					break;
				}
			}
		}
	}



	
	private void MoveAllArenas()
	{
		foreach (GameObject arena in allArenasArray)
		{
			arena.transform.position = Vector3.MoveTowards(arena.transform.position, allArenasDictionary[arena], rotateSpeed);

			if (arena.transform.position == allArenasDictionary[arena])
			{
				arena.transform.position = allArenasDictionary[arena];

				rotating = false;
			}
		}
	}


	private string ChosenArena()
	{
		foreach (GameObject arena in allArenasArray)
		{
			if (arena.transform.position == Vector3.zero)
			{
				return arena.name;
			}
		}

		return null;
	}
}
