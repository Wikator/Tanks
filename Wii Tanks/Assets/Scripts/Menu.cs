using FishNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public sealed class Menu : MonoBehaviour
{
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
    private Button background1Button;

    [SerializeField]
    private Button background2Button;

    [SerializeField]
    private Button goBackButton;

    [SerializeField]
    [ColorUsage(hdr: true, showAlpha: true)]
    private Color[] backgroundColors = new Color[7];

    [SerializeField]
    [ColorUsage(hdr: true, showAlpha: true)]
    private Color oldColor;

    [SerializeField]
    [ColorUsage(hdr: true, showAlpha: true)]
    private Color targetColor;

    private Renderer backgroundRenderer;
    private float lerpValue;


    //Start-up screen
    //User will have a choice to either host a server or connect as a client
    //With a PlayFlow dedicated server active, hostButton should be disabled

    private void Start()
    {
        Settings.ChosenBackground = "Background2";

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

        background1Button.onClick.AddListener(() => Settings.ChosenBackground = "Background1");
        background2Button.onClick.AddListener(() => Settings.ChosenBackground = "Background2");

        backgroundRenderer = GameObject.Find("Background").GetComponent<Renderer>();
        targetColor = backgroundColors[0];
        oldColor = backgroundColors[0];
        lerpValue = 1f;

        StartCoroutine(AnimateColors());

        //Settings.currentArena = "MainMenu";
    }


    private IEnumerator AnimateColors()
    {
        yield return new WaitForFixedUpdate();

        backgroundRenderer.material.color = Color.Lerp(oldColor, targetColor, lerpValue);
        lerpValue += 1 / 200f;

        if (lerpValue >= 1f)
        {
            lerpValue = 0f;
            oldColor = targetColor;

            while (targetColor == oldColor)
            {
                targetColor = backgroundColors[Random.Range(0, backgroundColors.Length - 1)];
            }
        }

        StartCoroutine(AnimateColors());
    }
}

