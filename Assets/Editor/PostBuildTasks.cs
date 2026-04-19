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
        private static void OnPostBuildLicenseCopy(BuildTarget target, string pathToBuiltProject)
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
        
        [PostProcessBuild]
        private static void OnPostBuildDocumentationCopy(BuildTarget target, string pathToBuiltProject)
        {
            string gitRoot = Directory.GetParent(Application.dataPath).FullName;
            
            string docsSourceFolderPath = Path.Combine(gitRoot, "AbyssEditorManual", "site");
            
            const string docsDestinationFolderName = "AbyssEditorDocumentation";
            string destinationDirectory = Path.Combine(Path.GetDirectoryName(pathToBuiltProject), docsDestinationFolderName);

            CopyDirectoryRecursive(docsSourceFolderPath, destinationDirectory);
            
            Debug.Log($"Offline Docs {pathToBuiltProject}");
        }
        
        private static void CopyDirectoryRecursive(string sourceDir, string destinationDir)
        {
            var dir = new DirectoryInfo(sourceDir);
            
            if (!dir.Exists)
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            
            DirectoryInfo[] dirs = dir.GetDirectories();
            
            Directory.CreateDirectory(destinationDir);
            
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }
            
            foreach (DirectoryInfo subDir in dirs)
            {
                string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                CopyDirectoryRecursive(subDir.FullName, newDestinationDir);
            }
        }
    }
}