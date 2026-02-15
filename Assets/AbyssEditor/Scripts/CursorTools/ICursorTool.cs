using AbyssEditor.Scripts.UI.HotBar;
namespace AbyssEditor.Scripts.CursorTools
{
    public interface ICursorTool
    {
        public void EnableTool();
        public void EnableTool(IHotBarButton hotBarButton);
        public void DisableTool();
        public void HandleToolUpdate(bool blockInput);
    }
}
