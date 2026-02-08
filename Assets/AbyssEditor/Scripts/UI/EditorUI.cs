using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI {
    public class EditorUI : MonoBehaviour {
        public static EditorUI inst;
        public GameObject errorPrefab;
        public Color[] uiColors;

        private void Awake() {
            inst = this;
        }

        private void Start()
        {
            if(string.IsNullOrEmpty(Preferences.data.gamePath))
            {
                UIWindow.ShowWindow("Button_Settings");
            }
            else
            {
                UIWindow.ShowWindow("Button_LoadBatch");
            }

            if (Preferences.data.autoLoadMaterials)
            {
                MaterialIconGenerator.main.GenerateMaterialIcons();
            }
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