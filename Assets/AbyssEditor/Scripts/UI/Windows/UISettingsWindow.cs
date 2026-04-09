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
        [SerializeField] private Toggle enableBrushLogsToggleButton;
        [SerializeField] private Toggle enableStatsToggleButton;
        [SerializeField] private Toggle discordRPCToggleButton;
        [SerializeField] private UIHybridInput fieldOfViewSlider;
        [SerializeField] private UIHybridInput threadCountSlider;


        private void Start()
        {
            fullscreenToggleButton.SetIsOnWithoutNotify(Preferences.data.fullscreen);
            autoLoadMaterialsToggleButton.SetIsOnWithoutNotify(Preferences.data.autoLoadMaterials);
            enableBrushLogsToggleButton.SetIsOnWithoutNotify(Preferences.data.enableBrushLogs);
            enableStatsToggleButton.SetIsOnWithoutNotify(Preferences.data.enableStats);
            discordRPCToggleButton.SetIsOnWithoutNotify(Preferences.data.discordRPC);
            fieldOfViewSlider.OnValueUpdated += OnFieldOfViewChanged;
            fieldOfViewSlider.OnEndDragging += SavePreferences;
            fieldOfViewSlider.formatFunction = value => value.ToString("0");
            fieldOfViewSlider.SetValue(Preferences.data.fieldOfView);
            
            threadCountSlider.OnValueUpdated += OnThreadCountChanged;
            threadCountSlider.OnEndDragging += SavePreferences;
            threadCountSlider.formatFunction = value => $"{value:0} {Language.main.Get("ThreadCountFormat")}";
            threadCountSlider.maxValue = SystemInfo.processorCount;
            threadCountSlider.minValue = 1;
            threadCountSlider.SetValue(Preferences.data.threadCount);
        }

        public void BrowseGamePath()
        {
            string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFolderPanel(Language.main.Get("FileBrowserTip"), Application.persistentDataPath, false);

            if (paths.Length != 0)
            {
                Preferences.data.gamePath = paths[0];
                SavePreferences();
                UpdatePathDisplay(Preferences.data.gamePath);
                if (!SnPaths.CheckIsGamePathValid())
                {
                    DebugOverlay.LogError(Language.main.Get("GamePathNotValid"));
                }
            }
        }
        
        public override void EnableWindow()
        {
            if (SnPaths.CheckIsGamePathValid())
            {
                UpdatePathDisplay(Preferences.data.gamePath);
            }
            else
            {
                DebugOverlay.LogError(Language.main.Get("GamePathNotValid"));
            }
            base.EnableWindow();
        }
        private void UpdatePathDisplay(string path) => gamePathText.text = path;

        public void OnFullscreenToggle(bool value)
        {
            Screen.fullScreenMode = (value ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed);
            Preferences.data.fullscreen = value;
            SavePreferences();
        }

        public void OnAutoLoadMaterialsToggle(bool value)
        {
            Preferences.data.autoLoadMaterials = value;
            SavePreferences();
        }

        public void OnEnableBrushLogsToggle(bool value)
        {
            Preferences.data.enableBrushLogs = value;
            SavePreferences();
        }
        
        public void OnEnableStatsToggle(bool value)
        {
            Preferences.data.enableStats = value;
            StatsTextUI.main.ToggleVisibility(value);
            SavePreferences();
        }

        public void OnDiscordRPCToggle(bool value)
        {
            Preferences.data.discordRPC = value;
            SavePreferences();
        }
        
        private void OnFieldOfViewChanged()
        {
            float value = fieldOfViewSlider.lerpedValue;
            Preferences.data.fieldOfView = value;
            CameraControls.main.SetFieldOfView(value);
        }
        
        private void OnThreadCountChanged()
        {
            int value = (int) threadCountSlider.lerpedValue;
            Preferences.data.threadCount = value;
        }

        private static void SavePreferences()
        {
            Preferences.SavePreferences();
        }
    }
}
