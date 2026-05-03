using System.Collections;
using System.Collections.Generic;
using AbyssEditor.Scripts.BatchOutline;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.InputMaps;
using AbyssEditor.Scripts.UI.HotBar.HotBarButtons;
using AbyssEditor.Scripts.UI.Windows;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssEditor.Scripts.CursorTools
{
    public class RemoveBatchTool : ICursorTool
    {
        private AbyssEditorInput.RemoveBatchActions input;

        private BatchOutline.BatchOutline hoveredOutline;

        private bool dialogOpen = false;
        
        public CursorTool toolType => CursorTool.RemoveBatch;
        
        public void Start()
        {
            input = InputManager.main.input.RemoveBatch;
            input.SelectBatch.performed += OnSelectBatchKeybind;
        }

        public void EnableTool([CanBeNull] HotBarButton hotBarButton)
        {
            input.Enable();
            List<Vector3Int> loadedBatches = new List<Vector3Int>();
            foreach (VoxelBatch voxelMesh in VoxelMetaspace.metaspace.batches.Values)
            {
                loadedBatches.Add(voxelMesh.batchIndex);
            }
            BatchOutlineManager.main.DrawBatchRemoveOutlines(loadedBatches);
        }
        
        public void DisableTool()
        {
            input.Disable();
            hoveredOutline = null;
            BatchOutlineManager.main.ResetOutlines();
        }

        public void HandleToolUpdate(bool blockInput)
        {
            if (blockInput)
            {
                if (!dialogOpen) hoveredOutline = null;
                BatchOutlineManager.main.HoverRemoveOutline(hoveredOutline);
                return;
            }
            
            hoveredOutline = GetHoveredBatchOutline();
            
            BatchOutlineManager.main.HoverRemoveOutline(hoveredOutline);
        }

        private BatchOutline.BatchOutline GetHoveredBatchOutline()
        {
            Ray ray = CameraControls.main.cam.ScreenPointToRay(input.MousePositon.ReadValue<Vector2>());
            
            Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, 1);

            if (!hit.collider || !hit.collider.gameObject.TryGetComponent(out BatchOutline.BatchOutline outlineCollider))
            {
                return null;
            }
            
            return outlineCollider;
        }

        private void OnSelectBatchKeybind(InputAction.CallbackContext ctx)
        {
            if (!hoveredOutline) return;
            
            Vector3 batchPosition = hoveredOutline.transform.parent.position;
            
            Vector3Int batchIndex = new Vector3Int(
                Mathf.FloorToInt(batchPosition.x / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt(batchPosition.y / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt(batchPosition.z / VoxelWorld.BATCH_WIDTH)
            );
            CursorToolManager.main.StartCoroutine(RemoveBatchConfirmation(batchIndex));
        }

        private IEnumerator RemoveBatchConfirmation(Vector3Int batchIndex)
        {
            dialogOpen = true;
            ConfirmationWindow.main.OpenWindow(
                Language.main.Get("BatchRemoveConfirmationMessage"),
                Language.main.Get("GenericCancel"),
                Language.main.Get("GenericConfirm"),
                out ConfirmationWindow.Response response
            );
                
            yield return new WaitUntil(() => response.receivedResponse);

            if (!response.response)
            {
                yield break;
            }
            
            yield return VoxelMetaspace.metaspace.RemoveBatch(batchIndex);
            
            BatchOutlineManager.main.ResetOutlines();
            EnableTool(null);
            dialogOpen = false;
        }
    }
}
