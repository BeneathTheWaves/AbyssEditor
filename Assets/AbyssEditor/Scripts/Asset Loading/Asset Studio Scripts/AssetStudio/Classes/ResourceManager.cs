using System.Collections.Generic;
using AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Extensions;
namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio.Classes
{
    public class ResourceManager : Object
    {
        public KeyValuePair<string, PPtr<Object>>[] m_Container;

        public ResourceManager(ObjectReader reader) : base(reader)
        {
            var m_ContainerSize = reader.ReadInt32();
            m_Container = new KeyValuePair<string, PPtr<Object>>[m_ContainerSize];
            for (int i = 0; i < m_ContainerSize; i++)
            {
                m_Container[i] = new KeyValuePair<string, PPtr<Object>>(reader.ReadAlignedString(), new PPtr<Object>(reader));
            }
        }
    }
}
