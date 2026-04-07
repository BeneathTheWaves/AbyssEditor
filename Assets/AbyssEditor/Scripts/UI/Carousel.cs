using System;
using System.Collections.Generic;
using AbyssEditor.Scripts.Essentials;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI
{
    public class Carousel : MonoBehaviour
    {
        [SerializeField] private List<String> optionsLanguageKeys;
        private int selectedElement = 0;

        private Button leftButton;
        private Button rightButton;
        private TextMeshProUGUI optionsText;

        public Action<string> onOptionSelected;

        public string GetSelectedElementLanguageKey()
        {
            return optionsLanguageKeys[selectedElement];
        }
        
        private void Start()
        {
            if (optionsLanguageKeys == null || optionsLanguageKeys.Count <= 0)
            {
                Debug.LogError("No options set for carousel!!! for " + transform.name);
                gameObject.SetActive(false);
                return;
            }
        
            leftButton = transform.Find("LeftButton").GetComponent<Button>();
            leftButton.onClick.AddListener(OnLeftButton);
            rightButton = transform.Find("RightButton").GetComponent<Button>();
            rightButton.onClick.AddListener(OnRightButton);
        
            optionsText = transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            optionsText.text = Language.main.Get(optionsLanguageKeys[selectedElement]);
        }

        private void OnLeftButton()
        {
            if (selectedElement == 0)
            {
                selectedElement = optionsLanguageKeys.Count - 1;
            }
            else
            {
                selectedElement--;
            }
            onOptionSelected.Invoke(optionsLanguageKeys[selectedElement]);
            optionsText.text = Language.main.Get(optionsLanguageKeys[selectedElement]);
        }
    
        private void OnRightButton()
        {
            if (selectedElement == optionsLanguageKeys.Count - 1)
            {
                selectedElement = 0;
            }
            else
            {
                selectedElement++;
            }
            onOptionSelected.Invoke(optionsLanguageKeys[selectedElement]);
            optionsText.text = Language.main.Get(optionsLanguageKeys[selectedElement]);
        }
    }
}
