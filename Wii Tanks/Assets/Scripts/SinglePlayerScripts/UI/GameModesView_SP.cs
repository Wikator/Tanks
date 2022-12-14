using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameModesView_SP : MonoBehaviour
{
    [SerializeField]
    private Button endlessModeButton;

    [SerializeField]
    private Button campaignMode;


    void Start()
    {
        endlessModeButton.onClick.AddListener(() => SceneManager.LoadScene("placeholder"));

        campaignMode.onClick.AddListener(() => SceneManager.LoadScene("placeholder"));
    }
}
