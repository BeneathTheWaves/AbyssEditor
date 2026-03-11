using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssEditor.Scripts.Essentials
{
    public class Bootstrap : MonoBehaviour
    {
        IEnumerator Start()
        {
            if (IsSceneLoaded("MainMenu") || IsSceneLoaded("AbyssEditor") )
            {
                Debug.LogWarning("Skipping Main Menu load");
                yield break;
            }
            
            AsyncOperation menuLoadOpp = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);
            
            menuLoadOpp.allowSceneActivation = true;
            yield return null;
        }
        
        public static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.isLoaded;
        }

    }
}
