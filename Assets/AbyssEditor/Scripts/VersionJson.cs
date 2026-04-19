using System;
using Newtonsoft.Json;
using UnityEngine;

namespace AbyssEditor.Scripts
{
    public class VersionJson
    {
        [NonSerialized] public const string VERSION_FILE_NAME = "versionChecking.json";
        
        public string latestVersion;
        public string downloadURL;
        public string notificationMessage;

        public static VersionJson DefaultEmpty()
        {
            VersionJson versionJson = new();
            versionJson.latestVersion = Application.version;
            return versionJson;
        }

        public static VersionJson ConvertFromBlob(string textBlob)
        {
            VersionJson versionJson;
            try
            {
                versionJson = JsonConvert.DeserializeObject<VersionJson>(textBlob);
            }
            catch (JsonReaderException)
            {
                Debug.LogError($"Failed to load version information form {textBlob}!");
                return null;
            }
            return versionJson;
        }
    }
}
