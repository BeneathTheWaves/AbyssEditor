using UnityEngine.UI;
using SFB;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ReefEditor.UI {
    public class UISettingsWindow : UIWindow {
        UICheckbox fullscreenCheckbox;

        private void Start() {
            fullscreenCheckbox = GetComponentInChildren<UICheckbox>();

            if(fullscreenCheckbox != null)
            {
				fullscreenCheckbox.SetState(true);
				fullscreenCheckbox.transform.GetComponent<Button>().onClick.AddListener(UpdateFullscreenMode);
			}
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

        private void UpdateFullscreenMode() { Screen.fullScreenMode = (fullscreenCheckbox.check ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed); }
    }
}