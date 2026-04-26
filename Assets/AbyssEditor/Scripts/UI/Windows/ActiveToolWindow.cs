using System;
using System.Collections;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.TerrainMaterials;
using UnityEngine;

namespace AbyssEditor.Scripts.UI.Windows {
    [RequireComponent(typeof(UIWindow))]
    public class ActiveToolWindow : MonoBehaviour {
        [SerializeField] private UIHybridInput brushSizeSelector;
        [SerializeField] private UIHybridInput brushStrengthSelector;
        [SerializeField] private UIBlocktypePreview blocktypePreview;

        private BrushTool brushTool;

        private void Awake() {
            brushSizeSelector.formatFunction = FormatSize;
            brushSizeSelector.OnValueUpdated += SetNewBrushSize;
            brushStrengthSelector.formatFunction = FormatStrength;
            brushStrengthSelector.OnValueUpdated += SetNewBrushStrength;
        }
        private IEnumerator Start()
        {
            //Brush tool is initialized in awake so must get vars here
            brushSizeSelector.minValue = BrushTool.MIN_BRUSH_SIZE;
            brushSizeSelector.maxValue = BrushTool.MAX_BRUSH_SIZE;
            
            yield return new WaitUntil(() => CursorToolManager.main != null);
            brushTool = CursorToolManager.main.brushTool;
            brushTool.OnParametersChanged += RedrawValues;
            RedrawValues();
        }

        // For receiving commands from UI
        private void SetNewBrushSize() {
            brushTool.SetBrushSize(brushSizeSelector.lerpedValue);
        }
        private void SetNewBrushStrength() {
            brushTool.SetBrushStrength(brushStrengthSelector.lerpedValue);
        }

        // formatting
        private string FormatSize(float val) => Math.Round(val, 1).ToString("0.0");
        private string FormatStrength(float val) => Math.Round(val, 3).ToString("0.000");

        // For redrawing UI
        private void RedrawValues() {
            RedrawBlocktypeDisplay();
            RedrawRadiusDisplay();
            RedrawStrengthDisplay();
        }

        private void RedrawRadiusDisplay() {
            brushSizeSelector.SetValue(brushTool.currentBrushSize);
        }
        private void RedrawStrengthDisplay() {
            brushStrengthSelector.SetValue(brushTool.currentBrushStrength);
        }
        private void RedrawBlocktypeDisplay(){
            try
            {
				blocktypePreview.UpdatePreview(Convert.ToInt32(brushTool.currentSelectedType), SnMaterialLoader.instance.blocktypesData[brushTool.currentSelectedType]);
			}
            catch (NullReferenceException)
            {}
        }
    }
}
