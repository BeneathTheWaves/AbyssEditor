using System.IO;
using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;

namespace AbyssEditor.Scripts {
    public class SnPaths : MonoBehaviour {

        public static SnPaths instance { get; private set; }
        
        public bool belowzero;
        public string userBatchOutputPath;
        
        public string batchSourcePath => Path.Combine(Preferences.data.gamePath, gameDataFolder, dataToUnmanaged, gameExportWindow, "CompiledOctreesCache");
        public string batchOutputPath => exportIntoGame ? batchSourcePath : userBatchOutputPath;
        private string gameDataFolder => belowzero ? "SubnauticaZero_Data" : "Subnautica_Data";
        private string gameExportWindow => belowzero ? "Expansion" : "Build18";
        public string resourcesSourcePath => Path.Combine(Preferences.data.gamePath, gameDataFolder);
        public string blocktypeStringsFilename => belowzero ? "blocktypeStringsBZ" : "blocktypeStrings";
        
        public static string dataToUnmanaged = Path.Combine("StreamingAssets", "SNUnmanagedData");
        public static string dataToAddressables = Path.Combine("StreamingAssets", "aa", "StandaloneWindows64");
        public bool exportIntoGame;

        void Awake() {
            instance = this;
		}
        
        public static bool CheckIsGamePathValid() {
            return Directory.Exists(instance.batchSourcePath) && Directory.Exists(instance.resourcesSourcePath);
        }
    }
}