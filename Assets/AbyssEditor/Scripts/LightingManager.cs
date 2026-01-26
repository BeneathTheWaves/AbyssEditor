using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;

namespace AbyssEditor.Scripts
{
    public class LightingManager : MonoBehaviour
    {
        public static LightingManager main;
    
        public Transform sunTransform;
        Light brushLight;
    
        public Light[] sunLights;
        void Awake()
        {
            main = this;
        }
    
        void Start()
        {
            brushLight = Brush.GetBrushLight();
            LoadFromSettings();
        }

        public void LoadFromSettings()
        {
            UpdateBrushLight(Preferences.data.enableBrushLight);
            UpdateSunRotation(Preferences.data.sunPitch, Preferences.data.sunYaw);
            UpdateSunColor(Preferences.data.sunColorR, Preferences.data.sunColorG, Preferences.data.sunColorB);
            UpdateSunIntensity(Preferences.data.sunIntensity);
        }

        public void UpdateBrushLight(bool value)
        {
            brushLight.enabled = value;
        }

        public void UpdateSunRotation(float pitch, float yaw)
        {
            sunTransform.eulerAngles = new Vector3(pitch, yaw, 0);
        }


        public void UpdateSunColor(float r, float g, float b)
        {
            foreach (var light in sunLights)
            {
                light.color = new Color(r, g, b);
            }
        }
    
        public void UpdateSunIntensity(float value)
        {
            foreach (var light in sunLights)
            {
                light.intensity = value;
            }
        }
    }
}
