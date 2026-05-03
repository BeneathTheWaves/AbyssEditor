using System;
using AbyssEditor.Scripts.CursorTools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI {
    public class UIHybridInput : MonoBehaviour, IDragHandler, IEndDragHandler, IPointerClickHandler {

        public float lerpedValue {
            get => Mathf.Lerp(minValue, maxValue, sliderValue);
            private set => sliderValue = Mathf.InverseLerp(minValue, maxValue, value);
        }
        
        public float minValue = 0;
        public float maxValue = 1;

        public delegate string UIFormatFunction(float val);
        public UIFormatFunction formatFunction;
        
        private float sliderValue;
        private float realWidth;
        private TMP_InputField field;
        private RectTransform rectTransform;

        private RectTransform bar;
        private const float PADDING = 3;

        public event Action OnValueUpdated;
        public event Action OnEndDragging;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            bar = transform.Find("Bar").Find("fill") as RectTransform;
                
            field = GetComponentInChildren<TMP_InputField>();
            field.onEndEdit.AddListener(DisableInputField);
            OnValueUpdated += Redraw;
        }

        private void OnRectTransformDimensionsChange()
        {
            realWidth = rectTransform.rect.width * rectTransform.lossyScale.x;
            Redraw();
        }
        
        private void Start() {
            field.enabled = false;
            OnRectTransformDimensionsChange();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            field.enabled = true;
            field.Select();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            float barStart = transform.position.x - realWidth / 2f;
            sliderValue = Mathf.Clamp01((eventData.position.x - barStart) / realWidth);
            OnValueUpdated?.Invoke();
            CursorToolManager.main.RegisterInputBlock(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            OnEndDragging?.Invoke();
            CursorToolManager.main.UnregisterInputBlock(this);
        }

        public void SetValue(float lerpedVal) {
            lerpedValue = lerpedVal;
            Redraw();
        }

        private void DisableInputField(string val) {
            if (float.TryParse(val, out float lerpedVal))
                lerpedValue = lerpedVal;
            field.enabled = false;
            
            OnValueUpdated?.Invoke();
        }
        
        private void Redraw() {
            bar.offsetMin = new Vector2(PADDING, PADDING);
            
            float rightOffset = -PADDING - (rectTransform.rect.width * (1 - sliderValue));
            
            bar.offsetMax = new Vector2(rightOffset, -PADDING);

            if (formatFunction != null)
            {
                field.SetTextWithoutNotify(formatFunction(lerpedValue));
            }
        }
    }
}
