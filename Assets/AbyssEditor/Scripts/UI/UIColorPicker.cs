using System;
using System.Collections;
using System.Collections.Generic;
using hsvcolorpicker.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI
{
    public class UIColorPicker : MonoBehaviour
    {
        public Color color;
        [SerializeField] private bool expandParentOnColorPicker = true;
        [SerializeField] private ColorPicker colorPicker;
        public event Action OnColorChanged;
        
        private Button pickerButton;
        private Image pickerImage;
        
        private RectTransform parentRect;

        private void Awake()
        {
            parentRect = transform.parent.GetComponent<RectTransform>();
        }
    
        private void Start()
        {
            pickerButton = GetComponent<Button>();
            pickerButton.onClick.AddListener(OnButtonClick);
            pickerImage = GetComponent<Image>();
            colorPicker.onValueChanged.AddListener(OnPickerColorChanged);
        }

        public void SetInitialColor(Color initialColor)
        {
            color = initialColor;
            if (pickerImage != null)
            {
                pickerImage.color = initialColor;
            }
        }

        private void OnButtonClick()
        {
            if (colorPicker.isActiveAndEnabled)
            { 
                colorPicker.gameObject.SetActive(false);
                ShrinkParent();
            }
            else
            {
                EnablePicker();
            }
        }

        private void OnPickerColorChanged(Color newColor)
        {
            color = newColor;
            pickerImage.color = newColor;
            OnColorChanged?.Invoke();
        }
    
        private void EnablePicker()
        {
            colorPicker.gameObject.SetActive(true);
            colorPicker.CurrentColor = color;
            if (expandParentOnColorPicker) ExpandParentToFitNewSize();
        }

        private void ExpandParentToFitNewSize()
        {
            StartCoroutine(FixSizeNextFrame());
        }
        
        /*
         Yes this is kinda stupid, but it's the best we got. The color picker changes its size on initialization, so we got to wait a frame for it to be the right size we expect
         */
        private IEnumerator FixSizeNextFrame()
        {
            yield return null;

            float heightToAdd = colorPicker.GetComponent<RectTransform>().rect.height;
            parentRect.sizeDelta += new Vector2(0, heightToAdd);
        }
        
        private void ShrinkParent()
        {
            float heightToAdd = colorPicker.GetComponent<RectTransform>().rect.height;
            parentRect.sizeDelta -= new Vector2(0, heightToAdd);
        }
    }
}
