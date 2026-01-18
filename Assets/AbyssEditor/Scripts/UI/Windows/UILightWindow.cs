using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI {
    public class UILightWindow : UIWindow {
        public Light[] sunLights;
        // Two things:
        // 1. Rotate the sun around X and Y axis with a hybrid slider
        UIHybridInput rotationXSlider;
        UIHybridInput rotationYSlider;
        UIHybridInput sunR;
        UIHybridInput sunG;
        UIHybridInput sunB;
        UIHybridInput sunIntensity;
        public Transform sunTransform;
        // 2. Enable/disable brush light
        UICheckbox checkbox;
        Light brushLight;

        private void Start() {
            rotationXSlider = transform.GetChild(1).GetChild(0).GetComponentInChildren<UIHybridInput>();
            rotationXSlider.OnValueUpdated += UpdateSunRotation;
            rotationXSlider.formatFunction = FormatAngle;
            rotationXSlider.maxValue = 90;
            rotationXSlider.SetValue(60);

            rotationYSlider = transform.GetChild(1).GetChild(1).GetComponentInChildren<UIHybridInput>();
            rotationYSlider.OnValueUpdated += UpdateSunRotation;
            rotationYSlider.formatFunction = FormatAngle;
            rotationYSlider.maxValue = 360;
            rotationYSlider.SetValue(250);
            rotationYSlider.modValue = true;

            sunR = transform.GetChild(1).GetChild(3).GetComponentInChildren<UIHybridInput>();
            sunR.OnValueUpdated += UpdateSunColor;
            sunR.formatFunction = FormatScalar;
            sunR.SetValue(1f);

            sunG = transform.GetChild(1).GetChild(4).GetComponentInChildren<UIHybridInput>();
            sunG.OnValueUpdated += UpdateSunColor;
            sunG.formatFunction = FormatScalar;
            sunG.SetValue(1f);

            sunB = transform.GetChild(1).GetChild(5).GetComponentInChildren<UIHybridInput>();
            sunB.OnValueUpdated += UpdateSunColor;
            sunB.formatFunction = FormatScalar;
            sunB.SetValue(1f);

            sunIntensity = transform.GetChild(1).GetChild(6).GetComponentInChildren<UIHybridInput>();
            sunIntensity.OnValueUpdated += UpdateSunIntensity;
            sunIntensity.formatFunction = FormatScalar;
            sunIntensity.SetValue(1f);

            brushLight = Brush.GetBrushLight();
            checkbox = GetComponentInChildren<UICheckbox>();
            checkbox.transform.GetComponent<Button>().onClick.AddListener(UpdateBrushLight);

            UpdateSunRotation();
            UpdateBrushLight();
        }

        private string FormatAngle(float lerpedVal) => $"{Mathf.RoundToInt(lerpedVal)} deg";
        private string FormatScalar(float lerpedVal) => lerpedVal.ToString("#.##");

        // getting commands from UI
        void UpdateBrushLight() => brushLight.enabled = checkbox.check;
        void UpdateSunRotation() => sunTransform.eulerAngles = new Vector3(rotationXSlider.LerpedValue, rotationYSlider.LerpedValue, 0);

        void UpdateSunColor()
        {
            foreach (var light in sunLights)
            {
                light.color = new Color(sunR.LerpedValue, sunG.LerpedValue, sunB.LerpedValue);
            }
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