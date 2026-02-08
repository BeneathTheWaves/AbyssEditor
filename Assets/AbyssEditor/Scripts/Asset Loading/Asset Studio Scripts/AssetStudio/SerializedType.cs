using System.Collections.Generic;
namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio
{
    public class SerializedType
    {
        public int classID;
        public bool m_IsStrippedType;
        public short m_ScriptTypeIndex = -1;
        public List<TypeTreeNode> m_Nodes;
        public byte[] m_ScriptID; //Hash128
        public byte[] m_OldTypeHash; //Hash128
        public int[] m_TypeDependencies;
    }
}
