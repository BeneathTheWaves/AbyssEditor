using AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Extensions;
namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Classes
{
    public sealed class TextAsset : NamedObject
    {
        public byte[] m_Script;

        public TextAsset(ObjectReader reader) : base(reader)
        {
            m_Script = reader.ReadUInt8Array();
        }
    }
}
