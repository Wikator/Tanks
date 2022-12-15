using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CampaignModeManager_SP : MonoBehaviour
{
    public static CampaignModeManager_SP Instance { get; private set; }

    [SerializeField]
    private GameObject[] allArenasArray = new GameObject[5];
    

    private List<Spawn_SP> enemySpawns = new();

    private Spawn_SP playerSpawn;
    
    private readonly List <GameObject> allCurrentArenas = new();

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

                    rotating = false;
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


    public Vector3 FindPlayerSpawn()
    {
        return playerSpawn.transform.position;
    }

    public Vector3 FindEnemySpawn()
    {
        Spawn_SP chosenSpawn = enemySpawns[Random.Range(0, enemySpawns.Count)];

        if (chosenSpawn.isOccupied)
        {
            return FindEnemySpawn();
        }

        return chosenSpawn.transform.position;
    }

    public void StartGame()
    {
        GameObject firstArena = Instantiate(allArenasArray[Random.Range(0, 5)], arenaPositions[3], Quaternion.identity);
        UpdateSpawns(firstArena.transform.GetChild(0));
        allCurrentArenas.Add(firstArena);

        allCurrentArenas.Add(Instantiate(allArenasArray[Random.Range(0, 5)], arenaPositions[4], Quaternion.identity));
    }

    
    public void NextMap()
    {
        allCurrentArenas.Add(Instantiate(allArenasArray[Random.Range(0, 5)], arenaPositions[5], Quaternion.identity));

        for (int i = 0; i < 3; i++)
        {
            if (allCurrentArenas[i])
            {
                allArenasDictionary[allCurrentArenas[i]] = arenaPositions[i + 1];
            }
        }

        rotating = true;

        lerpValue = 0f;

        oldColor = backgroundRenderer.material.color;
        newColor = backgroundColors[Random.Range(0, 5)];
    }
}
