using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.UI;
using SFB;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    public class UISettingsWindow : UIWindow {

        private void Start()
        {
            OnFullscreenToggle(true);
            OnAutoLoadMaterialsToggle(true);
            UpdatePathDisplay(Preferences.data.gamePath);
        }

        public void BrowseGamePath() {
            string[] paths = StandaloneFileBrowser.OpenFolderPanel("Select a Subnautica or Below Zero game folder.", Application.persistentDataPath, false);

            if (paths.Length != 0) {
                Preferences.data.gamePath = paths[0];
                Preferences.SavePreferences();
                UpdatePathDisplay(Preferences.data.gamePath);
            }
        }
        public override void EnableWindow()
        {
            UpdatePathDisplay(Preferences.data.gamePath);
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