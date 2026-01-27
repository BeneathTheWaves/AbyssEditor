using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        pickerImage.color = color;
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
        pickerObject = Instantiate(pickerAsset, this.transform.parent);
        HSVPicker.ColorPicker colorPickerInstance = pickerObject.GetComponent<HSVPicker.ColorPicker>();
        colorPickerInstance.CurrentColor = color;
        colorPickerInstance.onValueChanged.AddListener(OnPickerColorChanged);
    }
    
    bool IsPointerOverPicker()
    {
        PointerEventData data = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(data, results);

        foreach (var r in results)
        {
            if (r.gameObject.transform.IsChildOf(pickerObject.transform))
                return true;
        }

        return false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("OnPointerClick");
    }
}
