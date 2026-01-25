using System.IO;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
namespace AbyssEditor.Scripts.SaveSystem.Loaders
{
    public class V1Loader : IPreferencesLoader, ILatestLoader
    {
        public int Version() => 1;
        
        public DataFormatSnapshot LoadFromFile(string filePath)
        {
            string raw = File.ReadAllText(filePath);
            DataFormatV1 dataFormatV1Data;
            try
            {
                dataFormatV1Data = JsonConvert.DeserializeObject<DataFormatV1>(raw);
            }
            catch (JsonReaderException ex)
            {
                Debug.LogError($"Failed to load version information for {Preferences.PREFERENCES_FILE_NAME}, creating a new default...");
                return null;
            }
            return dataFormatV1Data;
        }
        
        public (IPreferencesLoader, DataFormatSnapshot) UpgradeToNextVersion(DataFormatSnapshot format)
        {
            Debug.LogError("No version beyond latest to upgrade to!");
            return (null, null);
        }

        public PreferencesFormat ConvertLoaderFormatToPreferencesFormat(DataFormatSnapshot format)
        {
            DataFormatV1 formatData = (DataFormatV1) format;
            PreferencesFormat preferencesFormat = new PreferencesFormat();
            
            preferencesFormat.gamePath = formatData.gamePath;
            return preferencesFormat;
        }

        public class DataFormatV1 : DataFormatSnapshot
        {
            public string gamePath;
        }
    }
}
