using UnityEngine;
namespace AbyssEditor.Scripts.CursorTools.Brush
{
    /* Brush types:
    add/remove
    paint material / select material?
    flatten surface
    smooth available always with SHIFT */
    public enum BrushMode {
        Add,
        Remove,
        Paint,
        Eyedropper,
        Flatten,
        Smooth,
    }
    
    public static class BrushModeExtensions
    {
        public static Color GetColor(this BrushMode mode)
        {
            return mode switch
            {
                BrushMode.Add => Color.green,
                BrushMode.Remove => Color.red,
                BrushMode.Paint => Color.blue,
                BrushMode.Eyedropper => Color.yellow,
                BrushMode.Flatten => Color.aquamarine,
                BrushMode.Smooth => Color.purple,
                _ => Color.white
            };
        }
    }
}
