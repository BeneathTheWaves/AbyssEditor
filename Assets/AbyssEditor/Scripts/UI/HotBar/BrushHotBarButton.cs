using System;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.HotBar
{
    public class BrushHotBarButton : MonoBehaviour, IHotBarButton
    {
        private Toggle toggle;
        
        [SerializeField] private BrushMode brushMode;

        private Action<IHotBarButton> hotBarOnPress;

        public void Awake()
        {
            toggle = GetComponent<Toggle>();
        }

        private void OnValueChanged(bool value)
        {
            hotBarOnPress?.Invoke(this);
        }

        public void InitializeListener(Action<IHotBarButton> callback)
        {
            hotBarOnPress = callback;
            toggle.onValueChanged.AddListener(OnValueChanged);
        }
        public void SetToggle(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }

        public BrushMode GetBrushMode()
        {
            return brushMode;
        }
    }
}
