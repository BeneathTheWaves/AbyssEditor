using System.Collections.Generic;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
namespace AbyssEditor.Scripts.TerrainMaterials
{
    public class BlocktypeMaterial {
        private readonly string originalName;
        public readonly string prettyName;
        public readonly int blockType;
        public readonly bool hasDeco;
        private bool useCap;
        public Dictionary<long, string> propertyFromPathIDMap = new Dictionary<long, string>();
        private Texture2D[] textures;

        private Material madeMaterial;

        public BlocktypeMaterial(string _originalName, string _prettyName, int _blockType, bool _hasDeco) {
            originalName = _originalName;
            prettyName = _prettyName;
            blockType = _blockType;
            hasDeco = _hasDeco;
        }

        public Texture2D MainTexture {
            get {
                return textures[0];
            }
        }
        public Texture2D SideTexture {
            get {
                return textures[2];
            }
        }

        public bool ExistsInGame {
            get { 
                return textures != null;
            }
        }

        public void SetTexture(long pathID, Texture2D texture) {
            if (textures == null) {
                textures = new Texture2D[4];
            }
            
            switch (propertyFromPathIDMap[pathID]) {
                case "_MainTex":
                case "_CapTexture":
                    textures[0] = texture;
                    break;
                case "_BumpMap":
                case "_CapBumpMap":
                    textures[1] = texture;
                    break;
                case "_SideTexture":
                    useCap = true;
                    textures[2] = texture;
                    break;
                case "_SideBumpMap":
                    useCap = true;
                    textures[3] = texture;
                    break;
                default:
                    break;
            }
        } 

        public Material GetUnityMaterial()
        {
            if (madeMaterial) return madeMaterial;
            
            Material mat;
            if (useCap) {
                mat = new Material(VoxelWorld.world.batchCappedMat);
                mat.enableInstancing = true;

                mat.SetTexture(mainTex, textures[0]);
                mat.SetTexture(normalMap, textures[1]);
                mat.SetTexture(sideTex, textures[2]);
                mat.SetTexture(sideNormalMap, textures[3]);
            } else {
                mat = new Material(VoxelWorld.world.batchMat);
                mat.enableInstancing = true;
                
                mat.SetTexture(mainTex, textures[0]);
                mat.SetTexture(normalMap, textures[1]);
            }
            mat.SetFloat("Tile", 0.5f);
            
            mat.name = originalName;

            madeMaterial = mat;
            
            return mat;
        }
        
        private static readonly int mainTex = Shader.PropertyToID("_MainTex");
        private static readonly int normalMap = Shader.PropertyToID("_NormalMap");
        private static readonly int sideTex = Shader.PropertyToID("_SideTex");
        private static readonly int sideNormalMap = Shader.PropertyToID("_SideNormalMap");
    }
}