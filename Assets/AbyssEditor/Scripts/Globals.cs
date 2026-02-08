using System.IO;
using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;
namespace AbyssEditor.Scripts {
    public class Globals : MonoBehaviour {

        public static Globals instance;

        public Material batchMat;
        public Material batchCappedMat;
        public Material brushGizmoMat;
        public Material simpleMapMat;
        public Material boundaryGizmoMat;
        public Color[] brushColors;
        public bool belowzero;
        public string userBatchOutputPath;

        public string batchSourcePath { get { return Path.Combine(Preferences.data.gamePath, gameDataFolder, dataToUnmanaged, gameExportWindow, "CompiledOctreesCache"); } }
        public string batchOutputPath { get { return exportIntoGame ? batchSourcePath : userBatchOutputPath; } }
        public string gameDataFolder { get { return belowzero ? "SubnauticaZero_Data" : "Subnautica_Data"; } }
        public string gameExportWindow { get { return belowzero ? "Expansion" : "Build18"; } }
        public string resourcesSourcePath { get { return Path.Combine(Preferences.data.gamePath, gameDataFolder); } }
        public string blocktypeStringsFilename { get { return belowzero ? "blocktypeStringsBZ" : "blocktypeStrings"; } }
        
        public const int THREAD_GROUP_SIZE = 8;
        
        public static string dataToUnmanaged = Path.Combine("StreamingAssets", "SNUnmanagedData");
        public static string dataToAddressables = Path.Combine("StreamingAssets", "aa", "StandaloneWindows64");
        public bool exportIntoGame;

        void Awake() {
            instance = this;
		}

        public static Color ColorFromType(int type) {
            Random.InitState(type);
            return new Color(Random.value, Random.value, Random.value);
        }

        public static Material GetBatchMat() {
            return instance.batchMat;
        }
        
        public static Material GetSimpleMapMat() {
            return instance.simpleMapMat;
        }

        public static int LinearIndex(int x, int y, int z, int dim) {
            return x + y * dim + z * dim * dim;
        }
        public static int LinearIndex(int x, int y, int z, Vector3Int dim) {
            return x + y * dim.x + z * dim.x * dim.y;
        }

        public static void UpdateBoundaries(Vector3 newPos, float radius) {
            instance.boundaryGizmoMat.SetVector("_CursorWorldPos", newPos);
            instance.boundaryGizmoMat.SetFloat("_BlendRadius", radius);
        }

        public static bool CheckIsGamePathValid() {
            return Directory.Exists(instance.batchSourcePath) && Directory.Exists(instance.resourcesSourcePath);
        }
    }
}