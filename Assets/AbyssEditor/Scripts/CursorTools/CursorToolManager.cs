using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.UI.HotBar.HotBarButtons;
using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts.CursorTools
{
    public class CursorToolManager : MonoBehaviour
    {
        public static CursorToolManager main;
        
        public BrushTool brushTool { get; private set; }
        private RemoveBatchTool batchRemoveTool { get; set; }
        private SizeRefTool sizeRefTool { get; set; }

        private readonly HashSet<ICursorTool> tools = new();

        private readonly HashSet<MonoBehaviour> inputBlockingScripts = new();

        private ICursorTool activeTool;

        private bool activeToolHidden;

        private void Awake()
        {
            main = this;
            brushTool = new BrushTool();
            batchRemoveTool = new RemoveBatchTool();
            sizeRefTool = new SizeRefTool();
            tools.Add(brushTool);
            tools.Add(batchRemoveTool);
            tools.Add(sizeRefTool);
        }

        private void Start()
        {
            foreach (ICursorTool tool in tools)
            {
                tool.Start();
            }
        }

        private void Update()
        {
            activeTool?.HandleToolUpdate(IsInputBlocked());
        }

        private void SafeChangeActiveTool(CursorTool tool)
        {
            if (activeTool != null && activeTool.ToolType == tool) return;
            activeTool?.DisableTool();
            activeTool = tools.First(t => t.ToolType == tool);
        }

        public bool EnableTool(CursorTool tool, HotBarButton hotBarButton = null)
        {
            if (HasBlockingScripts()) return false;
            
            SafeChangeActiveTool(tool);
            activeTool?.EnableTool(hotBarButton);
            return true;
        }

        public void DisableActiveTool()
        {
            activeTool?.DisableTool();
            activeTool = null;
        }

        public void RegisterInputBlock(MonoBehaviour script)
        {
            inputBlockingScripts.Add(script);
        }

        public void UnregisterInputBlock(MonoBehaviour script)
        {
            inputBlockingScripts.Remove(script);
        }

        private bool IsInputBlocked() => IsMouseOverUI() || HasBlockingScripts();

        private bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();
        
        private bool HasBlockingScripts() => inputBlockingScripts.Count > 0;
    }

    public enum CursorTool
    {
        None,
        Brush,
        RemoveBatch,
        SizeRef
    }
}
