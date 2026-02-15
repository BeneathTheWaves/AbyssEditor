using System;
using System.Collections.Generic;
using AbyssEditor.Scripts.CursorTools;
using UnityEngine;

namespace AbyssEditor.Scripts.UI.HotBar
{ 
    public class HotBar : MonoBehaviour
    {
        private List<IHotBarButton> buttons = new();

        private IHotBarButton currentButton;

        private void Awake()
        {
            buttons = new List<IHotBarButton>(transform.GetComponentsInChildren<IHotBarButton>());
        }
        private void Start()
        {
            foreach (IHotBarButton button in buttons)
            {
                button.InitializeListener(OnAnyButtonPress);
            }
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                OnAnyButtonPress(buttons[0]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                OnAnyButtonPress(buttons[1]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                OnAnyButtonPress(buttons[2]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                OnAnyButtonPress(buttons[3]);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                OnAnyButtonPress(buttons[4]);
            }
            
            /*
            BrushMode _newActiveMode = userSelectedMode;
            if (Input.GetKey(KeyCode.LeftShift)) {
                // Always smooth
                _newActiveMode = BrushMode.Smooth;
            } else if (Input.GetKey(KeyCode.LeftControl)) {
                // Complementary op
                if (userSelectedMode == BrushMode.Add)
                    _newActiveMode = BrushMode.Remove;
                if (userSelectedMode == BrushMode.Paint)
                    _newActiveMode = BrushMode.Eyedropper;
                if (userSelectedMode == BrushMode.Remove)
                    _newActiveMode = BrushMode.Add;
                if (userSelectedMode == BrushMode.Eyedropper)
                    _newActiveMode = BrushMode.Paint;
            }
            if (_newActiveMode != activeMode) {
                activeMode = _newActiveMode;
                OnParametersChanged?.Invoke();
            }*/
        }

        private void OnAnyButtonPress(IHotBarButton newButton)
        {
            if (currentButton == newButton)
            {
                currentButton.SetToggle(false);
                currentButton = null;
                CursorToolManager.main.DisableActiveTool();
                return;
            }
            
            currentButton?.SetToggle(false);
            
            currentButton = newButton;
            currentButton.SetToggle(true);
            
            CursorToolManager.main.Enable<BrushTool>(currentButton);
        }
    }
}

