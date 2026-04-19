using System.IO;
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

        public void OpenUserManual()
        {
#if UNITY_EDITOR || UNITY_EDITOR_LINUX
            string gitRoot = Directory.GetParent(Application.dataPath).FullName;
            string docsFolderPath = Path.Combine(gitRoot, "AbyssEditorManual", "site");
#elif UNITY_STANDALONE_OSX
            string resourcesFolder = Path.Combine(Application.dataPath, "Resources");
            string docsFolderPath = Path.Combine(resourcesFolder, "AbyssEditorDocumentation");
            Debug.LogError($"Opening {Application.dataPath} {resourcesFolder} {docsFolderPath}");
#else
            string gameRoot = Directory.GetParent(Application.dataPath).FullName;
            string docsFolderPath = Path.Combine(gameRoot, "AbyssEditorDocumentation");
#endif
            string docsIndex = Path.Combine(docsFolderPath, "index.html");

            Application.OpenURL("file://" + docsIndex);
        }
        
        public void Quit()
        {
            Application.Quit(1);
        }
    }
}
