using FishNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public sealed class Menu : MonoBehaviour
{
    #region Serialized

    [SerializeField]
    private TextMeshProUGUI title;

    [SerializeField]
    private GameObject mainMenu;

    [SerializeField]
    private GameObject settingsMenu;

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

    [SerializeField]
    private Button settingsButton;

    [SerializeField]
    private Button Camera1Button;

    [SerializeField]
    private Button Camera2Button;

    [SerializeField]
    private Button goBackButton;

    [SerializeField]
    [ColorUsage(hdr: true, showAlpha: true)]
    private Color[] backgroundColors = new Color[7];

    #endregion

    [ColorUsage(hdr: true, showAlpha: true)]
    private Color oldColor;

    [ColorUsage(hdr: true, showAlpha: true)]
    private Color targetColor;

    private Renderer backgroundRenderer;

    private float lerpValue;


    //Start-up screen
    //User will have a choice to either host a server or connect as a client
    //With a PlayFlow dedicated server active, hostButton should be disabled

    private void Start()
    {
        PlayerPrefs.SetString("Background", "Background2");

        if (PlayerPrefs.GetString("Background") != "")
        {
            Settings.ChosenBackground = PlayerPrefs.GetString("Background");
        }
        else
        {
            Settings.ChosenBackground = "Background2";
        }

        if (PlayerPrefs.GetString("Camera") != "")
        {
            Settings.Camera = PlayerPrefs.GetString("Camera");
        }
        else
        {
            Settings.Camera = "Camera1";
        }

        

        if (testLocally)
        {
			connectButton.gameObject.SetActive(true);
			
			connectButton.onClick.AddListener(() => InstanceFinder.ClientManager.StartConnection());
        }

        hostButton.onClick.AddListener(() => SceneManager.LoadScene("MapSelection"));

		endlessModeButton.onClick.AddListener(() => SceneManager.LoadScene("MapSelection_SP"));
        campaignButton.onClick.AddListener(() => SceneManager.LoadScene("CampaignArena"));

        settingsButton.onClick.AddListener(() => settingsMenu.SetActive(true));
        settingsButton.onClick.AddListener(() => mainMenu.SetActive(false));
        goBackButton.onClick.AddListener(() => settingsMenu.SetActive(false));
        goBackButton.onClick.AddListener(() => mainMenu.SetActive(true));

        Camera1Button.onClick.AddListener(() => Settings.Camera = "Camera1");
        Camera2Button.onClick.AddListener(() => Settings.Camera = "Camera2");
        
        backgroundRenderer = GameObject.Find("Background").GetComponent<MeshRenderer>();
        targetColor = backgroundColors[0];
        oldColor = backgroundColors[0];
        lerpValue = 1f;

        //title.outlineWidth = 0.045f;

        //Settings.currentArena = "MainMenu";
    }

    private void FixedUpdate()
    {
        backgroundRenderer.material.color = Color.Lerp(oldColor, targetColor, lerpValue);
        title.faceColor = Color.Lerp(oldColor, targetColor, lerpValue);
        title.outlineColor = Color.Lerp(oldColor, targetColor, lerpValue);
        lerpValue += 1 / 200f;

        if (lerpValue >= 1f)
        {
            lerpValue = 0f;
            oldColor = targetColor;

            while (targetColor == oldColor)
            {
                targetColor = backgroundColors[Random.Range(0, backgroundColors.Length)];
            }
        }

        backgroundRenderer.material.SetFloat("_Rotation", backgroundRenderer.material.GetFloat("_Rotation") + 0.01f);
    }
}

