using AbyssEditor.Scripts.BatchOutline;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.VoxelTech;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.Windows {
    public class UILoadWindow : UIWindow
    {
        public Carousel carouselLoadMethod;
        
        public GameObject optoctreeGroup;
        public TMP_InputField rangeStartInput;
        public TMP_InputField rangeEndInput;
        public Toggle moddedBatchesCheckbox;
        
        public GameObject optoctreePatchGroup;
        public Button selectFileButton;
        

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
            OnEndEditInputField();
        }
        
        public override void DisableWindow()
        {
            base.DisableWindow();
            BatchOutlineManager.main.ResetLoadOutlines();
        }
        
        private void ChangeLoadMethod(string method)
        {
            switch (method)
            {
                case "BaseGame":
                    optoctreeGroup.SetActive(true);
                    optoctreePatchGroup.SetActive(false);
                    break;
                case "TerrainPatcher":
                    optoctreeGroup.SetActive(false);
                    optoctreePatchGroup.SetActive(true);
                    break;
            }
        }

        public void OnSelectFileButton()
        {
            string[] paths = StandaloneFileBrowser.StandaloneFileBrowser.OpenFilePanel(Language.main.Get("PatchSelectFileBrowserTip"), Application.persistentDataPath, "", false);

            if (paths.Length == 0 || !paths[0].ToLower().EndsWith(".optoctreepatch"))
            {
                EditorUI.DisplayErrorMessage("Please select a valid file!");
                return;
            }
            
            Debug.Log(paths[0]);
            
            VoxelWorld.world.LoadOctreePatch(paths[0]);
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
        
        public void LoadBatch() {

            if (!Globals.CheckIsGamePathValid()) {
                EditorUI.DisplayErrorMessage("Please select a valid game path");
                return;
            }
            
            bool startEntered = TryParseBatchString(rangeStartInput.text, out Vector3Int startBatchIndex);
            bool endEntered = TryParseBatchString(rangeEndInput.text, out Vector3Int endBatchIndex);

            if (!startEntered && !endEntered) {
                EditorUI.DisplayErrorMessage("Please enter at least one batch index: \n\"x(space)y(space)z\"");
                return;
            }
            
            // assume user wants to load a single batch if only 1 is correct
            if (!startEntered) startBatchIndex = endBatchIndex; 
            if (!endEntered) endBatchIndex = startBatchIndex;
            
            
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