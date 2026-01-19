using UnityEngine;

namespace AbyssEditor.UI {
    public class UIQuitWindow : UIWindow {
        public void CloseApp() {
            Application.Quit();
        }
    }
}
