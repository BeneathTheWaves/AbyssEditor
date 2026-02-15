using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts.UI.HotBar;
using UnityEngine;
using UnityEngine.EventSystems;
namespace AbyssEditor.Scripts.CursorTools
{
    public class CursorToolManager : MonoBehaviour
    {
        public static CursorToolManager main;
        
        public BrushTool brushTool;

        private readonly HashSet<ICursorTool> tools = new();
        
        private readonly HashSet<MonoBehaviour> inputBlockingScripts = new();

        private ICursorTool activeTool;

        private void Awake()
        {
            main = this;
            
            brushTool = new BrushTool();
            tools.Add(brushTool);
        }

        private void Update()
        {
            activeTool?.HandleToolUpdate(CanUseTool());
        }

        public void Enable<T>() where T : ICursorTool
        {
            activeTool?.DisableTool();
            activeTool = tools.OfType<T>().First();
            activeTool?.EnableTool();
        }
        
        public void Enable<T>(IHotBarButton hotBarButton) where T : ICursorTool
        {
            activeTool?.DisableTool();
            activeTool = tools.OfType<T>().First();
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
        }

        private bool CanUseTool() => IsMouseOverUI() || HadBlockingScripts();
        
        private bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();

        private bool HadBlockingScripts() => inputBlockingScripts.Count > 0;
    }
}
