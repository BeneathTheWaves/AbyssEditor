using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.UI;
using SFB;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    public class UISettingsWindow : UIWindow
    {
        public TextMeshProUGUI gamePathText;
        public Toggle fullscreenToggleButton;
        public Toggle autoLoadMaterialsToggleButton;
        
        private void Start()
        {
            fullscreenToggleButton.isOn = Preferences.data.fullscreen;
            
            autoLoadMaterialsToggleButton.isOn = Preferences.data.autoLoadMaterials;
        }

        public void BrowseGamePath() {
            string[] paths = StandaloneFileBrowser.OpenFolderPanel(Language.main.Get("FileBrowserTip"), Application.persistentDataPath, false);

            if (paths.Length != 0) {
                Preferences.data.gamePath = paths[0];
                Preferences.SavePreferences();
                UpdatePathDisplay(Preferences.data.gamePath);
            }
        }
        public override void EnableWindow()
        {
            if (Globals.CheckIsGamePathValid())
            {
                UpdatePathDisplay(Preferences.data.gamePath);
            }
            base.EnableWindow();
        }
        private void UpdatePathDisplay(string path) => gamePathText.text = path;

        public void OnFullscreenToggle(bool value)
        {
            Screen.fullScreenMode = (value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
            Preferences.data.fullscreen = value;
            Preferences.SavePreferences();
        }

        public void OnAutoLoadMaterialsToggle(bool value)
        {
            Preferences.data.autoLoadMaterials = value;
            Preferences.SavePreferences();
        }
    }
}