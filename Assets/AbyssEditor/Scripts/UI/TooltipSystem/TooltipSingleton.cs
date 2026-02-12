using TMPro;
using UnityEngine;

namespace AbyssEditor.Scripts.UI.TooltipSystem
{
    public class TooltipSingleton : MonoBehaviour
    {
        [SerializeField] private RectTransform canvas;
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private GameObject toggledObject;
        [SerializeField] private RectTransform pivot;
        
        private static TooltipSingleton instance;

        private ITooltipSource source;

        private void Awake()
        {
            if (instance != null)
            {
                Debug.LogWarning("Multiple TooltipSingleton instances exist!");
            }

            instance = this;
        }

        public static void AddTooltipSource(ITooltipSource source)
        {
            instance.source = source;
            instance.UpdateActiveState();
        }
        
        public static void RemoveTooltipSource(ITooltipSource source)
        {
            if (instance.source == source)
            {
                instance.source = null;
            }
            instance.UpdateActiveState();
        }

        private void UpdateActiveState()
        {
            toggledObject.SetActive(source != null);
        }

        private void Update()
        {
            if (source != null)
            {
                text.text = source.GetText();
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, Input.mousePosition,
                        Camera.current, out var point))
                {
                    pivot.localPosition = point;
                }
            }
        }
    }
}