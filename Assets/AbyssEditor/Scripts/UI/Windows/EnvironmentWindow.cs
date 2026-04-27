using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    [RequireComponent(typeof(UIWindow))]
    public class EnvironmentWindow : MonoBehaviour {
        [SerializeField] private UIHybridInput sunPitch;
        [SerializeField] private UIHybridInput sunYaw;
        [SerializeField] private UIColorPicker sunColor;
        [SerializeField] private UIHybridInput sunIntensity;
        [SerializeField] private UIHybridInput ambientIntensity;
        [SerializeField] private Toggle brushLightToggle;
        [SerializeField] private Toggle surfaceWaterToggle;
        [SerializeField] private UIHybridInput lodSlider;

        private void Start() {
            sunPitch.OnValueUpdated += OnUpdateSunRotation;
            sunPitch.OnEndDragging += SaveToDisk;
            sunPitch.formatFunction = FormatAngle;
            sunPitch.SetValue(Preferences.data.sunPitch);

            sunYaw.OnValueUpdated += OnUpdateSunRotation;
            sunYaw.OnEndDragging += SaveToDisk;
            sunYaw.formatFunction = FormatAngle;
            sunYaw.SetValue(Preferences.data.sunYaw);

            sunColor.OnColorChanged += OnUpdateSunColor;
            sunColor.OnColorChanged += SaveToDisk;
            sunColor.SetInitialColor(new Color(Preferences.data.sunColorR, Preferences.data.sunColorG, Preferences.data.sunColorB));
            
            sunIntensity.OnValueUpdated += OnUpdateSunIntensity;
            sunIntensity.OnEndDragging += SaveToDisk;
            sunIntensity.formatFunction = FormatScalar;
            sunIntensity.SetValue(Preferences.data.sunIntensity);
            
            ambientIntensity.OnValueUpdated += OnUpdateAmbientIntensity;
            ambientIntensity.OnEndDragging += SaveToDisk;
            ambientIntensity.formatFunction = FormatScalar;
            ambientIntensity.SetValue(Preferences.data.ambientIntensity);
            
            brushLightToggle.SetIsOnWithoutNotify(Preferences.data.enableBrushLight);
            brushLightToggle.onValueChanged.AddListener(OnUpdateBrushLight);
            
            surfaceWaterToggle.SetIsOnWithoutNotify(Preferences.data.displaySurfaceWater);
            surfaceWaterToggle.onValueChanged.AddListener(OnUpdateSurfaceWater);
            
            lodSlider.OnValueUpdated += OnUpdateLODLevel;
            lodSlider.OnEndDragging += SaveToDisk;
            lodSlider.formatFunction = FormatLOD;
            lodSlider.SetValue(Preferences.data.lodLevel);
        }

        private static string FormatAngle(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)}°";
        private static string FormatScalar(float lerpedVal) => lerpedVal.ToString("0.00");
        private static string FormatLOD(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)}";

        private void OnUpdateSunRotation()
        {
            Preferences.data.sunPitch = sunPitch.lerpedValue;
            Preferences.data.sunYaw = sunYaw.lerpedValue;
            LightingManager.main.UpdateSunRotation(sunPitch.lerpedValue, sunYaw.lerpedValue);
        }

        private void OnUpdateSunColor()
        {
            Preferences.data.sunColorR = sunColor.color.r;
            Preferences.data.sunColorG = sunColor.color.g;
            Preferences.data.sunColorB = sunColor.color.b;
            LightingManager.main.UpdateSunColor(sunColor.color.r, sunColor.color.g, sunColor.color.b);
        }

        private void OnUpdateSunIntensity()
        {
            Preferences.data.sunIntensity = sunIntensity.lerpedValue;
            LightingManager.main.UpdateSunIntensity(sunIntensity.lerpedValue);
        }

        private void OnUpdateAmbientIntensity()
        {
            Preferences.data.ambientIntensity = ambientIntensity.lerpedValue;
            LightingManager.main.UpdateAmbientIntensity(ambientIntensity.lerpedValue);
        }

        // getting commands from UI
        private static void OnUpdateBrushLight(bool value)
        {
            Preferences.data.enableBrushLight = value;
            LightingManager.main.UpdateBrushLight(value);
            SaveToDisk();
        }
        
        private static void OnUpdateSurfaceWater(bool value)
        {
            Preferences.data.displaySurfaceWater = value;
            VoxelMetaspace.metaspace.ReloadBoundaries();
            SaveToDisk();
        }
        
        private void OnUpdateLODLevel()
        {
            Preferences.data.lodLevel = Mathf.RoundToInt(lodSlider.lerpedValue);
            SaveToDisk();
        }
        
        private static void SaveToDisk() => Preferences.SavePreferencesToDisk();
    }
}