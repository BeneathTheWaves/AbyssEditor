using System;
using System.Collections;
using System.Collections.Generic;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.VoxelTech;
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
            bool updateMeshesOnLoad = VoxelMetaspace.metaspace.meshes.Count != 0;

            int extraReloadMeshPhase = 0;
            if(updateMeshesOnLoad)
            {
                extraReloadMeshPhase = 1;//add another phase for loading meshes :p
            }
            
            EditorProcessHandle statusHandle = TaskManager.main.GetEditorProcessHandle(2 + extraReloadMeshPhase);// load mats and update icons
            if (!SnMaterialLoader.instance.contentLoaded)
            {
                yield return SnMaterialLoader.instance.LoadMaterialsFromGameAsync(updateMeshesOnLoad, statusHandle);
            }

            BlocktypeMaterial[] blockTypes = SnMaterialLoader.instance.blocktypesData;

            int successCount = 0;
            
            for (int i = 0; i < blockTypes.Length; i++)
            {
                BlocktypeMaterial mat = blockTypes[i];
                
                statusHandle.SetStatus($"Generating Icon For {i}");
                statusHandle.SetProgress((float) i / blockTypes.Length);
                
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
            
            statusHandle.CompletePhase();
        }
    }
}
