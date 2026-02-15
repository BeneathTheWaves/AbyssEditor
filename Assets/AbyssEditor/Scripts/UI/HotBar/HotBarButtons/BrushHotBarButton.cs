using System;
using AbyssEditor.Scripts.CursorTools.Brush;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.HotBar.HotBarButtons
{
    public class BrushHotBarButton : HotBarButton
    {
        [SerializeField] private BrushMode brushMode;

        public BrushMode GetBrushMode()
        {
            return brushMode;
        }
    }
}
