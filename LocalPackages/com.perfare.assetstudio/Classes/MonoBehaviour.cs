using AssetStudio.Extensions;
namespace AssetStudio.Classes
{
    public sealed class MonoBehaviour : Behaviour
    {
        public PPtr<MonoScript> m_Script;
        public string m_Name;

        public MonoBehaviour(ObjectReader reader) : base(reader)
        {
            m_Script = new PPtr<MonoScript>(reader);
            m_Name = reader.ReadAlignedString();
        }
    }
}
