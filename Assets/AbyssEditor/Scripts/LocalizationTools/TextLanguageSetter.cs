using AbyssEditor.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
