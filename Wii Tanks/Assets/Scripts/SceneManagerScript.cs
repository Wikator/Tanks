using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerScript : MonoBehaviour
{
    [SerializeField]
    private string sceneName;

    public static Transform BulletEmpty { get; private set; }
    public static Transform MuzzleFlashEmpty { get; private set; }
    public static Transform ExplosionEmpty { get; private set; }

    public static GameObject[] AllBullets
    {
        get
        {
            GameObject[] allBullets = new GameObject[BulletEmpty.childCount];

            for (int i = 0; i < allBullets.Length; i++)
            {
                allBullets[i] = BulletEmpty.GetChild(i).gameObject;
            }

            return allBullets;
        }
    }


    private void Awake()
    {
        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);

        BulletEmpty = GameObject.Find("Bullets").transform;
        MuzzleFlashEmpty = GameObject.Find("MuzzleFlashes").transform;
        ExplosionEmpty = GameObject.Find("Explosions").transform;
    }
}
