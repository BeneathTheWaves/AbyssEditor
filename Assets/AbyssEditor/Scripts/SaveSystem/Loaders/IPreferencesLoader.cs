namespace AbyssEditor.Scripts.SaveSystem.Loaders
{
    public interface IPreferencesLoader
    {
        public abstract int Version();

        public abstract DataFormatSnapshot LoadFromFile(string filePath);
        

        /// <summary>
        /// Upgrade a loaders parsed format to the next format, if available
        /// </summary>
        /// <param name="format">format class within the loader that is being upgraded from</param>
        /// <returns>The loader that the format has been upgraded and the converted DataFormatSnapshot for that loader</returns>
        public abstract (IPreferencesLoader, DataFormatSnapshot) UpgradeToNextVersion(DataFormatSnapshot format);
    }
}
