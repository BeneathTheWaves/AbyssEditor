namespace AbyssEditor.Scripts.Asset_Loading.Asset_Studio_Scripts.AssetStudio
{
    public interface IProgress
    {
        void Report(int value);
    }

    public sealed class DummyProgress : IProgress
    {
        public void Report(int value) { }
    }
}
