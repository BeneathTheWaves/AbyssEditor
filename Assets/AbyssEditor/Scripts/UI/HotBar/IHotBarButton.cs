using System;

namespace AbyssEditor.Scripts.UI.HotBar
{
    public interface IHotBarButton
    {
        public void InitializeListener(Action<IHotBarButton> callback);
        public void SetToggle(bool isOn);
    }
}
