using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    public void OnClick()
    {
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void onClick()
    {
        OnClick();
    }
}
