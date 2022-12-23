using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverView_SP : View_SP
{
    [SerializeField]
    private Button restartButton;

    [SerializeField]
    private Button mapSelectionButton;

    public override void Init()
    {
        if (Initialized)
            return;

        base.Init();

        restartButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex));

        mapSelectionButton.onClick.AddListener(() => SceneManager.LoadScene("MapSelection_SP"));
    }
}
