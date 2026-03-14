using System;
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

        private GameObject pickerAsset;
        private Button pickerButton;
        private Image pickerImage;
    
        public event Action onColorChanged;
    
        private GameObject pickerObject;

        private void Awake()
        {
            if (pickerAsset == null)
            {
                pickerAsset = Resources.Load<GameObject>("UI/Color Picker");
            }
        }
    
        private void Start()
        {
            pickerButton = GetComponent<Button>();
            pickerButton.onClick.AddListener(OnButtonClick);
        
            pickerImage = GetComponent<Image>();

        }

        public void SetInitialColor(Color initialColor)
        {
            color = initialColor;
            pickerImage.color = initialColor;
        }

        private void OnButtonClick()
        {
            if (pickerObject != null)
            {
                Destroy(pickerObject);
            }
            else
            {
                SpawnPicker();
            }
        }

        private void OnPickerColorChanged(Color newColor)
        {
            color = newColor;
            pickerImage.color = newColor;
            onColorChanged?.Invoke();
        }
    
        private void SpawnPicker()
        {
            pickerObject = Instantiate(pickerAsset, transform.parent);
            ColorPicker colorPickerInstance = pickerObject.GetComponent<ColorPicker>();
            colorPickerInstance.CurrentColor = color;
            colorPickerInstance.onValueChanged.AddListener(OnPickerColorChanged);
        }
    }
}
