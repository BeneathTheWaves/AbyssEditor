using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;

namespace AbyssEditor.Scripts {
    public class SnPaths : MonoBehaviour {

        public static SnPaths instance { get; private set; }
        
        public GameInstallType currentGameInstallType { get; private set; }
        
        public string BatchSourcePath() => Path.Combine(Preferences.data.gamePath, GameDataFolder(), dataToUnmanaged, GameExportWindow(), "CompiledOctreesCache");
        public string CompiledPatchesFolder() => Path.Combine(BatchSourcePath(), "patches");
        private string GameDataFolder() 
        {
            return currentGameInstallType switch
            {
                GameInstallType.SubnauticaWindows => "Subnautica_Data",
                GameInstallType.SubnauticaMac => Path.Combine("Subnautica.app", "Contents", "Resources", "Data"),
                GameInstallType.BelowZeroWindows => "SubnauticaZero_Data",
                //TODO: below zero directory structure
                _ => "error"
            };
        }
        private string GameExportWindow()
        {
            return currentGameInstallType switch
            {
                GameInstallType.SubnauticaWindows or GameInstallType.SubnauticaMac => "Build18",
                GameInstallType.BelowZeroWindows or GameInstallType.BelowZeroMac => "Expansion",
                _ => "error"
            };
            ;
        }
        public string resourcesSourcePath => Path.Combine(Preferences.data.gamePath, GameDataFolder());
        public string BlockTypeStringsFilename()
        {

            return currentGameInstallType switch
            {
                GameInstallType.SubnauticaWindows or GameInstallType.SubnauticaMac => "blocktypeStrings",
                GameInstallType.BelowZeroWindows or GameInstallType.BelowZeroMac => "blocktypeStringsBZ",
                _ => "error"
            };
            ;
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
            if (string.IsNullOrWhiteSpace(Preferences.data.gamePath))
            {
                return false;
            }
            
            IEnumerable<string> directories = Directory
                .GetDirectories(Preferences.data.gamePath)
                .Select(Path.GetFileName);
            
            foreach (string directoryName in directories)
            {
                switch (directoryName)
                {
                    case "Subnautica_Data":
                        instance.currentGameInstallType = GameInstallType.SubnauticaWindows;
                        return true;
                    case "Subnautica.app":
                        instance.currentGameInstallType = GameInstallType.SubnauticaMac;
                        return true;
                    case "SubnauticaZero_Data":
                        instance.currentGameInstallType = GameInstallType.BelowZeroWindows;
                        return true;
                }
            }

            return false;
        }
        
        public enum GameInstallType
        {
            SubnauticaWindows,
            BelowZeroWindows,
            SubnauticaMac,
            BelowZeroMac,
        }
    }
}