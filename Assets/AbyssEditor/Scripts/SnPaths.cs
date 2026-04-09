using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;

namespace AbyssEditor.Scripts {
    public class SnPaths : MonoBehaviour {

        public static SnPaths instance { get; private set; }
        
        public GameType currentGameType { get; private set; }
        
        public string BatchSourcePath() => Path.Combine(Preferences.data.gamePath, GameDataFolder(), dataToUnmanaged, GameExportWindow(), "CompiledOctreesCache");
        public string CompiledPatchesFolder() => Path.Combine(BatchSourcePath(), "Patches");
        private string GameDataFolder() 
        {
            return currentGameType switch
            {
                GameType.Subnautica => "Subnautica_Data",
                GameType.SubnauticaBelowZero => "SubnauticaZero_Data",
                _ => "error"
            };
        }
        private string GameExportWindow()
        {
            return currentGameType switch
            {
                GameType.Subnautica => "Build18",
                GameType.SubnauticaBelowZero =>  "Expansion",
                _ => "error"
            };
        }
        public string resourcesSourcePath => Path.Combine(Preferences.data.gamePath, GameDataFolder());
        public string BlockTypeStringsFilename()
        {
            return currentGameType switch
            {
                GameType.Subnautica => "blocktypeStrings",
                GameType.SubnauticaBelowZero =>  "blocktypeStringsBZ",
                _ => "error"
            };
        }
        
        private static readonly string dataToUnmanaged = Path.Combine("StreamingAssets", "SNUnmanagedData");
        private static readonly string dataToAddressables = Path.Combine("StreamingAssets", "aa", "StandaloneWindows64");
        
        private void Start() {
            instance = this;
            
            if (string.IsNullOrWhiteSpace(Preferences.data.gamePath)) return;
            
            if (!IsGamePathValid()) return;
            
            TrySetGameType();
        }
        
        public static bool IsGamePathValid() {
            return TrySetGameType() && Directory.Exists(instance.BatchSourcePath()) && Directory.Exists(instance.resourcesSourcePath);
        }

        private static bool TrySetGameType()
        {
            IEnumerable<string> directories = Directory
                .GetDirectories(Preferences.data.gamePath)
                .Select(Path.GetFileName);
            
            foreach (string directoryName in directories)
            {
                switch (directoryName)
                {
                    case "Subnautica_Data":
                        instance.currentGameType = GameType.Subnautica;
                        return true;
                    case "SubnauticaZero_Data":
                        instance.currentGameType = GameType.SubnauticaBelowZero;
                        return true;
                }
            }

            return false;
        }

        public enum GameType
        {
            Subnautica,
            SubnauticaBelowZero,
        }
    }
}