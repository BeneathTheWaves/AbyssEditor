using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssEditor.Scripts.UI
{
    public class Menu : MonoBehaviour
    {
        private void Awake()
        {
            if (!SceneManager.GetSceneByName("Essentials").isLoaded)
            {
                SceneManager.LoadScene("Essentials", LoadSceneMode.Additive);
            }
        }
    
        private void Start()
        {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        public void LoadBatch()
        {
            SceneManager.LoadScene("AbyssEditor", LoadSceneMode.Additive); //Loadbatch window now automatically gets opened when the scene is loaded.
            SceneManager.UnloadSceneAsync("MainMenu");
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
}
