using System;
using System.Collections.Generic;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.InputMaps;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AbyssEditor.Scripts.UI.HotBar
{ 
    public class HotBar : MonoBehaviour
    {
        private List<IHotBarButton> buttons = new();
        
        private AbyssEditorInput input;

        private IHotBarButton currentButton;

        private void Awake()
        {
            input = new AbyssEditorInput();
            buttons = new List<IHotBarButton>(transform.GetComponentsInChildren<IHotBarButton>());
        }
        private void Start()
        {
            foreach (IHotBarButton button in buttons)
            {
                button.InitializeListener(OnAnyButtonPress);
            }

            RegisterHotKeyActions();
        }

        private void RegisterHotKeyActions()
        {            
            input.HotBar.Enable();
            
            input.HotBar.HotbarSelect.performed += OnNumberKeyDown;
        }

        private void OnNumberKeyDown(InputAction.CallbackContext ctx)
        {
            //Dont process input if typing
            if (EventSystem.current.currentSelectedGameObject != null &&
                EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() != null)
                return;
            
            int numberKey = int.Parse(ctx.control.name);

            int index;
            if (numberKey == 0)
            {
                index = 9;//Zero becomes 9 index wise
            }
            else
            {
                index = numberKey - 1;
            }

            if (index < 0 || index >= buttons.Count)
            {
                return;
            }
            
            OnAnyButtonPress(buttons[index]);
        }

        public void Update()
        {
            /*BrushMode _newActiveMode = userSelectedMode;
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

