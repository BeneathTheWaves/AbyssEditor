using System.Collections.Generic;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.InputMaps;
using AbyssEditor.Scripts.UI.HotBar.HotBarButtons;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AbyssEditor.Scripts.UI.HotBar
{ 
    public class HotBar : MonoBehaviour
    {
        private List<HotBarButton> buttons = new();
        
        private AbyssEditorInput.HotBarActions input;

        private HotBarButton currentButton;

        private void Awake()
        {
            input = new AbyssEditorInput().HotBar;
            buttons = new List<HotBarButton>(transform.GetComponentsInChildren<HotBarButton>());
        }
        private void Start()
        {
            foreach (HotBarButton button in buttons)
            {
                button.InitializeListener(OnAnyButtonPress);
            }

            RegisterHotKeyActions();
        }

        private void RegisterHotKeyActions()
        {            
            input.Enable();
            
            input.HotbarSelect.performed += OnNumberKeyDown;
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

        private void OnAnyButtonPress(HotBarButton newButton)
        {
            if (currentButton == newButton)
            {
                currentButton.SetToggle(false);
                currentButton = null;
                CursorToolManager.main.DisableActiveTool();
                return;
            }

            if (currentButton && currentButton.GetType() != newButton.GetType())
            {
                CursorToolManager.main.DisableActiveTool();
            }
            
            currentButton?.SetToggle(false);
            
            currentButton = newButton;
            currentButton.SetToggle(true);
            CursorToolManager.main.EnableTool(currentButton.cursorToolType, currentButton);
        }
    }
}

