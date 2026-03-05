using System.Collections.Generic;
using AbyssEditor.Scripts.InputMaps;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssEditor.Scripts.CursorTools
{
    public class SizeRefTool : ICursorTool
    {
        private GameObject sizeRefGhostPrefab;
        private GameObject sizeRefPrefab;
        
        private GameObject sizeRefGhostInstance;
        private GameObject sizeRefInstance;

        private readonly List<GameObject> strays = new();

        private AbyssEditorInput.SizeRefToolActions input;

        private float rotationAngle;

        private const float ROTATE_SPEED = 10;
        
        public void Start()
        {
            sizeRefGhostPrefab = Resources.Load<GameObject>("SizeRefGhostPrefab");
            sizeRefPrefab = Resources.Load<GameObject>("SizeRefPrefab");
            input = InputManager.main.input.SizeRefTool;
            input.Place.performed += PlaceSizeRef;
            input.Rotation.performed += OnRotate;
        }

        public void EnableTool()
        {
            input.Enable();
            sizeRefGhostInstance = Object.Instantiate(sizeRefGhostPrefab);
        }

        public void DisableTool()
        {
            input.Disable();
            Object.Destroy(sizeRefGhostInstance);
        }

        private void PlaceSizeRef(InputAction.CallbackContext ctx)
        {
            if (!TryGetPlacementHit(out RaycastHit hit)) return;

            bool allowMultiple = input.AllowPlacingMultiple.IsPressed();

            GameObject placedInstance = null;
            
            // If multiple size refs are not allowed, just re-use the current one
            if (!allowMultiple)
            {
                placedInstance = sizeRefInstance;   
            }
            else if (sizeRefInstance != null) // Otherwise, if placing multiple, convert the old one to a stray
            {
                strays.Add(sizeRefInstance);
            }
            
            if (placedInstance == null)
            {
                placedInstance = Object.Instantiate(sizeRefPrefab);
            }

            PositionTransform(placedInstance.transform, hit);
            sizeRefInstance = placedInstance;

            // Clear strays if needed
            if (!allowMultiple && strays.Count > 0)
            {
                foreach (var stray in strays)
                {
                    Object.Destroy(stray);
                }
                strays.Clear();
            }
        }

        private void PositionTransform(Transform transform, RaycastHit hit)
        {
            transform.position = hit.point;
            transform.up = hit.normal;
            transform.Rotate(new Vector3(0, rotationAngle, 0), Space.Self);
        }

        private void OnRotate(InputAction.CallbackContext ctx)
        {
            rotationAngle += ctx.ReadValue<float>() * ROTATE_SPEED;
        }

        public void HandleToolUpdate(bool blockInput)
        {
            if (blockInput)
                return;

            bool ghostHit = TryGetPlacementHit(out RaycastHit hit);
            
            sizeRefGhostInstance.SetActive(ghostHit);
            
            if (ghostHit)
            {
                PositionTransform(sizeRefGhostInstance.transform, hit);
            }
        }

        private bool TryGetPlacementHit(out RaycastHit hit)
        {
            var ray = Camera.main.ScreenPointToRay(input.MousePosition.ReadValue<Vector2>());
            return Physics.Raycast(ray, out hit, Mathf.Infinity, 1);
        }
    }
}