using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    public class UISettingsWindow : UIWindow
    {
        [SerializeField] private TextMeshProUGUI gamePathText;
        [SerializeField] private Toggle fullscreenToggleButton;
        [SerializeField] private Toggle autoLoadMaterialsToggleButton;
        [SerializeField] private Toggle discordRPCToggleButton;

        
        private void Start()
        {
            fullscreenToggleButton.SetIsOnWithoutNotify(Preferences.data.fullscreen);
            
            autoLoadMaterialsToggleButton.SetIsOnWithoutNotify(Preferences.data.autoLoadMaterials);

            discordRPCToggleButton.SetIsOnWithoutNotify(Preferences.data.discordRPC);
        }

        public void BrowseGamePath() {
            string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFolderPanel(Language.main.Get("FileBrowserTip"), Application.persistentDataPath, false);

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

        public void OnDiscordRPCToggle(bool value)
        {
            Preferences.data.discordRPC = value;
            Preferences.SavePreferences();
        }
    }
}
