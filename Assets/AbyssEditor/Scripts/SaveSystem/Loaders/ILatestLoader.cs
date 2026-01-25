namespace AbyssEditor.Scripts.SaveSystem.Loaders
{
    public interface ILatestLoader
    {
        public abstract PreferencesFormat ConvertLoaderFormatToPreferencesFormat(DataFormatSnapshot format);
        
    }
}
