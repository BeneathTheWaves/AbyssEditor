using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssEditor.Scripts.SaveSystem.Loaders;
using Newtonsoft.Json;
using UnityEngine;


namespace AbyssEditor.Scripts.SaveSystem
{
    /// <summary>
    /// When adding to this format make sure to edit V1Loaders format with the same data and update the 
    /// </summary>
    [Serializable]
    public class PreferencesMainFormat : DataFormatSnapshot
    {
        public readonly int configVersion = 1;//Latest Version!!!
        
        //Materials
        public bool showFavoritedOnly = false;
        public HashSet<int> favoritedMaterials = new();//must instantiate, forces newtonsoft to initialize the list
        
        //Lighting Tab
        public float sunPitch = 60f;
        public float sunYaw = 180;
        public bool enableBrushLight = true;
        public float sunColorR = 1f;
        public float sunColorG = 1f;
        public float sunColorB = 1f;
        public float sunIntensity = 0.5f;
        public float ambientIntensity = 0.5f;
        
        //Settings
        public string gamePath = "";
        public bool fullscreen = false;
        public bool autoLoadMaterials = true;
    }
    
    public class Preferences : MonoBehaviour
    {
        public const string PREFERENCES_FILE_NAME = "preferences.json";
        
        private static readonly IPreferencesLoader[] loaders = new IPreferencesLoader[]
        {
            new V1Loader()//,
            //new V2Loader()
        };

        public static PreferencesMainFormat data;

        public static void SavePreferences()
        {
            try
            {
                string filePath = Path.Combine(Application.persistentDataPath, PREFERENCES_FILE_NAME);
                string raw = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, raw);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save preferences: {ex}");
            }
        }
        
        private void Awake()
        {
            data = Load();
            SavePreferences();
        }

        private static PreferencesMainFormat Load()
        {
            if (!PreferencesExists(out string filePath))
            {
                return GetDefaultConfig();
            }

            if (!TryGetPreferencesVersion(filePath, out int version))
            {
                return GetDefaultConfig();
            }
            
            IPreferencesLoader loader = loaders.FirstOrDefault(l => l.Version() == version);
            if (loader == null)
            {
                Debug.LogError($"No loader available for v{version} when loading presets");
                return GetDefaultConfig();
            }
            
            DataFormatSnapshot loadedFormat = loader.LoadFromFile(filePath);

            if (loadedFormat == null)
            {
                return GetDefaultConfig();
            }
            
            while (loader is not ILatestLoader)
            {
                (loader, loadedFormat) = loader.UpgradeToNextVersion(loadedFormat);
            }

            return (PreferencesMainFormat)loadedFormat;
        }

        /// <summary>
        /// Get the current preference files version
        /// </summary>
        /// <param name="filePath">Path to preferences file</param>
        /// <param name="version">the resulting version that is found, or the latest if there are errors</param>
        /// <returns>true if a version is found, false if there are errors</returns>
        private static bool TryGetPreferencesVersion(string filePath, out int version)
        {
            string raw = File.ReadAllText(filePath);
            PreferencesVersionFormat deserializedVersionInfoOnly;
            try
            {
                deserializedVersionInfoOnly = JsonConvert.DeserializeObject<PreferencesVersionFormat>(raw);
            }
            catch (JsonReaderException)
            {
                Debug.LogError($"Failed to load version information for {PREFERENCES_FILE_NAME}, creating a new default...");
                version = -1;
                return false;
            }
            
            if (deserializedVersionInfoOnly == null || deserializedVersionInfoOnly.configVersion == 0)
            {
                Debug.LogError($"No Version Field found in {PREFERENCES_FILE_NAME}, creating a new default...");
                version = -1;
                return false;
            }
    
            version =  deserializedVersionInfoOnly.configVersion;
            return true;
        }

        private static void BackupConfigIfExists()
        {
            if(PreferencesExists(out string filePath))
            {
                //TODO: Dont overwrite old backups maybe lol
                Debug.Log("Creating backup of old config file...");
                File.Copy(filePath, filePath+".bak", true);
            }
        }

        private static bool PreferencesExists(out string filePath)
        {
            filePath = Path.Combine(Application.persistentDataPath, PREFERENCES_FILE_NAME);
            if (File.Exists(filePath))
            {
                return true;
            }
            return false;
        }
        
        internal static PreferencesMainFormat GetDefaultConfig()
        {
            BackupConfigIfExists();//safety check
            return new PreferencesMainFormat();
        }
        
        private class PreferencesVersionFormat { public int configVersion { get; set; } }
    }
}