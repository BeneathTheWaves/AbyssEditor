using System.Collections.Generic;
using AbyssEditor.Scripts.EntityHandling;
using AbyssEditor.Scripts.UI.EntitySystem;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.Windows
{

    public class UIEntityWindow : UIWindow
    {
        public static UIEntityWindow main;

        public GameObject buttonPrefab;
        public RectTransform contentParent;
        public Transform poolParent;

        public RectTransform directoryButtonsParent;
        public GameObject directoryButtonPrefab;
        public GameObject directorySeparatorPrefab;

        public InputField searchBar;

        private List<string> visitedFolderHistory = new List<string>();
        private string lastActiveFolderPath;
        private string activeFolderPath;

        private bool showingSearchResults;

        private List<EntityBrowserButton> activeButtons = new List<EntityBrowserButton>();
        private Queue<EntityBrowserButton> pooledButtons = new Queue<EntityBrowserButton>();

        private void Awake()
        {
            main = this;
        }

        private void Start()
        {
            RenderFolder(EntityDatabase.main.RootFolder);
        }

        private void ResetWindow()
        {
            foreach (var button in activeButtons)
            {
                button.rectTransform.SetParent(poolParent);
                pooledButtons.Enqueue(button);
            }
            activeButtons.Clear();
            for (int i = 0; i < directoryButtonsParent.childCount; i++)
            {
                Destroy(directoryButtonsParent.GetChild(i).gameObject);
            }
        }

        public void RenderFolder(string folderPath)
        {
            if (EntityDatabase.main.TryGetFolder(folderPath, out var folder))
            {
                RenderFolder(folder);
            }
            else if (string.IsNullOrEmpty(folderPath))
            {
                RenderFolder(EntityDatabase.main.RootFolder); ;
            }
            else
            {
                DebugOverlay.LogError($"Failed to load folder at path '{folderPath}'!");
            }
        }

        public void RenderFolder(EntityBrowserFolder folder)
        {
            lastActiveFolderPath = activeFolderPath;
            visitedFolderHistory.Add(lastActiveFolderPath);

            ResetWindow();

            // Draw subentries

            foreach (var item in folder.Subentries)
            {
                RenderBrowserEntry(item);
            }

            // Draw directory buttons

            var directoriesList = folder.GetParentFolders();
            directoriesList.Add(folder);
            if (!directoriesList.Contains(EntityDatabase.main.RootFolder)) directoriesList.Insert(0, EntityDatabase.main.RootFolder);

            for (int i = 0; i < directoriesList.Count; i++)
            {
                var button = Instantiate(directoryButtonPrefab);
                button.GetComponent<RectTransform>().SetParent(directoryButtonsParent);
                button.GetComponent<EntityBrowserButton>().SetBrowserEntry(directoriesList[i]);
                if (i < directoriesList.Count - 1)
                {
                    Instantiate(directorySeparatorPrefab).GetComponent<RectTransform>().SetParent(directoryButtonsParent);
                }
            }

            activeFolderPath = folder.Path;
            showingSearchResults = false;
            searchBar.text = null;
        }

        public void OnUpdateFilterInput()
        {
            if (string.IsNullOrEmpty(searchBar.text))
            {
                RenderFolder(EntityDatabase.main.RootFolder);
                return;
            }

            Filter(searchBar.text);

            showingSearchResults = true;
        }

        public void Filter(string searchString)
        {
            ResetWindow();

            foreach (var folder in EntityDatabase.main.AllFolders)
            {
                if (FilterFolder(folder, searchString))
                {
                    RenderBrowserEntry(folder);
                }
            }

            foreach (var entity in EntityDatabase.main.AllEntitiesInBrowser)
            {
                if (FilterEntity(entity, searchString))
                {
                    RenderBrowserEntry(entity);
                }
            }
        }

        // Does anyone here know regex? On top of cases, we could ignore spaces and underscores too

        private bool FilterFolder(EntityBrowserFolder folder, string searchString)
        {
            return folder.Name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool FilterEntity(EntityBrowserEntity entity, string searchString)
        {
            return entity.EntityData.ClassId.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0 
                || entity.Name.IndexOf(searchString, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void RenderBrowserEntry(EntityBrowserEntryBase entry)
        {
            EntityBrowserButton button;
            if (pooledButtons.Count > 0)
            {
                button = pooledButtons.Dequeue();
            }
            else
            {
                button = Instantiate(buttonPrefab).GetComponent<EntityBrowserButton>();
            }
            button.rectTransform.SetParent(contentParent);
            button.SetBrowserEntry(entry);
            activeButtons.Add(button);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Mouse4))
            {
                GoUnback();
            }
            if (Input.GetKeyDown(KeyCode.Mouse3))
            {
                GoBack();
            }
        }

        public void GoBack()
        {
            if (showingSearchResults)
            {
                RenderFolder(activeFolderPath);
                return;
            }
            string folderToReturnTo;
            if (visitedFolderHistory.Count <= 0) folderToReturnTo = null;
            else folderToReturnTo = visitedFolderHistory[visitedFolderHistory.Count - 1];
            if (string.IsNullOrEmpty(folderToReturnTo))
                RenderFolder(EntityDatabase.main.RootFolder);
            else
                RenderFolder(folderToReturnTo);
            visitedFolderHistory.RemoveAt(visitedFolderHistory.Count - 1);
            if (visitedFolderHistory.Count > 0)
            {
                visitedFolderHistory.RemoveAt(visitedFolderHistory.Count - 1);
            }
        }

        public void GoUnback() // tf is that button called?
        {
            RenderFolder(lastActiveFolderPath);
        }
    }
}


    /* OLD VERSION
    public class UIEntityWindow : UIWindow
    {
        private static readonly int _specGlossMap = Shader.PropertyToID("_SpecGlossMap");
        private static readonly int _mainTex = Shader.PropertyToID("_MainTex");
        public class Prefab
        {
            private int index;
            private GameObject prefab;
            
            public Prefab(int index, GameObject prefab)
            {
                this.index = index;
                this.prefab = prefab;
            }
            public void Initialize(GameObject uiObj)
            {
                uiObj.GetComponentInChildren<Text>().text = $"{index + 1}) {prefab.name}";
                uiObj.GetComponent<Button>().onClick.AddListener(OnEntityClicked);
            }
            public void OnEntityClicked()
            {
                instance.SetActive(index);
            }
        }
        public static UIEntityWindow instance;

        public GameObject uiPrefab;
        public Transform iconParent;
        public string[] vanillaStartingPaths;

        public Shader urpShader;

        [NonSerialized]
        public bool isActive;

        [NonSerialized]
        public GameObject entity;

        private List<GameObject> prefabs = new List<GameObject>();
        private Dictionary<string, string> database = new Dictionary<string, string>();

        private void Awake()
        {
            instance = this;
        }
        public void SetActive(int index)
        {
            var prefab = prefabs[index];
            if (!prefab)
            {
                DebugOverlay.LogError($"Prefab in index: {index} not found.");
                return;
            }
            entity = Instantiate(prefab);
            isActive = true;
        }

        public void Load()
        {
            StartCoroutine(LoadCoroutine());
        }

        private IEnumerator LoadCoroutine()
        {
            var bundlesDir = Path.Combine(Globals.instance.gamePath, "Bundles");
            if (!Directory.Exists(bundlesDir))
            {
                DebugOverlay.LogError($"No folder found with path matching '{bundlesDir}'. All relevant bundles should be placed in this folder!");
                yield break;
            }

            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(2).gameObject.SetActive(true);
            var files = Directory.GetFiles(bundlesDir);
            foreach (var file in files)
            {
                if (Path.HasExtension(file))
                {
                    if (Path.GetExtension(file) != ".bundle")
                    {
                        continue;
                    }
                }

                var request = AssetBundle.LoadFromFileAsync(file);
                yield return request;
                var bundle = request.assetBundle;
                if (bundle == null)
                    continue;
                var assetNames = bundle.GetAllAssetNames();
                foreach (var assetName in assetNames)
                {
                    if (!assetName.EndsWith(".prefab"))
                    {
                        continue;
                    }
                    var asset = bundle.LoadAsset<GameObject>(assetName);
                    FixPrefabMaterials(asset);
                    prefabs.Add(asset);
                    var icon = Instantiate(uiPrefab, iconParent);
                    IconGenerator.IconOutput output = new IconGenerator.IconOutput();
                    yield return IconGenerator.GenerateIcon(asset, output);
                    icon.GetComponent<Image>().sprite = output.OutputSprite;
                    var prefab = new Prefab(prefabs.Count - 1, asset);
                    prefab.Initialize(icon);
                }
            }
            
    // i think this part was unused
            LoadVanillaPrefabs();

            foreach (var key in database.Values.Where(x => vanillaStartingPaths.Any(x.StartsWith)))
            {
                if (key.ToLower().Contains("base"))
                {
                    DebugOverlay.LogWarning($"Couldn't load prefab: {key} because it's in the blacklist.");
                    continue;
                }
                    
                Debug.Log($"Loading prefab: {key}");
                    
                var task = Addressables.LoadAssetAsync<GameObject>(key);
                yield return task;

                var asset = task.Result;

                if (asset == null)
                {
                    DebugOverlay.LogError($"Prefab: '{key}' is null. Skipping.");
                    continue;
                }
                FixPrefabMaterials(asset);
                var icon = Instantiate(uiPrefab, iconParent);
                IconGenerator.IconOutput output = new IconGenerator.IconOutput();
                yield return IconGenerator.GenerateIcon(asset, output);
                icon.GetComponent<Image>().sprite = output.OutputSprite;
                var prefab = new Prefab(prefabs.Count - 1, asset);
                prefab.Initialize(icon);
            }
    // i think this is the end of the unused part
            ResizeContent();
        }

        private void LoadVanillaPrefabs()
        {
            if (database.Count > 0)
            {
                return;
            }

            var fullFilename = Path.Combine(Globals.instance.resourcesSourcePath, Globals.dataToUnmanaged, "prefabs.db");
            
            if (!File.Exists(fullFilename))
            {
                DebugOverlay.LogError($"Prefabs.db file wasn't found in path: {fullFilename}");
                return;
            }
            
            using (FileStream fileStream = File.OpenRead(fullFilename))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    int num = binaryReader.ReadInt32();
                    Debug.Log(string.V1Format("PrefabDatabase::LoadPrefabDatabase(count: {0})", num));
                    for (int i = 0; i < num; i++)
                    {
                        string classId = binaryReader.ReadString();
                        string filePath = binaryReader.ReadString();
                        database[classId] = filePath;
                    }
                }
            }
        }

        private void FixPrefabMaterials(GameObject obj)
        {
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                if (materials == null)
                    continue;
                foreach (var m in materials)
                {
                    if (m)
                    {
                        m.shader = urpShader;
                        if (m.GetTexture(_specGlossMap) == null)
                        {
                            m.SetTexture(_specGlossMap, m.GetTexture(_mainTex));
                        }
                    }
                }
            }
        }

        void ResizeContent()
        {
            (transform.GetChild(2).GetChild(0).GetChild(0) as RectTransform).offsetMin = new Vector2(0, -225 * Mathf.Ceil(transform.GetChild(2).GetChild(0).GetChild(0).GetChild(0).childCount / 2f));
}
    }*/