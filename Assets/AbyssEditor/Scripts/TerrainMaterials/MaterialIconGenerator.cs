using System;
using System.Collections;
using System.Collections.Generic;
using AbyssEditor.Scripts.Asset_Loading;
using AbyssEditor.Scripts.UI;
using AbyssEditor.TerrainMaterials;
using AbyssEditor.UI;
using UnityEngine;
using MonoBehaviour = UnityEngine.MonoBehaviour;

namespace AbyssEditor.Scripts.TerrainMaterials
{
    public class MaterialIconGenerator : MonoBehaviour
    {
        public static MaterialIconGenerator main;
        
        public GameObject matIconPrefab;
        public Sprite favoritedButton;
        public Sprite unfavoritedButton;
        
        public readonly List<UIBlocktypeIconDisplay> icons = new();

        public bool materialIconsLoaded => icons.Count > 0;

        public bool loadingMaterialIcons = false;
        private void Awake()
        {
            main = this;
        }

        public void GenerateMaterialIcons(Action onCompleteCallback = null)
        {
            if (!Globals.CheckIsGamePathValid())
            {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                return;
            }
            if (materialIconsLoaded)
            {
                Debug.LogWarning("Materials Already Loaded!");
                return;
            }
            if (loadingMaterialIcons)
            {
                Debug.LogError("Materials Being Loaded Already!");
                return;
            }
            
            loadingMaterialIcons = true;
            StartCoroutine(GenerateMaterialIconsAsync(onCompleteCallback));
        }
        
        private IEnumerator GenerateMaterialIconsAsync(Action onCompleteCallback = null)
        {
            if (!SnMaterialLoader.instance.contentLoaded)
            {
                MaterialRequest matLoadCoroutine = SnMaterialLoader.instance.LoadMaterialsFromGameAsync();
                while (!matLoadCoroutine.IsDone)
                {
                    EditorUI.UpdateStatusBar(matLoadCoroutine.Status, matLoadCoroutine.Progress);
                    yield return null;
                }
            }

            EditorUI.DisableStatusBar();

            BlocktypeMaterial[] blockTypes = SnMaterialLoader.instance.blocktypesData;

            int successCount = 0;
            
            foreach (BlocktypeMaterial mat in blockTypes)
            {
                if (mat != null && mat.ExistsInGame)
                {
                    GameObject newIconGameObj = Instantiate(matIconPrefab, this.transform);
                    UIBlocktypeIconDisplay newicon = new UIBlocktypeIconDisplay(newIconGameObj, mat);
                    icons.Add(newicon);
                    successCount++;
                }
            }
            DebugOverlay.LogMessage($"Finished loading {successCount} materials.");
            
            if (onCompleteCallback != null)
            {
                onCompleteCallback();
            }
        }
    }
}
