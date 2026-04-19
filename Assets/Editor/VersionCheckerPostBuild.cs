using System.IO;
using AbyssEditor.Scripts;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
{
    public static class VersionCheckerPostBuild
    {
        [PostProcessBuild]
        private static void OnPostBuildCreateVersionCheckerFile(BuildTarget target, string pathToBuiltProject)
        {
            if (!EditorPrefs.GetBool("UpdateVersionOnBuild"))
            {
                return;
            }
            if (EditorUserBuildSettings.development)
            {
                Debug.LogWarning("Skipping version file update on dev build");
                return;
            }
            
            string versionFilePath = Path.Combine(EditorUtils.gitRootPath, VersionJson.VERSION_FILE_NAME);

            if (!File.Exists(versionFilePath))
            {
                WriteVersionFile(VersionJson.DefaultEmpty(), versionFilePath);
                return;
            }
            string textBlob = File.ReadAllText(versionFilePath);
            VersionJson versionJson = VersionJson.ConvertFromBlob(textBlob);
            versionJson.latestVersion = Application.version;//Preserve the other data
            WriteVersionFile(versionJson, versionFilePath);
        }
        
        private static void WriteVersionFile(VersionJson versionJson, string filePath)
        {
            string raw = JsonConvert.SerializeObject(versionJson, Formatting.Indented);
            File.WriteAllText(filePath, raw);
        }
        
        public static class UpdateVersionFileOnBuildMenu
        {
            private const string MENU_PATH = "Build Settings/Update Version File On Build";

            [MenuItem(MENU_PATH)]
            public static void UpdateVersionToggle()
            {
                bool current = EditorPrefs.GetBool("UpdateVersionOnBuild");
                EditorPrefs.SetBool("UpdateVersionOnBuild", !current);
            }

            [MenuItem(MENU_PATH, true)]
            public static bool UpdateVersionToggleValidate()
            {
                bool current = EditorPrefs.GetBool("UpdateVersionOnBuild");
                Menu.SetChecked(MENU_PATH, current);
                return true;
            }
        }
    }
}
