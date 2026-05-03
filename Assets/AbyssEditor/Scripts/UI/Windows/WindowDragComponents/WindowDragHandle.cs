using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts.UI.Windows.WindowDragComponents {
    [RequireComponent(typeof(RectTransform))]
    public class WindowDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private UIWindow window;
        private Vector3 offset;

        public void Start()
        {
            window = GetComponentInParent<UIWindow>();
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            window.PushToTop();

            Vector3 mousePos = eventData.position;
            offset = mousePos - window.GetWindowPos();
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 mousePos = eventData.position;
            Vector3 pos = mousePos - offset;
            pos = new Vector3(Mathf.Clamp(pos.x, 0, Screen.width), Mathf.Clamp(pos.y, 0, Screen.height), 0);
            window.Move(pos);
        }
    }
}