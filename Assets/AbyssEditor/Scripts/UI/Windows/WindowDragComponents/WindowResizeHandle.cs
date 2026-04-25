
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts.UI.Windows.WindowDragComponents
{
    [RequireComponent(typeof(RectTransform))]
    public class WindowResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField] private List<HandleLocation> locations;
        private UIWindow window;
        private Vector2 dragStartScreenPos;

        public void Start()
        {
            window = GetComponentInParent<UIWindow>();
            
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragStartScreenPos = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            float deltaLeft = 0;
            float deltaRight = 0;
            float deltaBottom = 0;
            float deltaTop = 0;


            foreach (HandleLocation handleLocation in locations)
            {
                switch (handleLocation)
                {
                    case HandleLocation.Left:
                        deltaLeft = HandleHorizontalDrag(eventData.position);
                        break;
                    case HandleLocation.Right:
                        deltaRight = HandleHorizontalDrag(eventData.position);
                        break;
                    case HandleLocation.Bottom:
                        deltaBottom = HandleVerticalDrag(eventData.position);
                        break;
                    case HandleLocation.Top:
                        deltaTop = HandleVerticalDrag(eventData.position);
                        break;
                }
            }

            dragStartScreenPos = eventData.position;
    
            window.UpdateWindowSize(deltaLeft, deltaTop, deltaBottom, deltaRight);
        }
        private float HandleHorizontalDrag(Vector2 mousePositon) => EditorUI.inst.InverseScaleMousePosToCanvas(mousePositon.x - dragStartScreenPos.x);

        private float HandleVerticalDrag(Vector2 mousePositon) => EditorUI.inst.InverseScaleMousePosToCanvas(mousePositon.y - dragStartScreenPos.y);

        
        private enum HandleLocation
        {
            Left,
            Right,
            Bottom,
            Top
        }
    }
}
