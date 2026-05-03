using AbyssEditor.Scripts.Legacy;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    [RequireComponent(typeof(UIWindow))]
    public class ExportWindow : MonoBehaviour {
        [SerializeField] private Carousel exportFileTypeCarousel;

        [SerializeField] private Toggle checkbox;
        [SerializeField] private GameObject checkboxGroup;
        [SerializeField] private Button exportButton;
        
        private ExportMode selectedExportMode;
        private bool exportIntoGame;
        
        private void Start()
        {
            ToggleCheckboxVisibility(selectedExportMode == ExportMode.Optoctree);
            checkbox.onValueChanged.AddListener(OnCheckboxInteract);
            OnCheckboxInteract(false);
            OnModeChanged(exportFileTypeCarousel.GetSelectedElementLanguageKey());
            exportFileTypeCarousel.onOptionSelected += OnModeChanged;
            exportButton.onClick.AddListener(Export);
        }

        private void Export() {
            if (VoxelMetaspace.metaspace.batches.Count == 0) {
                EditorUI.inst.DisplayErrorMessage("Nothing to export!");
                return;
            }

            string exportPath = null;
            
            switch (selectedExportMode)
            {
                case ExportMode.OptoctreePatch:
                {
                    exportPath = StandaloneFileBrowser.StandaloneFileBrowser.SaveFilePanel("Save patch as...", Application.dataPath, "TerrainPatch", "optoctreepatch");
                    if (string.IsNullOrEmpty(exportPath)) return;
                    
                    _ = VoxelWorld.ExportPatch(exportPath);
                    
                    break;
                }
                case ExportMode.Optoctree:
                {
                    if (exportIntoGame)
                    {
                        exportPath = SnPaths.instance.CompiledPatchesFolder();
                    }
                    else
                    {
                        string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFolderPanel("Select export folder...", Application.dataPath, false);
                        if (paths.Length == 0) return;
                        
                        exportPath = paths[0];
                    }
                    _ = VoxelWorld.ExportOptoctrees(exportPath);
                    break;
                }
                case ExportMode.Fbx:
                {
                    exportPath = StandaloneFileBrowser.StandaloneFileBrowser.SaveFilePanel("Save model as...", Application.dataPath, "Fbx Model", "fbx");
                    if (string.IsNullOrEmpty(exportPath)) return;
                    StartCoroutine(ExportFBX.ExportMetaspaceAsync(exportPath));
                    break;
                }
                case ExportMode.None:
                default:
                {
                    DebugOverlay.LogError("Unexpected export mode! (this shouldn't be possible)");
                    break;
                }
            }
        }

        private void OnCheckboxInteract(bool value) {
            exportIntoGame = value;
        }

        private void ToggleCheckboxVisibility(bool value)
        {
            checkboxGroup.SetActive(value);
            if (!value)
            {
                exportIntoGame = false;
            }
            else
            {
                exportIntoGame = checkbox.isOn;
            }
        }

        private void OnModeChanged(string mode) {
            switch (mode)
            {
                case ".optoctreePatch":
                    ToggleCheckboxVisibility(false);
                    selectedExportMode =  ExportMode.OptoctreePatch;
                    break;
                case ".optoctree":
                    ToggleCheckboxVisibility(true);
                    selectedExportMode = ExportMode.Optoctree;
                    break;
                case ".fbx":
                    ToggleCheckboxVisibility(false);
                    selectedExportMode = ExportMode.Fbx;
                    break;
            }
        }


        private enum ExportMode
        {
            None,
            OptoctreePatch, //.optoctreepatch file
            Optoctree, //.optoctrees files
            Fbx, //.fbx file
        }
    }
}