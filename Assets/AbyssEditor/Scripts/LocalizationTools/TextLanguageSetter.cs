using AbyssEditor.Scripts.Essentials;
using TMPro;
using UnityEngine;

namespace AbyssEditor.Scripts.LocalizationTools
{
    public class TextLanguageSetter : MonoBehaviour
    {
        private TextMeshProUGUI text;
        private string key;
        void Start()
        {
            text = GetComponent<TextMeshProUGUI>();
            key = text.text;
            text.text = Language.main.Get(key);
        }
    }
}
