using AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Extensions;
namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Classes
{
    public sealed class BuildSettings : Object
    {
        public string m_Version;

        public BuildSettings(ObjectReader reader) : base(reader)
        {
            var levels = reader.ReadStringArray();

            var hasRenderTexture = reader.ReadBoolean();
            var hasPROVersion = reader.ReadBoolean();
            var hasPublishingRights = reader.ReadBoolean();
            var hasShadows = reader.ReadBoolean();

            m_Version = reader.ReadAlignedString();
        }
    }
}
