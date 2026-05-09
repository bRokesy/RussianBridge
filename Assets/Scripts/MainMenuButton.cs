using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButton : MonoBehaviour
{
    public void onClick()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
