using UnityEngine;
namespace AbyssEditor.Scripts.UI.Windows {
    public class UIQuitWindow : UIWindow {
        public void CloseApp() {
            Application.Quit();
        }
    }
}
