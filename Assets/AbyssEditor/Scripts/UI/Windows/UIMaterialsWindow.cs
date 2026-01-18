using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ReefEditor.ContentLoading;

namespace ReefEditor.UI
{
    public class UIMaterialsWindow : UIWindow
    {
        public static UIMaterialsWindow main;

        public Transform gridParent;
        public Sprite favoritedButton;
        public Sprite unfavoritedButton;
        public Toggle showFavoritesOnlyToggle;

        private GameObject matIconPrefab;
        private bool materialsLoaded = false;
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
            transform.GetChild(1).GetChild(0).GetComponent<Text>().text = $"Load {(Globals.instance.belowzero ? "BZ" : "SN")} materials";
        }

        public void LoadMaterials()
        {

            if (!Globals.CheckIsGamePathValid())
            {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                return;
            }
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(2).gameObject.SetActive(true);

            if (!materialsLoaded)
            {
                StartCoroutine(DisplayMaterialIcons());
            }
        }

        public void SetShowFavoritedOnly()
        {
            showFavoritedOnly = showFavoritesOnlyToggle.isOn;
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

        private IEnumerator DisplayMaterialIcons()
        {
            materialsLoaded = true;

            Coroutine matLoadCoroutine = StartCoroutine(SNContentLoader.instance.LoadMaterialContent());
            while (SNContentLoader.instance.busyLoading)
            {
                EditorUI.UpdateStatusBar(SNContentLoader.instance.loadState, SNContentLoader.instance.loadProgress);
                yield return null;
            }

            EditorUI.DisableStatusBar();

            icons = new List<UIBlocktypeIconDisplay>();

            int successCount = 0;
            int errorCount = 0;
            foreach (BlocktypeMaterial mat in SNContentLoader.instance.blocktypesData)
            {
                if (mat != null && mat.ExistsInGame)
                {
                    GameObject newIconGameObj = Instantiate(matIconPrefab, gridParent);
                    UIBlocktypeIconDisplay newicon = new UIBlocktypeIconDisplay(newIconGameObj, mat);
                    icons.Add(newicon);
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }
            GetComponentInChildren<UnityEngine.UIElements.ScrollView>().scrollOffset = new Vector2(0, 0);
            DebugOverlay.LogMessage($"Finished loading {successCount} materials.");
            if (errorCount > 0) DebugOverlay.LogError($"Failed to load {errorCount} materials!");
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