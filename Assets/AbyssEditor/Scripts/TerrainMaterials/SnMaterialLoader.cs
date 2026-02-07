using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.TerrainMaterials;
using AbyssEditor.VoxelTech;
using UnityEngine;

namespace AbyssEditor.Scripts.TerrainMaterials
{
    public class SnMaterialLoader : MonoBehaviour {
        public static SnMaterialLoader instance;
        public BlocktypeMaterial[] blocktypesData;
        Dictionary<string, List<int>> materialBlocktypes;
        public bool contentLoaded = false;

        void Awake() {
            instance = this;
        }

        //NOTE: the status handle needs to check if another processPhase is needed for reloading meshes before this is called
        public IEnumerator LoadMaterialsFromGameAsync(bool updateMeshesOnLoad, EditorProcessHandle statusHandle)
        {
            statusHandle.SetStatus("Loading material names");
            statusHandle.SetProgress(0);
            
            int totalTasks = 12;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            yield return StartCoroutine(LoadMaterialNamesAsync());
            sw.Stop();
            DebugOverlay.LogMessage($"Loaded material names in {sw.ElapsedMilliseconds}ms");
            
            statusHandle.SetStatus("Getting assets");
            statusHandle.SetProgress(1f / totalTasks);

            sw.Restart();
            List<AssetStudio.Texture2D> textureAssets = new List<AssetStudio.Texture2D>();
            List<AssetStudio.Material> materialAssets = new List<AssetStudio.Material>();
            yield return StartCoroutine(GetMaterialAssetsAsync(textureAssets, materialAssets));
            sw.Stop();
            DebugOverlay.LogMessage($"Got assets in {sw.ElapsedMilliseconds}ms");
            
            statusHandle.SetStatus("Setting materials");
            statusHandle.SetProgress(4f / totalTasks);
            sw.Restart();
            SetMaterials(materialAssets.ToArray());
            yield return null;
            statusHandle.SetStatus("Setting textures");
            statusHandle.SetProgress(8f / totalTasks);
            yield return null;
            SetTextures(textureAssets.ToArray());
            sw.Stop();
            
            yield return null;
            DebugOverlay.LogMessage($"Set assets in {sw.ElapsedMilliseconds}ms");
            contentLoaded = true;
            
            if (updateMeshesOnLoad)
            {
                VoxelMetaspace.metaspace.StartCoroutine(VoxelMetaspace.metaspace.RegenerateMeshesCoroutine(statusHandle));
            }
            
            statusHandle.CompletePhase();
        }

        private IEnumerator GetMaterialAssetsAsync(List<AssetStudio.Texture2D> textureAssets, List<AssetStudio.Material> materialAssets) {

            string bundleName = Path.DirectorySeparatorChar + "resources.assets";
            string resourcesPath = Globals.instance.resourcesSourcePath + bundleName;
            string[] files = { resourcesPath };

            AssetStudio.AssetsManager assetManager = new AssetStudio.AssetsManager();
            assetManager.LoadFiles(files);
            
            foreach (AssetStudio.SerializedFile file in assetManager.assetsFileList)
            {
                yield return new WaitForEndOfFrame();
                foreach(AssetStudio.Object obj in file.Objects) {
                    if (obj is AssetStudio.Texture2D textureAsset) {
                        textureAssets.Add(textureAsset);
                        continue;
                    }

                    if (obj is AssetStudio.Material materialAsset) {
                        materialAssets.Add(materialAsset);
                    }
                }
            }

            assetManager.Clear();
        }
        private IEnumerator LoadMaterialNamesAsync() {
            ResourceRequest materialNameRequest = Resources.LoadAsync<TextAsset>(Globals.instance.blocktypeStringsFilename);
            yield return materialNameRequest;

            string combinedString = (materialNameRequest.asset as TextAsset).text;
            
            string[] lines = combinedString.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            blocktypesData = new BlocktypeMaterial[255];
            materialBlocktypes = new Dictionary<string, List<int>>();

            foreach (string line in lines) {
                string[] split1 = line.Split(')');
                int.TryParse(split1[0], out int blocktype);
                string materialName = split1[1].Substring(1);
                
                string nondecoName = materialName;
                bool hasDeco = materialName.Contains("deco");
                if (hasDeco) {
                    nondecoName = materialName.Split(' ')[0];
                }
                
                if (nondecoName == "Sand01ToCoral15") {
                    nondecoName = "Sand01";
                }

                if (!materialBlocktypes.TryGetValue(nondecoName, out List<int> blocktypes)) {
                    blocktypes = new List<int>();
                    materialBlocktypes.Add(nondecoName, blocktypes);
                }
                blocktypes.Add(blocktype);
                
                blocktypesData[blocktype] = new BlocktypeMaterial(materialName, nondecoName, blocktype, hasDeco);
            }
        }

        private void SetTextures(AssetStudio.Texture2D[] textureAssets) {
            foreach (AssetStudio.Texture2D textureAsset in textureAssets) {
                List<int> blocktypes = new List<int>();
                if (IsTextureNeeded(textureAsset.m_PathID, out blocktypes)) {
                    byte[] image_data = textureAsset.image_data.GetData();

                    Texture2D newtexture = new Texture2D(textureAsset.m_Width, textureAsset.m_Height, (TextureFormat)((int)textureAsset.m_TextureFormat), true);
                    newtexture.LoadRawTextureData(image_data);
                    newtexture.Apply();

                    foreach(int b in blocktypes) {
                        blocktypesData[b].SetTexture(textureAsset.m_PathID, newtexture);
                    }
                }
            }
        }

        private void SetMaterials(AssetStudio.Material[] materialAssets) {
            foreach (AssetStudio.Material materialAsset in materialAssets) {

                var texturePathIDs = new Dictionary<long, string>();
                foreach(KeyValuePair<string, AssetStudio.UnityTexEnv> pair in materialAsset.m_SavedProperties.m_TexEnvs) {
                    long pathID = pair.Value.m_Texture.m_PathID;
                    if (pathID != 0 && !texturePathIDs.ContainsKey(pathID))
                        texturePathIDs.Add(pathID, pair.Key);
                }

                if (materialBlocktypes.ContainsKey(materialAsset.m_Name)) {
                    foreach(int blocktype in materialBlocktypes[materialAsset.m_Name]) {
                        blocktypesData[blocktype].propertyFromPathIDMap = texturePathIDs;
                    }
                }
            }
        }

        
        public static Material GetMaterialForType(int b) {
            if (instance.contentLoaded && instance.blocktypesData[b] != null && instance.blocktypesData[b].ExistsInGame) {
                return instance.blocktypesData[b].MakeMaterial();
            }

            Material colorMat = new Material(Globals.GetBatchMat());
            colorMat.name = $"Material of type {b}";
            colorMat.SetColor("_Color", Globals.ColorFromType(b));
            return colorMat;
        }
        

        bool IsTextureNeeded(long pathID, out List<int> blocktypes) {

            blocktypes = new List<int>();

            for(int i = 0; i < 255; i++) {
                if (blocktypesData[i] != null) { 
                    if (blocktypesData[i].propertyFromPathIDMap.ContainsKey(pathID) && pathID != 0) {
                        blocktypes.Add(i);
                    }
                }
            }

            return blocktypes.Count > 0;
        }
    }
}