using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    [RequireComponent(typeof(UIWindow))]
    public class LightingWindow : MonoBehaviour {
        [SerializeField] private UIHybridInput sunPitch;
        [SerializeField] private UIHybridInput sunYaw;
        [SerializeField] private UIColorPicker sunColor;
        [SerializeField] private UIHybridInput sunIntensity;
        [SerializeField] private UIHybridInput ambientIntensity;
        
        [SerializeField] private Toggle brushLightToggle;

        private void Start() {
            sunPitch.OnValueUpdated += UpdateSunRotation;
            sunPitch.OnEndDragging += SavePreferences;
            sunPitch.formatFunction = FormatAngle;
            sunPitch.SetValue(Preferences.data.sunPitch);

            sunYaw.OnValueUpdated += UpdateSunRotation;
            sunYaw.OnEndDragging += SavePreferences;
            sunYaw.formatFunction = FormatAngle;
            sunYaw.SetValue(Preferences.data.sunYaw);

            sunColor.OnColorChanged += UpdateSunColor;
            sunColor.OnColorChanged += SavePreferences;
            sunColor.SetInitialColor(new Color(Preferences.data.sunColorR, Preferences.data.sunColorG, Preferences.data.sunColorB));
            
            sunIntensity.OnValueUpdated += UpdateSunIntensity;
            sunIntensity.OnEndDragging += SavePreferences;
            sunIntensity.formatFunction = FormatScalar;
            sunIntensity.SetValue(Preferences.data.sunIntensity);
            
            ambientIntensity.OnValueUpdated += UpdateAmbientIntensity;
            ambientIntensity.OnEndDragging += SavePreferences;
            ambientIntensity.formatFunction = FormatScalar;
            ambientIntensity.SetValue(Preferences.data.ambientIntensity);
            
            brushLightToggle.SetIsOnWithoutNotify(Preferences.data.enableBrushLight);
            brushLightToggle.onValueChanged.AddListener(UpdateBrushLight);
        }

        private string FormatAngle(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)}°";
        private string FormatScalar(float lerpedVal) => lerpedVal.ToString("0.00");

        // getting commands from UI
        public void UpdateBrushLight(bool value)
        {
            LightingManager.main.UpdateBrushLight(value);
            SavePreferences();
        }

        void UpdateSunRotation()
        {
            LightingManager.main.UpdateSunRotation(sunPitch.lerpedValue, sunYaw.lerpedValue);
        }

        void UpdateSunColor()
        {
            LightingManager.main.UpdateSunColor(sunColor.color.r, sunColor.color.g, sunColor.color.b);
        }

        void UpdateSunIntensity()
        {
            LightingManager.main.UpdateSunIntensity(sunIntensity.lerpedValue);
        }
        
        void UpdateAmbientIntensity()
        {
            LightingManager.main.UpdateAmbientIntensity(ambientIntensity.lerpedValue);
        }

        void SavePreferences()
        {
            Preferences.data.sunPitch = sunPitch.lerpedValue;
            Preferences.data.sunYaw = sunYaw.lerpedValue;
            Preferences.data.sunColorR = sunColor.color.r;
            Preferences.data.sunColorG = sunColor.color.g;
            Preferences.data.sunColorB = sunColor.color.b;
            Preferences.data.sunIntensity = sunIntensity.lerpedValue;
            Preferences.data.ambientIntensity = ambientIntensity.lerpedValue;
            
            Preferences.SavePreferences();
        }
    }
}