using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace AbstractClasses
{
    public abstract class ArenaSelectionScene : MonoBehaviour
    {
        [SerializeField]
        protected GameObject[] allArenasArray = new GameObject[7];

        [SerializeField]
        [ColorUsage(hdr: true, showAlpha: true)]
        private Color[] backgroundColors = new Color[7];

        [SerializeField]
        [ColorUsage(hdr: true, showAlpha: true)]
        private Color oldColor;

        [SerializeField]
        private Button goLeftButton;

        [SerializeField]
        private Button goRightButton;

        [SerializeField]
        private Button playButton;

        protected readonly Dictionary<GameObject, Vector3> allArenasDictionary = new();

        protected bool rotating;

        private readonly Vector3[] arenaPositions = new Vector3[9];


        [SerializeField]
        private float rotateSpeed;

        private MeshRenderer backgroundRenderer;

        private float lerpValue;


        private void Awake()
        {
            backgroundRenderer = GameObject.Find("Background").GetComponent<MeshRenderer>();
        }

        protected virtual void Start()
        {
            rotating = false;

            arenaPositions[0] = new Vector3(-460, -140, 280);
            arenaPositions[1] = new Vector3(-345, -105, 210);
            arenaPositions[2] = new Vector3(-230, -70, 140);
            arenaPositions[3] = new Vector3(-115, -35, 70);
            arenaPositions[4] = Vector3.zero;
            arenaPositions[5] = new Vector3(115, -35, 70);
            arenaPositions[6] = new Vector3(230, -70, 140);
            arenaPositions[7] = new Vector3(345, -105, 210);
            arenaPositions[8] = new Vector3(460, -140, 280);

            foreach (var arena in allArenasArray)
            {
                allArenasDictionary[arena] = arena.transform.position;
            }

            //Settings.currentArena = "ArenaSelection";

            goLeftButton.onClick.AddListener(GoLeft);
            goRightButton.onClick.AddListener(GoRight);
            playButton.onClick.AddListener(() => OnSpacePressed(ChosenArena()));
        }



        private void GoLeft()
        {
            if (rotating)
                return;

            rotating = true;

            lerpValue = 0f;

            oldColor = backgroundRenderer.material.color;

            for (var i = 0; i < allArenasArray.Length + 1; i++)
            {
                foreach (var arena in allArenasArray)
                {
                    if (arena.transform.position != arenaPositions[i])
                        continue;
                
                    allArenasDictionary[arena] = arenaPositions[i + 1];
                    break;
                }
            }

            foreach (var arena in allArenasArray)
            {
                if (arena.transform.position != arenaPositions[7])
                    continue;
            
                arena.transform.position = arenaPositions[0];
                allArenasDictionary[arena] = arenaPositions[1];
                break;
            }

            foreach (var arena in allArenasArray)
            {
                if (arena.transform.position != arenaPositions[8])
                    continue;
            
                arena.transform.position = arenaPositions[1];
                allArenasDictionary[arena] = arenaPositions[2];
                break;
            }
        }

        private void GoRight()
        {
            if (rotating)
                return;

            rotating = true;

            lerpValue = 0f;

            oldColor = backgroundRenderer.material.color;

            for (var i = allArenasArray.Length + 1; i > 0; i--)
            {
                foreach (var arena in allArenasArray)
                {
                    if (arena.transform.position != arenaPositions[i])
                        continue;
                
                    allArenasDictionary[arena] = arenaPositions[i - 1];
                    break;
                }
            }

            foreach (var arena in allArenasArray)
            {
                if (arena.transform.position != arenaPositions[1])
                    continue;
            
                arena.transform.position = arenaPositions[8];
                allArenasDictionary[arena] = arenaPositions[7];
                break;
            }

            foreach (var arena in allArenasArray)
            {
                if (arena.transform.position != arenaPositions[0])
                    continue;
            
                arena.transform.position = arenaPositions[7];
                allArenasDictionary[arena] = arenaPositions[6];
                break;
            }
        }

        private void FixedUpdate()
        {
            if (!rotating)
                return;
        
            MoveAllArenas();

            // Slowly change back ground color to the color in array of same index as an arena that has a value in dictionary as vector3.zero
            for (var i = 0; i < allArenasArray.Length; i++)
            {
                if (allArenasDictionary[allArenasArray[i]] != Vector3.zero)
                    continue;
            
                backgroundRenderer.material.color = Color.Lerp(oldColor, backgroundColors[i], lerpValue);
                lerpValue += 1 / 56f;
                break;
            }
        }

        private void MoveAllArenas()
        {
            foreach (var arena in allArenasArray)
            {
                arena.transform.position = Vector3.MoveTowards(arena.transform.position, allArenasDictionary[arena], rotateSpeed);

                if (arena.transform.position != allArenasDictionary[arena]) continue;
                arena.transform.position = allArenasDictionary[arena];

                rotating = false;
            }
        }

        private string ChosenArena()
        {
            return (
                from arena in allArenasArray
                where arena.transform.position == Vector3.zero
                select arena.name).FirstOrDefault();
        }

        protected abstract void OnSpacePressed(string arenaName);
    }
}