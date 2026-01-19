using UnityEngine.UI;
using SFB;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace AbyssEditor.UI {
    public class UISettingsWindow : UIWindow {

        private void Start()
        {
            OnFullscreenToggle(true);
            OnAutoLoadMaterialsToggle(true);
        }

        public void BrowseGamePath() {
            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select a Subnautica or Below Zero game folder.", Application.persistentDataPath, false);

            if (paths.Length != 0) {
                Globals.SetGamePath(paths[0], true);
                UpdatePathDisplay(paths[0]);

                if(SceneManager.GetActiveScene().buildIndex == 0)
                {
                    SceneManager.LoadScene("AbyssEditor");
                }
            }
        }
        public override void EnableWindow()
        {
            UpdatePathDisplay(Globals.instance.gamePath);
            base.EnableWindow();
        }
        private void UpdatePathDisplay(string path) => transform.GetChild(1).GetChild(1).GetComponent<Text>().text = path;

        public void OnFullscreenToggle(bool value)
        {
            Screen.fullScreenMode = (value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
        }

        public void OnAutoLoadMaterialsToggle(bool value)
        {
            Globals.instance.autoLoadMats = value;
        }
    }
}