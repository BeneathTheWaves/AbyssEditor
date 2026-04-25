using System;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.TerrainMaterials;
using UnityEngine;

namespace AbyssEditor.Scripts.UI.Windows {
    public class UIActiveToolWindow : UIWindow {
        [SerializeField] private UIHybridInput brushSizeSelector;
        [SerializeField] private UIHybridInput brushStrengthSelector;
        [SerializeField] private UIBlocktypePreview blocktypePreview;
        
        BrushTool brushTool;

        private void Awake() {
            brushSizeSelector.formatFunction = FormatSize;
            brushSizeSelector.OnValueUpdated += SetNewBrushSize;
            brushStrengthSelector.formatFunction = FormatStrength;
            brushStrengthSelector.OnValueUpdated += SetNewBrushStrength;
        }
        private void Start()
        {
            //Brush tool is initialized in awake so must get vars here
            brushSizeSelector.minValue = BrushTool.MIN_BRUSH_SIZE;
            brushSizeSelector.maxValue = BrushTool.MAX_BRUSH_SIZE;
            brushTool.OnParametersChanged += RedrawValues;
            RedrawValues();
        }

        protected override void EnableWindow()
        {
            base.EnableWindow();
            brushTool = CursorToolManager.main.brushTool;

            RedrawValues();
        }

        // For receiving commands from UI
        private void SetNewBrushSize() {
            brushTool.SetBrushSize(brushSizeSelector.lerpedValue);
        }
        private void SetNewBrushStrength() {
            brushTool.SetBrushStrength(brushStrengthSelector.lerpedValue);
        }
        /*public void SetNewBlocktype() {
            if (byte.TryParse(blocktypePreview.matNumber.ToString(), out byte typeValue)) {
                if (SnMaterialLoader.instance.blocktypesData[typeValue].ExistsInGame) {
                    brushTool.SetBrushMaterial(typeValue);
                }
            }
        }*/

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
