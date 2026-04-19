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
                string source = Path.Combine(EditorUtils.gitRootPath, fileName);
                string destination = Path.Combine(Path.GetDirectoryName(pathToBuiltProject)!, fileName);

                File.Copy(source, destination, true);
            }
            
            Debug.Log($"Licenses copied to build");
        }
        
        [PostProcessBuild]
        private static void OnPostBuildDocumentationCopy(BuildTarget target, string pathToBuiltProject)
        {
            string docsSourceFolderPath = Path.Combine(EditorUtils.gitRootPath, "AbyssEditorManual", "site");
            
            const string docsDestinationFolderName = "AbyssEditorDocumentation";
#if UNITY_STANDALONE_OSX
            // shitty macos opens our .app in a quarantined zone on the os. So we can only use file INSIDE the .app, which we put the docs in Resources
            string destinationDirectory = Path.Combine(pathToBuiltProject, "Contents", "Resources", docsDestinationFolderName);
#else
            string destinationDirectory = Path.Combine(Path.GetDirectoryName(pathToBuiltProject)!, docsDestinationFolderName);
#endif

            CopyDirectoryRecursive(docsSourceFolderPath, destinationDirectory);
            
            Debug.Log($"Offline Docs copied to build");
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