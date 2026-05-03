using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.UI.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.TerrainMaterials
{
        public class UIBlocktypeIconDisplay
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
                        favorited = Preferences.data.favoritedMaterials.Contains(mat.blockType);
                    return favorited.Value;
                }
                set
                {
                    if (value)
                    {
                        Preferences.data.favoritedMaterials.Add(mat.blockType);
                    }
                    else
                    {
                        Preferences.data.favoritedMaterials.Remove(mat.blockType);
                    }
                    Preferences.SavePreferencesToDisk();
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

                string title = $"{mat.blockType}) {materialName}";
                gameObject.GetComponentInChildren<TextMeshProUGUI>().text = title;

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
                CursorToolManager.main.brushTool.SetBrushMaterial((byte)mat.blockType);
            }

            public void OnFavoriteButtonPressed()
            {
                Favorited = !Favorited;
                UpdateFavoriteDisplay();
                MaterialsWindow.main.UpdateFilter();
            }

            private void UpdateFavoriteDisplay()
            {
                favoriteButtonImage.sprite = Favorited ? MaterialIconGenerator.main.favoritedButton : MaterialIconGenerator.main.unfavoritedButton;
            }
        }
}
