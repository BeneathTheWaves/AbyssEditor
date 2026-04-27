
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts.UI.Windows.WindowDragComponents
{
    [RequireComponent(typeof(RectTransform))]
    public class WindowResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private HandleSide locations;
        private UIWindow window;
        private Vector2 dragStartScreenPos;

        private static Texture2D verticalCursorTexture;
        private static Texture2D horizontalCursorTexture;
        private static Texture2D diagonalUpwardCursorTexture;
        private static Texture2D diagonalDownwardCursorTexture;
        
        private static bool isCursorHovered;
        private static bool isDragging;
        private static bool texturesInitialized = false;

        
        [Flags]
        private enum HandleSide
        {
            None = 0,
            Left = 1, 
            Right = 2, 
            Top = 4,
            TopLeft = 5,
            TopRight = 6,
            Bottom = 8,
            BottomLeft = 9,
            BottomRight = 10
        }

        public void Start()
        {
            window = GetComponentInParent<UIWindow>();
            if (!texturesInitialized)
            {
                verticalCursorTexture = Resources.Load<Texture2D>("Crosshairs/Cursor_Drag_Vertical");
                horizontalCursorTexture = Resources.Load<Texture2D>("Crosshairs/Cursor_Drag_Horizontal");
                diagonalUpwardCursorTexture = Resources.Load<Texture2D>("Crosshairs/Cursor_Drag_Diagonal_Upward");
                diagonalDownwardCursorTexture = Resources.Load<Texture2D>("Crosshairs/Cursor_Drag_Diagonal_Downward");
                
                texturesInitialized = true;
            }
        }

        // Drag
        public void OnBeginDrag(PointerEventData eventData)
        {
            dragStartScreenPos = eventData.position;
            isDragging = true;
            SetDragCursor();
        }
        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            if(!isCursorHovered) ResetCursor();
        }
        
        // Hover
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isDragging) return;
            
            isCursorHovered = true;
            SetDragCursor();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            if (isDragging) return;
            
            isCursorHovered = false;
            if(!isDragging) ResetCursor();
        }

        public void OnDrag(PointerEventData eventData)
        {
            float deltaLeft = 0;
            float deltaRight = 0;
            float deltaBottom = 0;
            float deltaTop = 0;

            if (locations.HasFlag(HandleSide.Left))
            {
                deltaLeft = HandleHorizontalDrag(eventData.position);
            }
            if (locations.HasFlag(HandleSide.Right))
            {
                deltaRight = HandleHorizontalDrag(eventData.position);
            }
            if (locations.HasFlag(HandleSide.Bottom))
            {
                deltaBottom = HandleVerticalDrag(eventData.position);
            }
            if (locations.HasFlag(HandleSide.Top))
            {
                deltaTop = HandleVerticalDrag(eventData.position);
            }
            
            dragStartScreenPos = eventData.position;
    
            window.UpdateWindowSize(deltaLeft, deltaTop, deltaBottom, deltaRight);
        }
        private float HandleHorizontalDrag(Vector2 mousePositon) => EditorUI.inst.InverseScaleMousePosToCanvas(mousePositon.x - dragStartScreenPos.x);

        private float HandleVerticalDrag(Vector2 mousePositon) => EditorUI.inst.InverseScaleMousePosToCanvas(mousePositon.y - dragStartScreenPos.y);

        private void SetDragCursor()
        {
            switch (locations)
            {
                case HandleSide.Left:
                case HandleSide.Right:
                    Cursor.SetCursor(horizontalCursorTexture, new Vector2(16, 16), CursorMode.Auto);
                    break;
                case HandleSide.Top:
                case HandleSide.Bottom:
                    Cursor.SetCursor(verticalCursorTexture, new Vector2(16, 16), CursorMode.Auto);
                    break;
                case HandleSide.TopLeft:
                case HandleSide.BottomRight:
                    Cursor.SetCursor(diagonalDownwardCursorTexture, new Vector2(16, 16), CursorMode.Auto);
                    break;
                case HandleSide.BottomLeft:
                case HandleSide.TopRight:
                    Cursor.SetCursor(diagonalUpwardCursorTexture, new Vector2(16, 16), CursorMode.Auto);
                    break;
                default:
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                    break;
            }
            
        }

        private static void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}
