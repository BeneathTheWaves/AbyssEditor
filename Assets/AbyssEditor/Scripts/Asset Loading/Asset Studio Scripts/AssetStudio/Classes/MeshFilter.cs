namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Classes
{
    public sealed class MeshFilter : Component
    {
        public PPtr<Mesh> m_Mesh;

        public MeshFilter(ObjectReader reader) : base(reader)
        {
            m_Mesh = new PPtr<Mesh>(reader);
        }
    }
}
