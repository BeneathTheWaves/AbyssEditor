using UnityEngine;
using UnityEngine.SceneManagement;
namespace AbyssEditor.Scripts.Essentials
{
    public class EnsureEssentials : MonoBehaviour
    {
        private void Awake()
        {
            if (!SceneManager.GetSceneByName("Essentials").isLoaded)
            {
                SceneManager.LoadScene("Essentials", LoadSceneMode.Additive);
            }
        }
    }
}
