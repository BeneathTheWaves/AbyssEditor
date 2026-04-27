using AbyssEditor.Scripts.UI.HotBar.HotBarButtons;
using JetBrains.Annotations;

namespace AbyssEditor.Scripts.CursorTools
{
    /// <summary>
    /// Represents a tool that uses the cursor where only 1 should be active at a time
    /// </summary>
    public interface ICursorTool
    {
        /// <summary>
        /// Called once within the Start unity event. Use to initialize variables/input
        /// </summary>
        public void Start() {}

        /// <summary>
        /// Enable the tool.
        /// </summary>
        /// <param name="hotBarButton">The HotBarButton that was used to call this. You can extract data from that button if multiple slots point to 1 tool</param>
        public void EnableTool([CanBeNull] HotBarButton hotBarButton);
        
        /// <summary>
        /// Disable the tool
        /// </summary>
        public void DisableTool();
        
        /// <summary>
        /// Called every frame the tool is enabled. Not required to be implemented
        /// </summary>
        /// <param name="blockInput">If the input should be blocked, like being over ui or some script is blocking, but an update is still needed to keep the tool working</param>
        /// <param name="hideTool">If the tool should be hidden. Is only true if the cursor is over UI, in which case block input is true but this will also be. This does not trip for blocking scripts</param>
        public void HandleToolUpdate(bool blockInput) { }

        public CursorTool toolType { get; }
    }
}
