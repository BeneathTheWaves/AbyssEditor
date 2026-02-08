using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    public class UILightWindow : UIWindow {
        public UIHybridInput sunPitch;
        public UIHybridInput sunYaw;
        public UIColorPicker sunColor;
        public UIHybridInput sunIntensity;
        
        public Toggle brushLightToggle;

        private void Start() {
            sunPitch.OnValueUpdated += UpdateSunRotation;
            sunPitch.OnEndDragging += SavePreferences;
            sunPitch.formatFunction = FormatAngle;
            sunPitch.SetValue(Preferences.data.sunPitch);

            sunYaw.OnValueUpdated += UpdateSunRotation;
            sunYaw.OnEndDragging += SavePreferences;
            sunYaw.formatFunction = FormatAngle;
            sunYaw.SetValue(Preferences.data.sunYaw);

            sunColor.onColorChanged += UpdateSunColor;
            sunColor.onColorChanged += SavePreferences;
            sunColor.SetInitialColor(new Color(Preferences.data.sunColorR, Preferences.data.sunColorG, Preferences.data.sunColorB));
            
            sunIntensity.OnValueUpdated += UpdateSunIntensity;
            sunIntensity.OnEndDragging += SavePreferences;
            sunIntensity.formatFunction = FormatScalar;
            sunIntensity.SetValue(Preferences.data.sunIntensity);
            
            brushLightToggle.SetIsOnWithoutNotify(Preferences.data.enableBrushLight);
        }

        private string FormatAngle(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)} deg";
        private string FormatScalar(float lerpedVal) => lerpedVal.ToString("0.00");

        // getting commands from UI
        public void UpdateBrushLight(bool value)
        {
            LightingManager.main.UpdateBrushLight(value);
            SavePreferences();
        }

        void UpdateSunRotation()
        {
            LightingManager.main.UpdateSunRotation(sunPitch.LerpedValue, sunYaw.LerpedValue);
        }

        void UpdateSunColor()
        {
            LightingManager.main.UpdateSunColor(sunColor.color.r, sunColor.color.g, sunColor.color.b);
        }

        void UpdateSunIntensity()
        {
            LightingManager.main.UpdateSunIntensity(sunIntensity.LerpedValue);
        }

        void SavePreferences()
        {
            Preferences.data.sunPitch = sunPitch.LerpedValue;
            Preferences.data.sunYaw = sunYaw.LerpedValue;
            Preferences.data.sunColorR = sunColor.color.r;
            Preferences.data.sunColorG = sunColor.color.g;
            Preferences.data.sunColorB = sunColor.color.b;
            Preferences.data.sunIntensity = sunIntensity.LerpedValue;
            
            Preferences.SavePreferences();
        }
    }
}