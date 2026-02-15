using System;
using AbyssEditor.Scripts.InputMaps;
using AbyssEditor.Scripts.UI.HotBar;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AbyssEditor.Scripts.CursorTools {
    public class BrushTool : ICursorTool {
        public const float MIN_BRUSH_SIZE = 1;
        public const float MAX_BRUSH_SIZE = 32;
        private const float MIN_BRUSH_STRENGTH = 0;
        private const float MAX_BRUSH_STRENGTH = 1;

        private const float SIZE_SCROLL_SPEED = 1f;
        private const float STRENGTH_SCROLL_SPEED = 0.05f;
        
        
        public float currentBrushSize = 10;
        public byte currentSelectedType = 1;
        public float currentBrushStrength = 0.5f;
        
        public event Action OnParametersChanged;
        
        private BrushMode activeMode;
        private BrushStroke stroke;
        private const float BRUSH_ACTION_PERIOD = 1.0f;
        
        private AbyssEditorInput.BrushActions input = new AbyssEditorInput().Brush;
        private GameObject brushAreaObject;

        public BrushTool()
        {
            input.ScrollWheelScale.performed += OnScrollWheel;
        }

        public void EnableTool() {
            input.Enable();
            if (brushAreaObject == null) {
                CreateBrushObject();
                DisableBrushGizmo();
            }
        }
        
        public void EnableTool(IHotBarButton hotBarButton) {
            input.Enable();
            BrushHotBarButton brushbutton = hotBarButton as BrushHotBarButton;
            SetBrushMode(brushbutton.GetBrushMode());
            EnableTool();
        }
        
        public void DisableTool() {
            input.Disable();
            DisableBrushGizmo();
        }
        
        public void SetBrushMaterial(byte value) {
            currentSelectedType = value;
            OnParametersChanged?.Invoke();
        }
        
        public void SetBrushSize(float t) {
            currentBrushSize = t;
            Light light = brushAreaObject.GetComponentInChildren<Light>();
            light.intensity = Mathf.Clamp(Mathf.Sqrt(currentBrushSize), 1, 12); ;
            light.range = 2 * currentBrushSize;
            OnParametersChanged?.Invoke();
        }
        
        public void SetBrushStrength(float t) {
            currentBrushStrength = t;
            OnParametersChanged?.Invoke();
        }

        public Light GetBrushLight() {
            if (brushAreaObject == null) {
                CreateBrushObject();
                DisableBrushGizmo();
            }
            return brushAreaObject.GetComponentInChildren<Light>();
        }
        
        public void HandleToolUpdate(bool blockInput)
        {
            if (blockInput)
            {
                DisableTool();
                return;
            }
            
            EnableTool();
            BrushAction(input.ActivateBrush.IsPressed());
        }

        private void OnScrollWheel(InputAction.CallbackContext ctx)
        {
            ctx.ReadValue<float>();
            if (input.ActivateBrushScale.IsPressed())
            {
                float newSize = currentBrushSize + (ctx.ReadValue<float>() * SIZE_SCROLL_SPEED);
                newSize = Mathf.Clamp(newSize, MIN_BRUSH_SIZE, MAX_BRUSH_SIZE);
                SetBrushSize(newSize);
            }
            if (input.ActivateBrushStrengthScale.IsPressed())
            {
                float newSize = currentBrushStrength + (ctx.ReadValue<float>() * STRENGTH_SCROLL_SPEED);
                newSize = Mathf.Clamp(newSize, MIN_BRUSH_STRENGTH, MAX_BRUSH_STRENGTH);
                SetBrushStrength(newSize);
            }
        }


        private void BrushAction(bool doAction) {
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            Physics.Raycast(ray, out hit, Mathf.Infinity, 1);

            if (!hit.collider)
            {
                DisableBrushGizmo();
                return;
            }

            EnableBrushGizmo(hit.point, hit.normal);

            if (!doAction)
            {
                stroke.EndStroke();
                return;
            }

            if (activeMode == BrushMode.Eyedropper) {
                SetBrushMaterial(VoxelWorld.world.SampleBlocktype(hit.point, ray));
                return;
            } 
            
            if (stroke.ReadyForNextAction()) {

                if (stroke.strokeLength == 0) stroke.FirstStroke(hit.point, currentBrushSize, currentBrushStrength, activeMode);
                else stroke.ContinueStroke(hit.point, activeMode);

                VoxelMetaspace.metaspace.ApplyJobBasedDensityAction(stroke);
            }
        }
        
        private void EnableBrushGizmo(Vector3 position, Vector3 normal) {

            brushAreaObject.SetActive(true);
            brushAreaObject.transform.position = position;
            brushAreaObject.transform.GetChild(0).position = position + normal * 2;

            if (activeMode == BrushMode.Eyedropper) {
                brushAreaObject.transform.localScale = Vector3.one * 2;
            } else {
                brushAreaObject.transform.localScale = Vector3.one * (2 * currentBrushSize);
            }
        }
        
        private void CreateBrushObject() {
            brushAreaObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            brushAreaObject.GetComponent<MeshRenderer>().sharedMaterial = Globals.instance.brushGizmoMat;
            brushAreaObject.GetComponent<SphereCollider>().enabled = false;
            brushAreaObject.transform.localScale = Vector3.one * currentBrushSize;

            GameObject lightObj = new GameObject("brushLight");
            lightObj.transform.SetParent(brushAreaObject.transform);
            lightObj.transform.localScale = Vector3.one;
            Light light = lightObj.AddComponent<Light>();
            light.enabled = false;
            light.intensity = Mathf.Clamp(Mathf.Sqrt(currentBrushSize), 1, 12);
            light.range = 2 * currentBrushSize;
        }
        
        private void DisableBrushGizmo() {
            if (brushAreaObject)
                brushAreaObject.SetActive(false);
            UpdateBoundaries(Vector3.zero, 0);
        }
        
        private void UpdateBoundaries(Vector3 newPos, float radius) {
            Globals.instance.boundaryGizmoMat.SetVector("_CursorWorldPos", newPos);
            Globals.instance.boundaryGizmoMat.SetFloat("_BlendRadius", radius);
        }
        
        private void SetBrushMode(BrushMode brushMode)
        {
            activeMode = brushMode;
            //if (selection < Globals.instance.brushColors.Length)
            //    Globals.instance.brushGizmoMat.color = Globals.instance.brushColors[selection];
            OnParametersChanged?.Invoke();
        }

        public struct BrushStroke {
            public Vector3 brushLocation;
            public float brushRadius;
            public float strength;
            public BrushMode brushMode;
            public int strokeLength;
            float lastBrushTime;

            // Stroke frequency increases with more strokes
            public void FirstStroke(Vector3 _position, float _radius, float _strength, BrushMode _mode) {
                strokeLength = 1;

                brushLocation = _position;
                brushRadius = _radius;
                brushMode = _mode;
                strength = _strength;
                
                lastBrushTime = Time.time;
            }
            
            public void ContinueStroke(Vector3 newPos, BrushMode newMode) {
                strokeLength++;

                brushLocation = newPos;
                brushMode = newMode;

                lastBrushTime = Time.time;
            }
            
            public void EndStroke() {
                strokeLength = 0;

                brushLocation = Vector3.zero;
                
                lastBrushTime = 0;
            }

            public bool ReadyForNextAction() {
                return (Time.time - lastBrushTime) >= (BRUSH_ACTION_PERIOD / (2 * Mathf.Clamp(strokeLength, 1, 5)));
            }
        }
    }

    /* Brush types:
    add/remove
    paint material / select material?
    flatten surface
    smooth available always with SHIFT */
    public enum BrushMode {
        Add,
        Remove,
        Paint,
        Eyedropper,
        Flatten,
        Smooth,
    }
}