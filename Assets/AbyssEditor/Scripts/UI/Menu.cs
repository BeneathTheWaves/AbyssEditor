using AbyssEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    private void Start()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
    }

    public void LoadBatch()
    {
        SceneManager.LoadScene("AbyssEditor"); //Loadbatch window now automatically gets opened when the scene is loaded.
    }

    public void ShowAbout()
    {
        SceneManager.LoadScene("AboutPage");
    }

    public void Back()
    {
        SceneManager.LoadScene(0);
    }

    public void Quit()
    {
        Application.Quit(1);
    }
}
