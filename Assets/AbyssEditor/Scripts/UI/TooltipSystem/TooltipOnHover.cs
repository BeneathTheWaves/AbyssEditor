using AbyssEditor.Scripts.Essentials;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AbyssEditor.Scripts.UI.TooltipSystem
{
    public class TooltipOnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ITooltipSource
    {
        public string text;
        public bool translateText = true;

        private bool _hovering;
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            TooltipSingleton.AddTooltipSource(this);
            _hovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipSingleton.RemoveTooltipSource(this);
            _hovering = false;
        }

        public virtual string GetText()
        {
            if (translateText)
                return Language.main.Get(text);
            
            return text;
        }

        private void OnDisable()
        {
            if (_hovering)
            {
                TooltipSingleton.RemoveTooltipSource(this);
                _hovering = false;
            }
        }
    }
}