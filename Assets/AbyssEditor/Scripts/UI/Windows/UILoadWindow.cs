using System;
using System.Collections;
using System.Collections.Generic;
using AbyssEditor.Scripts.BatchOutline;
using AbyssEditor.Scripts.BinaryReading;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.Windows {
    public class UILoadWindow : UIWindow
    {
        [SerializeField] private Carousel carouselLoadMethod;
        
        [SerializeField] private GameObject optoctreeGroup;
        [SerializeField] private TMP_InputField rangeStartInput;
        [SerializeField] TMP_InputField rangeEndInput;
        [SerializeField] private Toggle moddedBatchesCheckbox;
        
        [SerializeField] private GameObject optoctreePatchGroup;
        [SerializeField] private Button selectFileButton;
        [SerializeField] private TextMeshProUGUI patchFilePathText;
        
        private string lastPatchPath;
        private byte[] patchBytes;
        private List<Vector3Int> batchesInPatch;
        private List<int> batchOffsetsInPatch;

        private string lastLoadMethod;

        private void Start()
        {
            moddedBatchesCheckbox.isOn = false;
            carouselLoadMethod.onOptionSelected+=ChangeLoadMethod;
            ChangeLoadMethod("BaseGame");
            
            selectFileButton.onClick.AddListener(OnSelectFileButton);
        }

        public override void EnableWindow()
        {
            base.EnableWindow();
            ChangeLoadMethod(lastLoadMethod);
        }
        
        public override void DisableWindow()
        {
            base.DisableWindow();
            BatchOutlineManager.main.ResetOutlines();
        }
        
        private void ChangeLoadMethod(string newLoadMethod)
        {
            BatchOutlineManager.main.ResetOutlines();
            switch (newLoadMethod)
            {
                case "BaseGame":
                    OnEndEditInputField();
                    optoctreeGroup.SetActive(true);
                    optoctreePatchGroup.SetActive(false);
                    break;
                case "TerrainPatcher":
                    DrawPatchOutlines();
                    optoctreeGroup.SetActive(false);
                    optoctreePatchGroup.SetActive(true);
                    break;
            }
            lastLoadMethod = newLoadMethod;
        }

        public void OnSelectFileButton()
        {
            string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFilePanel(Language.main.Get("PatchSelectFileBrowserTip"), Application.persistentDataPath, "", false);

            if (paths.Length == 0 || !paths[0].ToLower().EndsWith(".optoctreepatch"))
            {
                EditorUI.DisplayErrorMessage("Please select a valid file!");
                return;
            }

            lastPatchPath = paths[0];

            patchFilePathText.text = lastPatchPath;

            patchBytes = ThreadedBinaryReadWriter.GetPatchBytes(lastPatchPath);
            
            ThreadedBinaryReadWriter.GetBatchIndexesAndOffsetsFromPatch(patchBytes, out List<Vector3Int> batchIndexes, out List<int> batchOffsets);
            batchesInPatch = batchIndexes;
            batchOffsetsInPatch = batchOffsets;

            DrawPatchOutlines();
        }

        private void DrawPatchOutlines()
        {
            if (batchesInPatch == null)
            {
                return;
            }
            
            BatchOutlineManager.main.DrawBatchOutline(batchesInPatch);

            Vector3Int minBatchIndex = batchesInPatch[0];
            Vector3Int maxBatchIndex = batchesInPatch[0];

            foreach (Vector3Int batchIndex in batchesInPatch)
            {
                minBatchIndex = Vector3Int.Min(minBatchIndex, batchIndex);
                maxBatchIndex = Vector3Int.Max(maxBatchIndex, batchIndex);
            }
            
            CameraControls.main.OnRegionLoad(minBatchIndex, maxBatchIndex);
        }

        public void OnApplyPatchButton()
        {
            StartCoroutine(ApplyPatch());
        }

        private IEnumerator ApplyPatch()
        {
            int overrideCount = 0;
            foreach (Vector3Int batchIndex in batchesInPatch)
            {
                if (VoxelMetaspace.metaspace.TryGetVoxelMesh(batchIndex, out VoxelBatch _))
                {
                    overrideCount++;
                }
            }

            if (overrideCount > 0)
            {
                UIConfirmationWindow.main.OpenWindow(
                    Language.main.Get("BatchOveriteWarning").Replace("%overiteCount%", overrideCount.ToString()),
                    Language.main.Get("GenericCancel"), 
                    Language.main.Get("GenericConfirm"), 
                    out UIConfirmationWindow.Response response
                );
                
                yield return new WaitUntil(() => response.receivedResponse);

                if (!response.response)
                {
                    yield break;
                }
            }
            _ = VoxelWorld.world.LoadOctreePatchAsync(patchBytes, batchesInPatch, batchOffsetsInPatch);
            base.DisableWindow();
        }

        public void OnEndEditInputField()
        {
            bool startEntered = TryParseBatchString(rangeStartInput.text, out Vector3Int startBatchIndex);
            bool endEntered = TryParseBatchString(rangeEndInput.text, out Vector3Int endBatchIndex);

            if (!startEntered && !endEntered)
            {
                return;
            }
            
            if (!startEntered) startBatchIndex = endBatchIndex; 
            if (!endEntered) endBatchIndex = startBatchIndex;
            
            BatchOutlineManager.main.DrawBatchOutline(startBatchIndex, endBatchIndex);
            
            Vector3Int minBatchIndex = startBatchIndex;
            Vector3Int maxBatchIndex = startBatchIndex;
            
            foreach (Vector3Int batchIndex in startBatchIndex.IterateTo(endBatchIndex))
            {
                minBatchIndex = Vector3Int.Min(minBatchIndex, batchIndex);
                maxBatchIndex = Vector3Int.Max(maxBatchIndex, batchIndex);
            }
            
            CameraControls.main.OnRegionLoad(minBatchIndex, maxBatchIndex);
        }

        public void OnLoadBatchButton()
        {
            StartCoroutine(LoadBatch());
        }

        private IEnumerator LoadBatch() {

            if (!Globals.CheckIsGamePathValid()) {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                yield break;
            }
            
            bool startEntered = TryParseBatchString(rangeStartInput.text, out Vector3Int startBatchIndex);
            bool endEntered = TryParseBatchString(rangeEndInput.text, out Vector3Int endBatchIndex);

            if (!startEntered && !endEntered) {
                EditorUI.DisplayErrorMessage("Please enter at least one batch index: \n\"x(space)y(space)z\"");
                yield break;
            }
            
            // assume user wants to load a single batch if only 1 is correct
            if (!startEntered) startBatchIndex = endBatchIndex; 
            if (!endEntered) endBatchIndex = startBatchIndex;
            
            Vector3Int startBatch = Vector3Int.Min(startBatchIndex, endBatchIndex);
            Vector3Int endBatch = Vector3Int.Max(startBatchIndex, endBatchIndex);

            int overrideCount = 0;
            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                if (VoxelMetaspace.metaspace.TryGetVoxelMesh(batchIndex, out VoxelBatch _))
                {
                    overrideCount++;
                }
            }

            if (overrideCount > 0)
            {
                UIConfirmationWindow.main.OpenWindow(
                    Language.main.Get("BatchOveriteWarning").Replace("%overiteCount%", overrideCount.ToString()),
                    Language.main.Get("GenericCancel"), 
                    Language.main.Get("GenericConfirm"), 
                    out UIConfirmationWindow.Response response
                );
                
                yield return new WaitUntil(() => response.receivedResponse);

                if (!response.response)
                {
                    yield break;
                }
            }
            
            _ = VoxelWorld.world.RegionLoadAsync(startBatchIndex, endBatchIndex, moddedBatchesCheckbox.isOn);
            base.DisableWindow();//we don't want it to remove the outlines until the loading is done so just call base method
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