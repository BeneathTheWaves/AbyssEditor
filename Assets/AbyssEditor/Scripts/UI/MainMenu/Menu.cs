using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssEditor.Scripts.UI.MainMenu
{
    public class Menu : MonoBehaviour
    {
        public void LoadBatch()
        {
            SceneManager.LoadScene("AbyssEditor", LoadSceneMode.Additive); //Loadbatch window now automatically gets opened when the scene is loaded.
            SceneManager.UnloadSceneAsync("MainMenu");
        }

        public void Quit()
        {
            Application.Quit(1);
        }
    }
}
