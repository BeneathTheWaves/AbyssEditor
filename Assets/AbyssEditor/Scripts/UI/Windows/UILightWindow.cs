using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.UI;
using UnityEngine;
using UnityEngine.UI;
namespace AbyssEditor.Scripts.UI.Windows {
    public class UILightWindow : UIWindow {
        public Light[] sunLights;
        // Two things:
        // 1. Rotate the sun around X and Y axis with a hybrid slider
        public UIHybridInput rotationPitchSlider;
        public UIHybridInput rotationYawSlider;
        public UIHybridInput sunR;
        public UIHybridInput sunG;
        public UIHybridInput sunB;
        public UIHybridInput sunIntensity;
        public Transform sunTransform;
        // 2. Enable/disable brush light
        public Toggle brushLightToggle;
        Light brushLight;

        private void Start() {
            rotationPitchSlider.OnValueUpdated += UpdateSunRotation;
            rotationPitchSlider.OnEndDragging += SavePreferences;
            rotationPitchSlider.formatFunction = FormatAngle;
            rotationPitchSlider.SetValue(Preferences.data.sunPitch);

            rotationYawSlider.OnValueUpdated += UpdateSunRotation;
            rotationYawSlider.OnEndDragging += SavePreferences;
            rotationYawSlider.formatFunction = FormatAngle;
            rotationYawSlider.SetValue(Preferences.data.sunYaw);

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

            brushLight = Brush.GetBrushLight();
            brushLightToggle.onValueChanged.AddListener(UpdateBrushLight);
            brushLightToggle.SetIsOnWithoutNotify(Preferences.data.enableBrushLight);

            UpdateSunRotation();
        }

        private string FormatAngle(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)} deg";
        private string FormatScalar(float lerpedVal) => lerpedVal.ToString("0.00");

        // getting commands from UI
        void UpdateBrushLight(bool value)
        {
            brushLight.enabled = value;
            Preferences.data.enableBrushLight = value;
            Preferences.SavePreferences();
        }
        
        void UpdateSunRotation() => sunTransform.eulerAngles = new Vector3(rotationPitchSlider.LerpedValue, rotationYawSlider.LerpedValue, 0);

        void UpdateSunColor()
        {
            foreach (var light in sunLights)
            {
                light.color = new Color(sunR.LerpedValue, sunG.LerpedValue, sunB.LerpedValue);
            }
        }

        void SavePreferences()
        {
            Preferences.data.sunPitch = rotationPitchSlider.LerpedValue;
            Preferences.data.sunYaw = rotationYawSlider.LerpedValue;
            Preferences.data.sunColorR = sunR.LerpedValue;
            Preferences.data.sunColorG = sunG.LerpedValue;
            Preferences.data.sunColorB = sunB.LerpedValue;
            Preferences.data.sunIntensity = sunIntensity.LerpedValue;
            Preferences.SavePreferences();
        }

        void UpdateSunIntensity()
        {
            foreach (var light in sunLights)
            {
                light.intensity = sunIntensity.LerpedValue;
            }
        }
    }
}