using System;
using System.Collections;
using System.Collections.Generic;
using AbyssEditor.Scripts.BatchOutline;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.VoxelTech;
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

        private string lastMethod;
        

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
            ChangeLoadMethod(lastMethod);
        }
        
        public override void DisableWindow()
        {
            base.DisableWindow();
            BatchOutlineManager.main.ResetLoadOutlines();
        }
        
        private void ChangeLoadMethod(string method)
        {
            BatchOutlineManager.main.ResetLoadOutlines();
            switch (method)
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
            lastMethod = method;
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

            patchBytes = BatchReadWriter.GetPatchBytes(lastPatchPath);
            batchesInPatch = BatchReadWriter.GetBatchIndexesFromPatch(patchBytes);

            DrawPatchOutlines();
        }

        public void DrawPatchOutlines()
        {
            if (batchesInPatch == null)
            {
                return;
            }
            
            BatchOutlineManager.main.DrawBatchOutline(batchesInPatch);

            Vector3Int minBatch = batchesInPatch[0];
            Vector3Int maxBatch = batchesInPatch[0];

            foreach (Vector3Int batchIndex in batchesInPatch)
            {
                minBatch = Vector3Int.Min(minBatch, batchIndex);
                maxBatch = Vector3Int.Max(maxBatch, batchIndex);
            }
            
            CameraControls.main.OnRegionLoad(minBatch, maxBatch);
        }

        public void OnApplyPatchButton()
        {
            StartCoroutine(ApplyPatch());
        }
        
        public IEnumerator ApplyPatch()
        {
            int overrideCount = 0;
            foreach (Vector3Int batchIndex in batchesInPatch)
            {
                if (VoxelMetaspace.metaspace.TryGetVoxelMesh(batchIndex))
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
            VoxelWorld.world.LoadOctreePatch(patchBytes, batchesInPatch);
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
            
            CameraControls.main.OnRegionLoad(startBatchIndex, endBatchIndex);
        }

        public void OnLoadBatchButton()
        {
            StartCoroutine(LoadBatch());
        }
        
        public IEnumerator LoadBatch() {

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
                if (VoxelMetaspace.metaspace.TryGetVoxelMesh(batchIndex))
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
            
            VoxelWorld.world.LoadRegion(startBatchIndex, endBatchIndex, moddedBatchesCheckbox.isOn);
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