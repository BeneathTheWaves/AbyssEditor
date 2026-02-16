namespace AssetStudio
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
