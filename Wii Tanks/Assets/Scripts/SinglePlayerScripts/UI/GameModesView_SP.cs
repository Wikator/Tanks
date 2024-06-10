using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameModesView_SP : MonoBehaviour
{
    [SerializeField] private Button endlessModeButton;

    [SerializeField] private Button campaignMode;


    private void Start()
    {
        endlessModeButton.onClick.AddListener(() => SceneManager.LoadScene("placeholder"));

        campaignMode.onClick.AddListener(() => SceneManager.LoadScene("placeholder"));
    }
}