using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AbyssEditor.Scripts.UI {
    public class UIHybridInput : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerClickHandler {

        public float LerpedValue {
            get {
                return Mathf.Lerp(minValue, maxValue, sliderValue);
            }
            set {
                sliderValue = Mathf.InverseLerp(minValue, maxValue, value);
            }
        }
        
        public float minValue = 0;
        public float maxValue = 1;

        public delegate string UIFormatFunction(float val);
        public UIFormatFunction formatFunction;
        
        private float sliderValue;
        float realWidth;
        TMP_InputField field;
        RectTransform rectTransform;
        
        RectTransform bar;
        private readonly float padding = 3;

        public event Action OnValueUpdated;
        public event Action OnEndDragging;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            OnRectTransformDimensionsChange();
            
            bar = transform.Find("Bar").Find("fill") as RectTransform;
                
            field = GetComponentInChildren<TMP_InputField>();
            field.onEndEdit.AddListener(DisableInputField);
            OnValueUpdated += Redraw;
        }
        
        void OnRectTransformDimensionsChange()
        {
            realWidth = rectTransform.rect.width * rectTransform.lossyScale.x;
        }
        
        private void Start() {
            field.enabled = false;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            float barStart = transform.position.x - realWidth / 2f;
            
            sliderValue = Mathf.Clamp01((eventData.position.x - barStart) / realWidth);
            
            OnValueUpdated?.Invoke();
        }

        public void OnPointerClick(PointerEventData eventData) {
            if (!eventData.dragging) {
                field.enabled = true;
                field.Select();
            }
        }

        public void SetValue(float lerpedVal) {
            LerpedValue = lerpedVal;
            Redraw();
        }

        private void DisableInputField(string val) {
            if (float.TryParse(val, out float lerpedVal))
                LerpedValue = lerpedVal;
            field.enabled = false;
            
            OnValueUpdated?.Invoke();
        }

        //
        private void Redraw() {
            bar.offsetMin = new Vector2(padding, padding);
            
            float rightOffset = -padding - (rectTransform.rect.width * (1 - sliderValue));
            
            bar.offsetMax = new Vector2(rightOffset, -padding);

            field.SetTextWithoutNotify(formatFunction(LerpedValue));
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragging?.Invoke();
        }
    }
}
