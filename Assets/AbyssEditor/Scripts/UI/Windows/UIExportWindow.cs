using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.Windows {
    public class UIExportWindow : UIWindow {
        UICheckbox checkbox;
        UIButtonSelect modeSelection;

        GameObject checkboxGroup => transform.GetChild(2).GetChild(1).gameObject;

        public void Export() {
            if (VoxelMetaspace.metaspace.meshes.Count == 0) {
                EditorUI.DisplayErrorMessage("Nothing to export!");
                return;
            }

            switch (modeSelection.selection) {
                case 0:
                    // Export some .optoctrees files
                    string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFolderPanel("Select export folder...", Application.dataPath, false);
                    if (paths.Length == 0) {
                        // user cancels
                        return;
                    }
                    Globals.instance.userBatchOutputPath = paths[0];
                    break;
                case 1:
                    // Save .optoctreepatch file
                    string path = StandaloneFileBrowser.StandaloneFileBrowser.SaveFilePanel("Save patch as...", Application.dataPath, "TerrainPatch", "optoctreepatch");
                    if (string.IsNullOrEmpty(path)) {
                        // user cancels
                        return;
                    }
                    Globals.instance.userBatchOutputPath = path;
                    break;
                case 2:
                    // Save .fbx file
                    path = StandaloneFileBrowser.StandaloneFileBrowser.SaveFilePanel("Save mesh as...", Application.dataPath, "SubnauticaScene", "fbx");
                    if (string.IsNullOrEmpty(path)) {
                        // user cancels
                        return;
                    }
                    Globals.instance.userBatchOutputPath = path;
                    break;
            }
            if (modeSelection.selection == 1) {
            }
            else {
            }

            //VoxelWorld.OnRegionExported += EditorUI.DisableStatusBar;
            //EditorUI.UpdateStatusBar("Exporting...", 1);

            VoxelWorld.ExportRegion(modeSelection.selection);
        }

        public void OnCheckboxInteract() {
            Globals.instance.exportIntoGame = checkbox.check;
        }

        public void OnModeChanged() {
            if (modeSelection.selection != 0) {
                checkboxGroup.SetActive(false);
                Globals.instance.exportIntoGame = false;
            } else {
                checkboxGroup.SetActive(true);
                OnCheckboxInteract();
            }
        }

        // overrides
        public override void EnableWindow()
        {
            if (checkbox is null) {
                checkbox = GetComponentInChildren<UICheckbox>();
                checkbox.transform.GetComponent<Button>().onClick.AddListener(OnCheckboxInteract);
                OnCheckboxInteract();
            }
            if (modeSelection is null) {
                modeSelection = GetComponentInChildren<UIButtonSelect>();
                modeSelection.OnSelectionChanged += OnModeChanged;
                OnModeChanged();
            }

            SetFileCountStrings();

            base.EnableWindow();
        }
        
        //TODO: this code might be the most fucking awful thing i have read in my life jesus christ
        private void SetFileCountStrings() {
            if (VoxelMetaspace.metaspace.meshes.Count == 0) {
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = "No batches loaded";
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(false);
                return;
            }

            int batchCount = VoxelMetaspace.metaspace.meshes.Count;
            if (batchCount == 1)
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = "1 file";
            else
                transform.GetChild(2).GetChild(0).GetChild(2).GetChild(0).GetComponent<Text>().text = $"{batchCount} files";
            transform.GetChild(2).GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
        }
    }
}