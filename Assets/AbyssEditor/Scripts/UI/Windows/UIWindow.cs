using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
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
        public virtual void UpdateScale()
        {
            transform.localScale = Vector3.one * 1;
        }
        public void ToggleWindow() {
            if (windowActive) DisableWindow();
            else EnableWindow();
        }

        public void PushToTop() {
            transform.SetAsLastSibling();
        }
    }
}