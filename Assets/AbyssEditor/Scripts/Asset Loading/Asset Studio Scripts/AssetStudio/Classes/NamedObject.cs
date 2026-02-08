using AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Extensions;
namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Classes
{
    public class NamedObject : EditorExtension
    {
        public string m_Name;

        protected NamedObject(ObjectReader reader) : base(reader)
        {
            m_Name = reader.ReadAlignedString();
        }
    }
}
