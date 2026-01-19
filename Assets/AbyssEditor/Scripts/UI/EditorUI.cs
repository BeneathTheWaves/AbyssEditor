using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.UI {
    public class EditorUI : MonoBehaviour {
        public static EditorUI inst;
        [SerializeField] RectTransform statusBar;
        public GameObject errorPrefab;
        public Color[] uiColors;

        private void Awake() {
            inst = this;
        }

        private void Start()
        {
            UIWindow.ShowWindow("Button_LoadBatch");
        }

        public static void UpdateStatusBar(string title, float val) {
            inst.statusBar.gameObject.SetActive(true);

            inst.statusBar.GetChild(0).GetComponent<Text>().text = title;
            inst.statusBar.GetChild(1).GetComponent<UIProgressBar>().SetFill(val);
        }
        public static void DisableStatusBar() {
            inst.statusBar.gameObject.SetActive(false);
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
            if (inst.statusBar.parent.childCount > 1)
            {
                Destroy(inst.statusBar.parent.GetChild(1).gameObject);
            }

            GameObject go = Instantiate(inst.errorPrefab, inst.statusBar.parent);
            go.transform.GetComponentInChildren<Text>().text = message;
            go.transform.GetChild(1).GetComponent<Image>().color = inst.uiColors[(int) type];
            go.transform.SetAsLastSibling();
        }

        public enum NotificationType
        {
            Error,
            Warning,
            Success
            
        }
    }
}