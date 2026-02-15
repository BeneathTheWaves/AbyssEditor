using System;
using AbyssEditor.Scripts.UI.HotBar;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;

namespace AbyssEditor.Scripts.CursorTools {
    public class BrushTool : ICursorTool {
        public float currentBrushSize = 10;
        
        public const float MIN_BRUSH_SIZE = 1;
        public const float MAX_BRUSH_SIZE = 32;
        
        public byte currentSelectedType = 1;
        public float currentBrushStrength = 0.5f;
        
        private BrushMode activeMode;
        
        public event Action OnParametersChanged;

        private BrushStroke stroke;
        private const float BRUSH_ACTION_PERIOD = 1.0f;

        GameObject brushAreaObject;
        
        public void EnableTool() {
            if (brushAreaObject == null) {
                CreateBrushObject();
                DisableBrushGizmo();
            }
        }
        
        public void EnableTool(IHotBarButton hotBarButton) {
            BrushHotBarButton brushbutton = hotBarButton as BrushHotBarButton;
            SetBrushMode(brushbutton.GetBrushMode());
            EnableTool();
        }
        
        public void DisableTool() {
            DisableBrushGizmo();
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

        public Light GetBrushLight() {
            if (brushAreaObject == null) {
                CreateBrushObject();
                DisableBrushGizmo();
            }
            return brushAreaObject.GetComponentInChildren<Light>();
        }
        
        //TODO: MOVE THIS INPUT HANDLING INTO HOTBAR CLASS
        public void HandleToolUpdate(bool mouseOverUI) {

            if (mouseOverUI)
            {
                DisableTool();
                return;
            }
            
            BrushAction(Input.GetMouseButton(0));
        }


        public void BrushAction(bool doAction) {
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit;
            Physics.Raycast(ray, out hit, Mathf.Infinity, 1);

            if (hit.collider) {
                EnableBrushGizmo(hit.point, hit.normal);
                if (doAction) {
                    if (activeMode == BrushMode.Eyedropper) {
                        SetBrushMaterial(VoxelWorld.world.SampleBlocktype(hit.point, ray));
                    } else {
                        if (stroke.ReadyForNextAction()) {

                            if (stroke.strokeLength == 0) stroke.FirstStroke(hit.point, currentBrushSize, currentBrushStrength, activeMode);
                            else stroke.ContinueStroke(hit.point, activeMode);

                            VoxelMetaspace.metaspace.ApplyJobBasedDensityAction(stroke);
                        }
                    }
                } else {
                    stroke.EndStroke();
                }
                UpdateBoundaries(hit.point, currentBrushSize + 2);
            } else {
                DisableBrushGizmo();
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
        private void DisableBrushGizmo() {
            if (brushAreaObject)
                brushAreaObject.SetActive(false);
            UpdateBoundaries(Vector3.zero, 0);
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