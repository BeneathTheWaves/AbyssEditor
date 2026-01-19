using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.UI {
    public class UILoadWindow : UIWindow
    {
        public UICheckbox moddedBatchesCheckbox;

        private void Start()
        {
            moddedBatchesCheckbox.SetState(false);
        }

        public void LoadBatch() {

            if (!Globals.CheckIsGamePathValid()) {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                return;
            }

            InputField rangeStartInput = transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<InputField>();
            InputField rangeEndInput = transform.GetChild(2).GetChild(2).GetChild(2).GetComponent<InputField>();

            Vector3Int start, end;
            bool startEntered = TryParseBatchString(rangeStartInput.text, out start);
            bool endEntered = TryParseBatchString(rangeEndInput.text, out end);

            if (!startEntered && !endEntered) {
                EditorUI.DisplayErrorMessage("Please enter at least one batch index: \n\"x(space)y(space)z\"");
                return;
            }
            
            // assume user wants to load a single batch if only 1 is correct
            if (!startEntered) start = end; 
            if (!endEntered) end = start;

            EditorUI.inst.StartCoroutine(LoadCoroutine(start, end));
            base.DisableWindow();
        }

        IEnumerator LoadCoroutine(Vector3Int start, Vector3Int end) {
            VoxelWorld.LoadRegion(start, end, moddedBatchesCheckbox.check);
            while (VoxelWorld.loadInProgress) {
                EditorUI.UpdateStatusBar(VoxelWorld.loadingState, VoxelWorld.loadingProgress);
                yield return null;
            }
            EditorUI.DisableStatusBar();
        }

        private bool TryParseBatchString(string s, out Vector3Int index) {
            
            string[] splitString = s.Split(' ');
            
            if (splitString.Length != 3) {
                index = Vector3Int.zero;
                return false;
            }
            
            if (int.TryParse(splitString[0], out int x) && int.TryParse(splitString[1], out int y) && int.TryParse(splitString[2], out int z)) {
                index = new Vector3Int(x, y, z);
                return true;
            }
            
            index = Vector3Int.zero;
            return false;
        }
    }
}