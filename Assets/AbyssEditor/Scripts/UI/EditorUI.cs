using System;
using System.Collections.Generic;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI {
    public class EditorUI : MonoBehaviour {
        public static EditorUI inst { get; private set; }
        [SerializeField] private GameObject errorPrefab;
        [SerializeField] private GameObject statusArea;
        [SerializeField] private Color[] uiColors;
        
        [SerializeField] private UIConfirmationWindow confirmationWindow;
        
        [SerializeField] private List<UIWindow> uiWindows;

        private void Awake() {
            inst = this;
        }

        private void Start()
        {
            if(string.IsNullOrEmpty(Preferences.data.gamePath))
            {
                EnableWindow<UISettingsWindow>();
            }
            else
            {
                EnableWindow<UILoadWindow>();
            }

            if (Preferences.data.autoLoadMaterials)
            {
                MaterialIconGenerator.main.GenerateMaterialIcons();
            }
        }

        private void EnableWindow<T>() where T : UIWindow
        {
            foreach (var window in uiWindows)
            {
                if (window.GetType() == typeof(T))
                {
                    window.ToggleWindow();
                }
            }
        }
        
        public void DisplayErrorMessage(string message)
        {
            DebugOverlay.LogError(message);
            GameObject go = Instantiate(errorPrefab, statusArea.transform);
            EditorErrorDisplay errorDisplay = go.GetComponent<EditorErrorDisplay>();
            errorDisplay.SetErrorText(message);
        }
    }
}