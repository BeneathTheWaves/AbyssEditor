using System;
using AbyssEditor.Scripts.CursorTools.Brush;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.HotBar.HotBarButtons
{
    public class HotBarButton: MonoBehaviour
    {
        private Toggle toggle;
        private Action<HotBarButton> hotBarOnPress;

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
