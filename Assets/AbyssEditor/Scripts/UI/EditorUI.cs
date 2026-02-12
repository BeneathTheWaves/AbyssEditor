using System;
using System.Collections.Generic;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI {
    public class EditorUI : MonoBehaviour {
        public static EditorUI inst;
        public GameObject errorPrefab;
        public Color[] uiColors;

        [SerializeField] private GameObject inputBlocker;
        [SerializeField] private UIConfirmationWindow confirmationWindow;
        
        [SerializeField] private List<UIWindow> uiWindows;

        private void Awake() {
            inst = this;
            confirmationWindow.Initialize();
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

        public void EnableWindow<T>() where T : UIWindow
        {
            foreach (var window in uiWindows)
            {
                if (window.GetType() == typeof(T))
                {
                    window.ToggleWindow();
                }
            }
        }

        public void BlockUIInput()
        {
            inputBlocker.SetActive(true);
        }
        
        public void UnBlockUIInput()
        {
            inputBlocker.SetActive(false);
        }

        public static void DisplayErrorMessage(string message, NotificationType type = NotificationType.Error)
        {
            switch (type)
            {
                case NotificationType.Success:
                    DebugOverlay.LogMessage(message);
                    break;
                case NotificationType.Warning:
                    DebugOverlay.LogWarning(message);
                    break;
                case NotificationType.Error:
                    DebugOverlay.LogError(message);
                    break;
            }

            // clear previous error if it exists
            /*
            if (inst.statusBar.parent.childCount > 1)
            {
                Destroy(inst.statusBar.parent.GetChild(1).gameObject);
            }

            GameObject go = Instantiate(inst.errorPrefab, inst.statusBar.parent);
            go.transform.GetComponentInChildren<Text>().text = message;
            go.transform.GetChild(1).GetComponent<Image>().color = inst.uiColors[(int) type];
            go.transform.SetAsLastSibling();
            */
        }

        public enum NotificationType
        {
            Error,
            Warning,
            Success
        }
    }
}