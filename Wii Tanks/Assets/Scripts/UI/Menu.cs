using FishNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public sealed class Menu : MonoBehaviour
{
    [SerializeField]
    private bool testLocally;

    [SerializeField]
    private Button hostButton;

    [SerializeField]
    private Button connectButton;


    //Start-up screen
    //User will have a choice to either host a server or connect as a client
    //With a PlayFlow dedicated server active, hostButton should be disabled

    private void Awake()
    {       
        if (testLocally)
        {
			connectButton.gameObject.SetActive(true);
			
			connectButton.onClick.AddListener(() => InstanceFinder.ClientManager.StartConnection());
        }

        hostButton.onClick.AddListener(() => SceneManager.LoadScene("MapSelection"));
    }
}
