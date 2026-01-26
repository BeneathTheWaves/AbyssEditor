using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows {
    public class UILightWindow : UIWindow {
        public UIHybridInput sunPitch;
        public UIHybridInput sunYaw;
        public UIHybridInput sunR;
        public UIHybridInput sunG;
        public UIHybridInput sunB;
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

            sunR.OnValueUpdated += UpdateSunColor;
            sunR.OnEndDragging += SavePreferences;
            sunR.formatFunction = FormatScalar;
            sunR.SetValue(Preferences.data.sunColorR);

            sunG.OnValueUpdated += UpdateSunColor;
            sunG.OnEndDragging += SavePreferences;
            sunG.formatFunction = FormatScalar;
            sunG.SetValue(Preferences.data.sunColorG);

            sunB.OnValueUpdated += UpdateSunColor;
            sunB.OnEndDragging += SavePreferences;
            sunB.formatFunction = FormatScalar;
            sunB.SetValue(Preferences.data.sunColorB);

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
            LightingManager.main.UpdateSunColor(sunR.LerpedValue, sunG.LerpedValue, sunB.LerpedValue);
        }

        void UpdateSunIntensity()
        {
            LightingManager.main.UpdateSunIntensity(sunIntensity.LerpedValue);
        }

        void SavePreferences()
        {
            Preferences.data.sunPitch = sunPitch.LerpedValue;
            Preferences.data.sunYaw = sunYaw.LerpedValue;
            Preferences.data.sunColorR = sunR.LerpedValue;
            Preferences.data.sunColorG = sunG.LerpedValue;
            Preferences.data.sunColorB = sunB.LerpedValue;
            Preferences.data.sunIntensity = sunIntensity.LerpedValue;
            
            Preferences.SavePreferences();
        }
    }
}