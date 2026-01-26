using System.Collections;
using System.Collections.Generic;
using AbyssEditor.Scripts;
using AbyssEditor.Scripts.Asset_Loading;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.UI;
using UnityEngine;
using UnityEngine.UI;
using AbyssEditor.TerrainMaterials;
using TMPro;
using UnityEngine.UIElements;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

namespace AbyssEditor.UI
{
    public class UIMaterialsWindow : UIWindow
    {
        public static UIMaterialsWindow main;

        public Transform gridParent;
        public Sprite favoritedButton;
        public Sprite unfavoritedButton;
        public GameObject showFavoritesOnlyToggle;
        
        public GameObject scrollView;
        public GameObject loadMatsButton;
        
        private GameObject matIconPrefab;
        private List<UIBlocktypeIconDisplay> icons;
        
        private bool showFavoritedOnly;

        private void Start()
        {
            main = this;
            if (matIconPrefab == null)
                matIconPrefab = Resources.Load<GameObject>("UI Material Icon");
        }

        public override void EnableWindow()
        {
            base.EnableWindow();
            loadMatsButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = Globals.instance.belowzero ? Language.main.Get("LoadmatsBZ") : Language.main.Get("LoadmatsSN");
        }

        public void LoadMaterials()
        {
            if (!Globals.CheckIsGamePathValid())
            {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                return;
            }
            loadMatsButton.SetActive(false);
            scrollView.SetActive(true);

            if (!SnMaterialLoader.instance.contentLoaded)
            {
                StartCoroutine(GenerateMaterialIcons());
            }
        }

        public void SetShowFavoritedOnly(bool value)
        {
            showFavoritedOnly = value;
            UpdateFilter();
        }

        private void UpdateFilter()
        {
            if (icons == null)
                return;
            foreach (var icon in icons)
            {
                icon.gameObject.SetActive(showFavoritedOnly ? icon.Favorited : true);
            }
        }

        private IEnumerator GenerateMaterialIcons()
        {
            MaterialRequest matLoadCoroutine = SnMaterialLoader.instance.LoadMaterialsFromGameAsync();
            while (!matLoadCoroutine.IsDone)
            {
                EditorUI.UpdateStatusBar(matLoadCoroutine.Status, matLoadCoroutine.Progress);
                yield return null;
            }

            EditorUI.DisableStatusBar();

            icons = new List<UIBlocktypeIconDisplay>();

            int successCount = 0;
            
            foreach (BlocktypeMaterial mat in SnMaterialLoader.instance.blocktypesData)
            {
                if (mat != null && mat.ExistsInGame)
                {
                    GameObject newIconGameObj = Instantiate(matIconPrefab, gridParent);
                    UIBlocktypeIconDisplay newicon = new UIBlocktypeIconDisplay(newIconGameObj, mat);
                    icons.Add(newicon);
                    successCount++;
                }
            }
            //scrollView.GetComponent<ScrollView>().scrollOffset = new Vector2(0, 0);
            DebugOverlay.LogMessage($"Finished loading {successCount} materials.");
        }

        private class UIBlocktypeIconDisplay
        {
            public GameObject gameObject;
            private BlocktypeMaterial mat;
            private Image favoriteButtonImage;
            private bool? favorited;

            public bool Favorited
            {
                get
                {
                    if (!favorited.HasValue)
                        favorited = PlayerPrefs.GetInt($"FavoritedBlockType_{mat.blocktype}") == 1;
                    return favorited.Value;
                }
                set
                {
                    PlayerPrefs.SetInt($"FavoritedBlockType_{mat.blocktype}", value ? 1 : 0);
                    favorited = value;
                }
            }

            public UIBlocktypeIconDisplay(GameObject _gameObject, BlocktypeMaterial _mat)
            {
                gameObject = _gameObject;
                mat = _mat;

                string materialName = mat.prettyName;
                if (materialName.Contains("deco"))
                {
                    materialName = string.Concat(materialName.Split(' ')[0], "-deco");
                }

                string title = $"{mat.blocktype}) {materialName}";
                gameObject.GetComponentInChildren<Text>().text = title;

                gameObject.GetComponent<Button>().onClick.AddListener(OnMaterialSelected);

                gameObject.transform.GetChild(2).gameObject.SetActive(mat.hasDeco);

                if (mat.MainTexture != null)
                {
                    Texture2D tex1 = mat.MainTexture;
                    gameObject.GetComponent<RawImage>().texture = tex1;
                }
                bool hasSideTexture = mat.SideTexture != null;
                if (hasSideTexture)
                {
                    Texture2D tex2 = mat.SideTexture;
                    gameObject.transform.GetChild(0).GetChild(0).GetComponent<RawImage>().texture = tex2;
                }
                gameObject.transform.GetChild(0).gameObject.SetActive(hasSideTexture);

                var favoriteButton = gameObject.transform.GetChild(3).gameObject.GetComponent<Button>();
                favoriteButton.onClick.AddListener(OnFavoriteButtonPressed);
                favoriteButtonImage = favoriteButton.image;

                UpdateFavoriteDisplay();
            }

            public void OnMaterialSelected()
            {
                Brush.SetBrushMaterial((byte)mat.blocktype);
            }

            public void OnFavoriteButtonPressed()
            {
                Favorited = !Favorited;
                UpdateFavoriteDisplay();
                main.UpdateFilter();
            }

            private void UpdateFavoriteDisplay()
            {
                favoriteButtonImage.sprite = Favorited ? main.favoritedButton : main.unfavoritedButton;
            }
        }
    }
}