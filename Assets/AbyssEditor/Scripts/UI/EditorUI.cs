using System.Collections.Generic;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.UI.Windows;
using UnityEngine;

namespace AbyssEditor.Scripts.UI {
    public class EditorUI : MonoBehaviour {
        public static EditorUI inst { get; private set; }
        [SerializeField] private GameObject errorPrefab;
        [SerializeField] private GameObject mainContentArea;
        [SerializeField] private GameObject statusArea;
        [SerializeField] private Color[] uiColors;
        
        private readonly Dictionary<string, UIWindow> uiWindows = new();
        
        private RectTransform rt;

        private void Awake() {
            if (inst != null)
            {
                Debug.LogError($"An instance of {GetType().Name} is already created!");
                Destroy(gameObject);
            }
            inst = this;
            rt = GetComponent<RectTransform>();
        }

        private void Start()
        {
            GetAvailableWindows();
            
            if(string.IsNullOrEmpty(Preferences.data.gamePath))
            {
                EnableWindow("Settings");
            }
            else
            {
                EnableWindow("Import");
            }

            if (Preferences.data.autoLoadMaterials)
            {
                MaterialIconGenerator.main.GenerateMaterialIcons();
            }
        }

        private void GetAvailableWindows()
        {
            foreach (Transform child in mainContentArea.transform)
            {
                if (child.TryGetComponent(out UIWindow window))
                {
                    uiWindows.Add(window.windowKey, window);
                }
            }
        }
        
        public void EnableWindow(string key)
        {
            if (!uiWindows.TryGetValue(key, out UIWindow window)) {
                Debug.LogError($"Window Key ({key}) could not be associated with a window!");
                return;
            }
            window.EnableWindow();
        }

        public void ToggleWindow(string key)
        {
            if (!uiWindows.TryGetValue(key, out UIWindow window)) {
                Debug.LogError($"Window Key ({key}) could not be associated with a window!");
                return;
            }
            window.ToggleWindow();
        }
        
        public float InverseScaleMousePosToCanvas(float mousePositon)
        {
            return mousePositon * (1.0f / rt.localScale.x);
        }
        
        public void DisplayErrorMessage(string message)
        {
            DebugOverlay.LogError(message);
            GameObject go = Instantiate(errorPrefab, statusArea.transform);
            EditorErrorDisplay errorDisplay = go.GetComponent<EditorErrorDisplay>();
            errorDisplay.SetErrorText(message);
        }
        
        public void OnDestroy()
        {
            inst = null;
        }
    }
}