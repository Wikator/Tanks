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

    [SerializeField]
    private Button endlessModeButton;

    [SerializeField]
    private Button campaignButton;


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

		endlessModeButton.onClick.AddListener(() => SceneManager.LoadScene("MapSelection_SP"));
        campaignButton.onClick.AddListener(() => SceneManager.LoadScene("CampaignArena"));
	}
}

