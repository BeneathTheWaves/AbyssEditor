using UnityEngine;

namespace AbyssEditor.Scripts.CursorTools.Brush
{
    public struct BrushStroke {
        public Vector3 brushLocation;
        public float brushRadius;
        public float squaredRadius;
        public float strength;
        public BrushMode brushMode;
        public int strokeLength;
        float lastBrushTime;
        
        private const float BRUSH_ACTION_PERIOD = .5f;

        // Stroke frequency increases with more strokes
        public void FirstStroke(Vector3 _position, float _radius, float _strength, BrushMode _mode) {
            strokeLength = 1;

            brushLocation = _position;
            brushRadius = _radius;
            squaredRadius = _radius * _radius;
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
