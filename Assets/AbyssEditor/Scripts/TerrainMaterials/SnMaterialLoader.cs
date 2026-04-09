using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.VoxelTech;
using AssetStudio;
using AssetStudio.Classes;
using UnityEngine;
using Material = UnityEngine.Material;
using MonoBehaviour = UnityEngine.MonoBehaviour;
using Object = AssetStudio.Classes.Object;
using Random = UnityEngine.Random;
using Shader = UnityEngine.Shader;
using TextAsset = UnityEngine.TextAsset;
using Texture2D = UnityEngine.Texture2D;
using TextureFormat = UnityEngine.TextureFormat;

namespace AbyssEditor.Scripts.TerrainMaterials
{
    public class SnMaterialLoader : MonoBehaviour {
        public static SnMaterialLoader instance;

        //TODO: this should prob be moved to metaspace (maybe make this class static as well)
        public BlocktypeMaterial[] blocktypesData;
        
        Dictionary<string, List<int>> materialBlocktypes;
        public bool contentLoaded = false;

        void Awake() {
            instance = this;
        }

        //NOTE: the status handle needs to check if another processPhase is needed for reloading meshes before this is called
        public IEnumerator LoadMaterialsFromGameAsync(bool updateMeshesOnLoad, EditorProcessHandle statusHandle)
        {
            statusHandle.SetTasksToCompleteForPhase(12);
            statusHandle.SetPhasePrefix("Loading material names");
            
            yield return StartCoroutine(LoadMaterialNamesAsync());
            
            statusHandle.IncrementTasksComplete();
            
            List<AssetStudio.Classes.Texture2D> textureAssets = new List<AssetStudio.Classes.Texture2D>();
            List<AssetStudio.Classes.Material> materialAssets = new List<AssetStudio.Classes.Material>();
            yield return StartCoroutine(GetMaterialAssetsAsync(textureAssets, materialAssets));
            
            statusHandle.SetPhasePrefix("Setting materials");
            statusHandle.IncrementTasksComplete(3);
            
            SetMaterials(materialAssets.ToArray());
            yield return null;
            
            statusHandle.SetPhasePrefix("Setting textures");
            statusHandle.IncrementTasksComplete(8);
            SetTextures(textureAssets.ToArray());
            yield return null;
            
            contentLoaded = true;
            
            if (updateMeshesOnLoad)
            {
                _ = VoxelMetaspace.metaspace.RegenerateMeshesAsync(statusHandle);
            }
            
            statusHandle.CompletePhase();
        }

        private IEnumerator GetMaterialAssetsAsync(List<AssetStudio.Classes.Texture2D> textureAssets, List<AssetStudio.Classes.Material> materialAssets) {

            //TODO: BZ material loading is bugged.
            //      In Subnautica 1, all the assets for terrain is stored in resources.
            //      In below zero they are not in that bundle. They are individually within their own bundles in StandaloneWindows64

            string bundleName = Path.DirectorySeparatorChar + "resources.assets";
            string resourcesPath = SnPaths.instance.resourcesSourcePath + bundleName;
            string[] files = { resourcesPath };

            AssetsManager assetManager = new AssetsManager();
            assetManager.LoadFiles(files);
            
            foreach (SerializedFile file in assetManager.assetsFileList)
            {
                yield return new WaitForEndOfFrame();
                foreach(Object obj in file.Objects) {
                    if (obj is AssetStudio.Classes.Texture2D textureAsset) {
                        textureAssets.Add(textureAsset);
                        continue;
                    }

                    if (obj is AssetStudio.Classes.Material materialAsset) {
                        materialAssets.Add(materialAsset);
                    }
                }
            }

            assetManager.Clear();
        }
        private IEnumerator LoadMaterialNamesAsync() {
            ResourceRequest materialNameRequest = Resources.LoadAsync<TextAsset>(SnPaths.instance.BlockTypeStringsFilename());
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

        private void SetTextures(AssetStudio.Classes.Texture2D[] textureAssets) {
            foreach (AssetStudio.Classes.Texture2D textureAsset in textureAssets) {
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

        private void SetMaterials(AssetStudio.Classes.Material[] materialAssets) {
            foreach (AssetStudio.Classes.Material materialAsset in materialAssets) {

                var texturePathIDs = new Dictionary<long, string>();
                foreach(KeyValuePair<string, UnityTexEnv> pair in materialAsset.m_SavedProperties.m_TexEnvs) {
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
                return instance.blocktypesData[b].GetUnityMaterial();
            }

            Material colorMat = new Material(VoxelWorld.world.batchMat);
            colorMat.enableInstancing = true;
            colorMat.name = $"Material of type {b}";
            colorMat.SetColor(color, ColorFromType(b));
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
        
        public static Color ColorFromType(int type) {
            Random.InitState(type);
            return new Color(Random.value, Random.value, Random.value);
        }
        
        private static readonly int color = Shader.PropertyToID("_Color");
    }
}