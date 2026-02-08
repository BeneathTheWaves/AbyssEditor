namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio
{
    public class BuildType
    {
        private string buildType;

        public BuildType(string type)
        {
            buildType = type;
        }

        public bool IsAlpha => buildType == "a";
        public bool IsPatch => buildType == "p";
    }
}
