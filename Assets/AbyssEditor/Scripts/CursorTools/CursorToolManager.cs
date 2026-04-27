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

        /* In the event a tool is requested to be enabled but blocking scripts are active,
           the request is cached here and enabled when all blocking scripts are removed*/ 
        private ToolEnableRequest blockedToolToEnable;

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
            activeTool?.HandleToolUpdate(IsCursorToolInputBlocked());
        }

        private void SafeChangeActiveTool(CursorTool tool)
        {
            if (activeTool != null && activeTool.toolType == tool) return;
            activeTool?.DisableTool();
            activeTool = tools.First(t => t.toolType == tool);
        }

        public void EnableTool(CursorTool tool, HotBarButton hotBarButton = null)
        {
            if (tool == CursorTool.None) return;
            
            if (HasBlockingScripts())
            {
                blockedToolToEnable = new ToolEnableRequest(tool, hotBarButton);
                return;
            }
            
            SafeChangeActiveTool(tool);
            activeTool?.EnableTool(hotBarButton);
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
            if (!HasBlockingScripts())
            {
                EnableTool(blockedToolToEnable.toolType, blockedToolToEnable.hotBarButton);
                blockedToolToEnable = ToolEnableRequest.empty;
            }
        }

        private bool IsCursorToolInputBlocked() => IsMouseOverUI() || HasBlockingScripts();

        private bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();
        
        private bool HasBlockingScripts() => inputBlockingScripts.Count > 0;

        
        private struct ToolEnableRequest
        {
            public readonly CursorTool toolType;
            public readonly HotBarButton hotBarButton;

            public ToolEnableRequest(CursorTool toolType, HotBarButton hotBarButton)
            {
                this.toolType = toolType;
                this.hotBarButton = hotBarButton;
            }
            
            public static ToolEnableRequest empty => new ToolEnableRequest(CursorTool.None, null);
        }
    }

    public enum CursorTool
    {
        None,
        Brush,
        RemoveBatch,
        SizeRef
    }
}
