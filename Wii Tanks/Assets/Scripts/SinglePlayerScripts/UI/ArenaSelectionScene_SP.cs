using UnityEngine.SceneManagement;

public sealed class ArenaSelectionScene_SP : ArenaSelectionScene
{
    protected override void OnSpacePressed(string arenaName)
    {
        SceneManager.LoadScene(arenaName);
    }
}
