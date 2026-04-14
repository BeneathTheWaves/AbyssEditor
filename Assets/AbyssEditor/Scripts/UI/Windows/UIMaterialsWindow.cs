using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TerrainMaterials;
using TMPro;
using UnityEngine;
using Toggle = UnityEngine.UI.Toggle;

namespace AbyssEditor.Scripts.UI.Windows
{
    public class UIMaterialsWindow : UIWindow
    {
        public static UIMaterialsWindow main;

        public Transform gridParent;
        public Toggle showFavoritesOnlyToggle;
        
        public GameObject scrollView;
        public GameObject loadMatsButton;
        
        private bool showFavoritedOnly;

        private void Start()
        {
            showFavoritedOnly = Preferences.data.showFavoritedOnly;
            showFavoritesOnlyToggle.SetIsOnWithoutNotify(Preferences.data.showFavoritedOnly);
        }

        public override void EnableWindow()
        {
            base.EnableWindow();
            string loadText = SnPaths.instance.currentGameInstallType switch
            {
                SnPaths.GameInstallType.SubnauticaWindows or SnPaths.GameInstallType.SubnauticaMac => Language.main.Get("LoadmatsSN"),
                SnPaths.GameInstallType.BelowZeroWindows or SnPaths.GameInstallType.BelowZeroMac => Language.main.Get("LoadmatsBZ"),
                _ => "error"
            };
            ;

            loadMatsButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = loadText;

            if (MaterialIconGenerator.main.materialIconsLoaded)
            {
                LoadIconsIntoGrid();
            }
        }

        public void LoadMaterialsIntoGrid()
        {
            MaterialIconGenerator.main.GenerateMaterialIcons(LoadIconsIntoGrid);
        }

        private void LoadIconsIntoGrid()
        {
            loadMatsButton.SetActive(false);
            scrollView.SetActive(true);
            
            foreach (UIBlocktypeIconDisplay icon in MaterialIconGenerator.main.icons)
            {
                icon.gameObject.transform.SetParent(gridParent, false);
            }
            UpdateFilter();
        }

        public void SetShowFavoritedOnly(bool value)
        {
            showFavoritedOnly = value;
            Preferences.data.showFavoritedOnly = value;
            Preferences.SavePreferences();
            UpdateFilter();
        }

        /// <summary>
        /// Refresh Material list to show favorite materials based on showFavoritedOnly
        /// </summary>
        public void UpdateFilter()
        {
            if (MaterialIconGenerator.main.icons == null)
                return;
            foreach (var icon in MaterialIconGenerator.main.icons)
            {
                icon.gameObject.SetActive(showFavoritedOnly ? icon.Favorited : true);
            }
        }
    }
}