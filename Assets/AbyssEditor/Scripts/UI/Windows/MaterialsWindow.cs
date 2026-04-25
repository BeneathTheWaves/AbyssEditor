using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TerrainMaterials;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Toggle = UnityEngine.UI.Toggle;

namespace AbyssEditor.Scripts.UI.Windows
{
    public class MaterialsWindow : MonoBehaviour
    {
        public static MaterialsWindow main;

        public Transform gridParent;
        public Toggle showFavoritesOnlyToggle;
        public GameObject loadMatsButton;
        
        private bool showFavoritedOnly;

        private void Start()
        {
            showFavoritedOnly = Preferences.data.showFavoritedOnly;
            showFavoritesOnlyToggle.SetIsOnWithoutNotify(Preferences.data.showFavoritedOnly);
            showFavoritesOnlyToggle.onValueChanged.AddListener(SetShowFavoritedOnly);
            
            string loadText = SnPaths.instance.currentGameInstallType switch
            {
                SnPaths.GameInstallType.SubnauticaWindows or SnPaths.GameInstallType.SubnauticaMac => Language.main.Get("LoadmatsSN"),
                SnPaths.GameInstallType.BelowZeroWindows or SnPaths.GameInstallType.BelowZeroMac => Language.main.Get("LoadmatsBZ"),
                _ => "error"
            };

            loadMatsButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = loadText;

            loadMatsButton.GetComponent<Button>().onClick.AddListener(LoadMaterialsIntoGrid);
            
            if (MaterialIconGenerator.main.materialIconsLoaded)
            {
                LoadIconsIntoGrid();
            }
        }

        public void LoadMaterialsIntoGrid() => MaterialIconGenerator.main.GenerateMaterialIcons(LoadIconsIntoGrid);

        private void LoadIconsIntoGrid()
        {
            loadMatsButton.SetActive(false);
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