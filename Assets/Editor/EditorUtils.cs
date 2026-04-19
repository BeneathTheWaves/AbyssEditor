using System.IO;
using UnityEngine;

namespace Editor
{
    public static class EditorUtils
    {
        public static readonly string gitRootPath = Directory.GetParent(Application.dataPath)!.FullName;
    }
}
