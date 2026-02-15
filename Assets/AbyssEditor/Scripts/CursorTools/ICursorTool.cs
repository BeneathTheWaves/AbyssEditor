using AbyssEditor.Scripts.UI.HotBar;
using AbyssEditor.Scripts.UI.HotBar.HotBarButtons;
namespace AbyssEditor.Scripts.CursorTools
{
    public interface ICursorTool
    {
        public void EnableTool();
        public void EnableTool(HotBarButton hotBarButton);
        public void DisableTool();
        public void HandleToolUpdate(bool blockInput);
    }
}
