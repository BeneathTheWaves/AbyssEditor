using System;
using AbyssEditor.Scripts.InputMaps;
using AbyssEditor.Scripts.UI.Windows;
using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts.UI {
    public class WindowDrag : MonoBehaviour, IBeginDragHandler, IDragHandler {
        [SerializeField] private RectTransform windowTf;
        private Vector3 offset;
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            UIWindow window = GetComponentInParent<UIWindow>();
            window.PushToTop();

            Vector3 mousePos = eventData.position;
            offset = mousePos - windowTf.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 mousePos = eventData.position;
            Vector3 pos = mousePos - offset;
            pos = new Vector3(Mathf.Clamp(pos.x, 0, Screen.width), Mathf.Clamp(pos.y, 0, Screen.height), 0);
            windowTf.position = pos;
        }
    }
}