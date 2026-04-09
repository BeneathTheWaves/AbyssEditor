using System;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.HotBar.HotBarButtons
{
    public class HotBarButton: MonoBehaviour
    {
        private Toggle toggle;
        private Action<HotBarButton> hotBarOnPress;
        [field: SerializeField] public CursorTool cursorToolType { get; private set; } = CursorTool.None; 

        public void Awake()
        {
            toggle = GetComponent<Toggle>();
        }
        
        public void SetToggle(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }

        public void InitializeListener(Action<HotBarButton> callback)
        {
            hotBarOnPress = callback;
            toggle.onValueChanged.AddListener(OnValueChanged);
        }
        
        private void OnValueChanged(bool value)
        {
            hotBarOnPress?.Invoke(this);
        }
    }
}
