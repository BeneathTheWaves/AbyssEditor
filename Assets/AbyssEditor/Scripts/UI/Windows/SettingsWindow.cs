using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    [RequireComponent(typeof(UIWindow))]
    public class SettingsWindow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI gamePathText;
        [SerializeField] private Carousel fullscreenModeCarousel;
        [SerializeField] private Toggle autoLoadMaterialsToggleButton;
        [SerializeField] private Toggle enableBrushLogsToggleButton;
        [SerializeField] private Toggle enableStatsToggleButton;
        [SerializeField] private Toggle discordRPCToggleButton;
        [SerializeField] private UIHybridInput fieldOfViewSlider;
        [SerializeField] private UIHybridInput threadCountSlider;


        private void Start()
        {
            fullscreenModeCarousel.onOptionSelected += OnFullscreenModeChanged;
            fullscreenModeCarousel.SetInitialValue(Preferences.data.fullscreenMode);
            autoLoadMaterialsToggleButton.SetIsOnWithoutNotify(Preferences.data.autoLoadMaterials);
            autoLoadMaterialsToggleButton.onValueChanged.AddListener(OnAutoLoadMaterialsToggle);
            enableBrushLogsToggleButton.SetIsOnWithoutNotify(Preferences.data.enableBrushLogs);
            enableBrushLogsToggleButton.onValueChanged.AddListener(OnEnableBrushLogsToggle);
            enableStatsToggleButton.SetIsOnWithoutNotify(Preferences.data.enableStats);
            enableStatsToggleButton.onValueChanged.AddListener(OnEnableStatsToggle);
            discordRPCToggleButton.SetIsOnWithoutNotify(Preferences.data.discordRPC);
            discordRPCToggleButton.onValueChanged.AddListener(OnDiscordRPCToggle);
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
            
            if (SnPaths.IsGamePathValid())
            {
                UpdatePathDisplay(Preferences.data.gamePath);
            }
            else
            {
                DebugOverlay.LogError(Language.main.Get("GamePathNotValid"));
            }
        }

        public void BrowseGamePath()
        {
            string sfbOpenLocation = !string.IsNullOrWhiteSpace(Preferences.data.gamePath) ? Preferences.data.gamePath : Application.persistentDataPath;
            
            string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFolderPanel(Language.main.Get("FileBrowserTip"), sfbOpenLocation, false);

            if (paths.Length == 0) return;
            
            Preferences.data.gamePath = paths[0];
            SavePreferences();
            UpdatePathDisplay(Preferences.data.gamePath);
            if (!SnPaths.IsGamePathValid())
            {
                DebugOverlay.LogError(Language.main.Get("GamePathNotValid"));
            }
        }
        
        private void UpdatePathDisplay(string path) => gamePathText.text = path;

        private void OnFullscreenModeChanged(string fullscreenLanguageKey)
        {
            ScreenModeManager.ChangeScreenMode(fullscreenLanguageKey);
        }

        private void OnAutoLoadMaterialsToggle(bool value)
        {
            Preferences.data.autoLoadMaterials = value;
            SavePreferences();
        }

        private void OnEnableBrushLogsToggle(bool value)
        {
            Preferences.data.enableBrushLogs = value;
            SavePreferences();
        }
        
        private void OnEnableStatsToggle(bool value)
        {
            Preferences.data.enableStats = value;
            StatsTextUI.main.ToggleVisibility(value);
            SavePreferences();
        }

        private void OnDiscordRPCToggle(bool value)
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
