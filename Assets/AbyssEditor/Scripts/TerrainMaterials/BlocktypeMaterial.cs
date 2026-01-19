using System.Collections.Generic;
using UnityEngine;

namespace AbyssEditor.TerrainMaterials
{
    public class BlocktypeMaterial {
        public string originalName;
        public string prettyName;
        public int blocktype;
        public bool hasDeco;
        private bool useCap;
        public Dictionary<long, string> propertyFromPathIDMap = new Dictionary<long, string>();
        public Texture2D[] textures;

        public BlocktypeMaterial(string _originalName, string _prettyName, int _blocktype, bool _hasDeco) {
            originalName = _originalName;
            prettyName = _prettyName;
            blocktype = _blocktype;
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

        public Material MakeMaterial() {
            Material mat;
            if (useCap) {
                mat = new Material(Globals.instance.batchCappedMat);

                mat.SetTexture("_MainTex", textures[0]);
                mat.SetTexture("_NormalMap", textures[1]);
                mat.SetTexture("_SideTex", textures[2]);
                mat.SetTexture("_SideNormalMap", textures[3]);
            } else {
                mat = new Material(Globals.instance.batchMat);

                mat.SetTexture("_MainTex", textures[0]);
                mat.SetTexture("_NormalMap", textures[1]);
            }
            mat.SetFloat("Tile", 1f);
            
            mat.name = originalName;

            return mat;
        }
    }
}