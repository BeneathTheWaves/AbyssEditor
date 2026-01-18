using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UIWindow : MonoBehaviour {
        bool windowActive = false;
        public virtual void DisableWindow() {
            windowActive = false;
            gameObject.SetActive(windowActive);
        }
        public virtual void EnableWindow() {
            windowActive = true;
            PushToTop();
            gameObject.SetActive(windowActive);
        }
        public void ToggleWindow() {
            if (windowActive) DisableWindow();
            else EnableWindow();
        }

        public void PushToTop() {
            transform.SetAsLastSibling();
        }

        public static void ShowWindow(string buttonName)
        {
            Button[] buttons = FindObjectsOfType<Button>();

            foreach(Button button in buttons)
            {
                if(button.gameObject.name == buttonName)
                {
                    button.onClick.Invoke();
                    return;
                }
            }

            Debug.LogError($"Button: \"{buttonName}\" does not exist. Make sure the correct button name is being passed.");
        }
    }
}