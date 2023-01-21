using FishNet;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

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

        backgroundRenderer = GameObject.Find("Plane").GetComponent<Renderer>();
        targetColor = backgroundColors[0];
        oldColor = backgroundColors[0];
        lerpValue = 1f;

        StartCoroutine(AnimateColors());
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

