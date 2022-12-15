using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampaignModeManager_SP : MonoBehaviour
{
    public static CampaignModeManager_SP Instance { get; private set; }

    [SerializeField]
    private GameObject[] allArenasArray = new GameObject[5];

    [HideInInspector]
    public List<Spawn_SP> enemySpawns = new();

    [HideInInspector]
    public Spawn_SP playerSpawn;

    [SerializeField]
    private  List <GameObject> allCurrentArenas = new();

    [SerializeField]
    [ColorUsage(hdr: true, showAlpha: true)]
    private Color[] backgroundColors = new Color[5];
    

    [ColorUsage(hdr: true, showAlpha: true)]
    private  Color oldColor;

    [ColorUsage(hdr: true, showAlpha: true)]
    private Color newColor;

    private readonly Dictionary<GameObject, Vector3> allArenasDictionary = new();

    private bool rotating;
    
    private readonly Vector3[] arenaPositions = new Vector3[7];


    private Renderer backgroundRenderer;

    private float lerpValue;

    private const float ROTATE_SPEED = 2.5f;

    private void Awake()
    {
        rotating = false;

        arenaPositions[0] = new Vector3(-345, -105, 210);
        arenaPositions[1] = new Vector3(-230, -70, 140);
        arenaPositions[2] = new Vector3(-115, -35, 70);
        arenaPositions[3] = Vector3.zero;
        arenaPositions[4] = new Vector3(115, -35, 70);
        arenaPositions[5] = new Vector3(230, -70, 140);
        arenaPositions[6] = new Vector3(345, -105, 210);

        backgroundRenderer = GameObject.Find("Plane").GetComponent<Renderer>();
        //allCurrentArenas.Add(null);
        Instance = this;
    }

    private void Start()
    {

    }

    private void FixedUpdate()
    {
        if (rotating)
        {
            MoveAllArenas();

            backgroundRenderer.material.color = Color.Lerp(oldColor, newColor, lerpValue);
            lerpValue += 1 / 56f;
        }
    }

    private void MoveAllArenas()
    {
        foreach (GameObject arena in allCurrentArenas)
        {
            if (arena)
            {
                arena.transform.position = Vector3.MoveTowards(arena.transform.position, allArenasDictionary[arena], ROTATE_SPEED);

                if (arena.transform.position == allArenasDictionary[arena])
                {
                    arena.transform.position = allArenasDictionary[arena];

                    if (arena.transform.position == arenaPositions[1])
                    {
                        allCurrentArenas.Remove(arena);
                        Destroy(arena);
                        rotating = false;
                        StartGame();
                        break;
                    }
                }
            }
        }
    }


    public void UpdateSpawns(Transform spawnParent)
    {
        enemySpawns.Clear();

        playerSpawn = spawnParent.GetChild(0).GetComponent<Spawn_SP>();

        foreach (Transform spawn in spawnParent.GetChild(1))
        {
            enemySpawns.Add(spawn.GetComponent<Spawn_SP>());
        }
    }


    public void StartGame()
    {
        if (allCurrentArenas.Count == 0)
        {
            allCurrentArenas.Add(Instantiate(allArenasArray[Random.Range(0, 5)], arenaPositions[2], Quaternion.identity));

            GameObject secondArena = Instantiate(allArenasArray[Random.Range(0, 5)], arenaPositions[3], Quaternion.identity);
            allCurrentArenas.Add(secondArena);

            allCurrentArenas.Add(Instantiate(allArenasArray[Random.Range(0, 5)], arenaPositions[4], Quaternion.identity));

            backgroundRenderer.material.color = secondArena.GetComponent<SceneInfo_SP>().backgroundColor;
        }

        UpdateSpawns(allCurrentArenas[1].transform.GetChild(0));



        GameMode_SP.Instance.StartNewRound();
    }

    
    public void NextMap()
    {
        allCurrentArenas.Add(Instantiate(allArenasArray[Random.Range(0, 5)], arenaPositions[5], Quaternion.identity));

        for (int i = 0; i < 4; i++)
        {
            if (allCurrentArenas[i])
            {
                allArenasDictionary[allCurrentArenas[i]] = arenaPositions[i + 1];
            }
        }

        rotating = true;

        lerpValue = 0f;

        oldColor = backgroundRenderer.material.color;
        newColor = allCurrentArenas[2].GetComponent<SceneInfo_SP>().backgroundColor;
    }
}
