using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Editor
{
    internal static class PostBuildTasks
    {
        private static readonly string[] filesToCopyIntoBuild = { "LICENSE", "Third-Party-Licenses.txt" };
        
        [PostProcessBuild]
        private static void OnPostBuild(BuildTarget target, string pathToBuiltProject)
        {
            foreach (string fileName in filesToCopyIntoBuild)
            {
                string gitRoot = Directory.GetParent(Application.dataPath).FullName;
                string source = Path.Combine(gitRoot, fileName);
                string destination = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), fileName);

                File.Copy(source, destination, true);
            }
            
            Debug.Log($"License info copied to: {pathToBuiltProject}");
        }
    }
}