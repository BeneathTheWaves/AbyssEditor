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

        public BrushTool BrushTool { get; private set; }
        public RemoveBatchTool BatchRemoveTool { get; private set; }
        public SizeRefTool SizeRefTool { get; private set; }

        private readonly HashSet<ICursorTool> tools = new();

        private readonly HashSet<MonoBehaviour> inputBlockingScripts = new();

        private ICursorTool activeTool;

        private bool activeToolHidden;

        private void Awake()
        {
            main = this;
            BrushTool = new BrushTool();
            BatchRemoveTool = new RemoveBatchTool();
            SizeRefTool = new SizeRefTool();
            tools.Add(BrushTool);
            tools.Add(BatchRemoveTool);
            tools.Add(SizeRefTool);
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

        private void DisableOldToolSafe<T>() where T : ICursorTool
        {
            if (activeTool == null || activeTool.GetType() != typeof(T))
            {
                activeTool?.DisableTool();
                activeTool = tools.OfType<T>().First();
            }
        }

        public void Enable<T>() where T : ICursorTool
        {
            DisableOldToolSafe<T>();
            activeTool?.EnableTool();
        }

        public void Enable<T>(HotBarButton hotBarButton) where T : ICursorTool
        {
            DisableOldToolSafe<T>();
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

        private bool IsInputBlocked() => IsMouseOverUI() || HasBlockingScripts();

        private bool IsMouseOverUI() => EventSystem.current.IsPointerOverGameObject();
        
        private bool HasBlockingScripts() => inputBlockingScripts.Count > 0;
    }
}
