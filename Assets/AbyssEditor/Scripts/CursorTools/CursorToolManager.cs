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

        private ICursorTool activeTool;

        private void Awake()
        {
            main = this;
            
            brushTool = new BrushTool();
            tools.Add(brushTool);
        }

        private void Update()
        {
            activeTool?.HandleToolUpdate(IsMouseOverUI());
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

        private bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();
    }
}
