using System;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.Windows {
    public class UIExportWindow : UIWindow {
        [SerializeField] private Carousel exportFileTypeCarousel;

        [SerializeField] private UICheckbox checkbox;
        [SerializeField] private GameObject checkboxGroup;
        
        private ExportMode selectedExportMode;

        public void Export() {
            if (VoxelMetaspace.metaspace.meshes.Count == 0) {
                EditorUI.DisplayErrorMessage("Nothing to export!");
                return;
            }
            switch (selectedExportMode)
            {
                case ExportMode.OptoctreePatch:
                {
                    // Save .optoctreepatch file
                    string path = StandaloneFileBrowser.StandaloneFileBrowser.SaveFilePanel("Save patch as...", Application.dataPath, "TerrainPatch", "optoctreepatch");
                    if (string.IsNullOrEmpty(path)) {
                        return;
                    }
                    Globals.instance.userBatchOutputPath = path;
                    break;
                }
                case ExportMode.Optoctree:
                {
                    // Export some .optoctrees files
                    string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFolderPanel("Select export folder...", Application.dataPath, false);
                    if (paths.Length == 0) {
                        return;
                    }
                    Globals.instance.userBatchOutputPath = paths[0];
                    break;
                }
            }

            _ = VoxelWorld.ExportRegionAsync(selectedExportMode);
        }

        private void OnCheckboxInteract() {
            Globals.instance.exportIntoGame = checkbox.check;
        }

        private void ToggleCheckboxVisibility(bool value)
        {
            checkboxGroup.SetActive(value);
            if (!value)
            {
                Globals.instance.exportIntoGame = false;
            }
            else
            {
                Globals.instance.exportIntoGame = checkbox.check;//TODO: why do we have a static variable determining the export type. Just make another export type god dammit
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
            }
        }

        private void Start()
        {
            if (selectedExportMode == ExportMode.Optoctree)
            {
                ToggleCheckboxVisibility(true);
            }
            else
            {
                ToggleCheckboxVisibility(false);
            }
            checkbox.transform.GetComponent<Button>().onClick.AddListener(OnCheckboxInteract);
            OnCheckboxInteract();
            exportFileTypeCarousel.onOptionSelected += OnModeChanged;
        }
    }
}