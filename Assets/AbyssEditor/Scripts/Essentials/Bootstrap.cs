using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AbyssEditor.Scripts.Essentials
{
    public class Bootstrap : MonoBehaviour
    {
        IEnumerator Start()
        {
            if (IsSceneLoaded("MainMenu"))
            {
                Debug.LogWarning("Skipping Main Menu load, this should only happen in editor...");
                yield break;
            }
            
            AsyncOperation menuLoadOpp = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Additive);

            /*
            menuLoadOpp.allowSceneActivation = false;

            while (menuLoadOpp.progress < 0.9f)
            {
                //we can check our load tasks that need to happen before everything here
            }

            */
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
