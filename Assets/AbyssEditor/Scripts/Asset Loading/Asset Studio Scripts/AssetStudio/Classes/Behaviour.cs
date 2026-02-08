using AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Extensions;
namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Classes
{
    public abstract class Behaviour : Component
    {
        public byte m_Enabled;

        protected Behaviour(ObjectReader reader) : base(reader)
        {
            m_Enabled = reader.ReadByte();
            reader.AlignStream();
        }
    }
}
