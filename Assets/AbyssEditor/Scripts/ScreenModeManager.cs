using System;
using AbyssEditor.Scripts.SaveSystem;
using UnityEngine;

namespace AbyssEditor.Scripts
{
    public class ScreenModeManager : MonoBehaviour
    {
        public void Start()
        {
            ChangeScreenMode(Preferences.data.fullscreenMode);
        }
        
        public static void ChangeScreenMode(string languageKey)
        {
            Screen.fullScreenMode = ConvertFullScreenModeLanguageKey(Preferences.data.fullscreenMode);
        }

        private static FullScreenMode ConvertFullScreenModeLanguageKey(string languageKey)
        {
            return languageKey switch
            {
                "BorderlessWindowed" => FullScreenMode.FullScreenWindow,
                "ExclusiveFullScreen" => FullScreenMode.ExclusiveFullScreen,
                "Windowed" => FullScreenMode.Windowed,
                _ => throw new ArgumentOutOfRangeException(nameof(languageKey), languageKey, null)
            };
        }
    }
}