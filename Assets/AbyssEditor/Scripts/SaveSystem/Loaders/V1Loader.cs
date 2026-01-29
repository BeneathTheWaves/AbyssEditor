using System.IO;
using Newtonsoft.Json;
using UnityEngine;
namespace AbyssEditor.Scripts.SaveSystem.Loaders
{
    public class V1Loader : IPreferencesLoader, ILatestLoader
    {
        public int Version() => 1;
        
        public DataFormatSnapshot LoadFromFile(string filePath)
        {
            string raw = File.ReadAllText(filePath);
            PreferencesMainFormat dataMainFormatV1Data;
            try
            {
                dataMainFormatV1Data = JsonConvert.DeserializeObject<PreferencesMainFormat>(raw);
            }
            catch (JsonReaderException)
            {
                Debug.LogError($"Failed to load version information for {Preferences.PREFERENCES_FILE_NAME}, creating a new default...");
                return null;
            }
            return dataMainFormatV1Data;
        }
        
        public (IPreferencesLoader, DataFormatSnapshot) UpgradeToNextVersion(DataFormatSnapshot format)
        {
            Debug.LogError("No version beyond latest to upgrade to!");
            return (null, null);
        }
    }
}
