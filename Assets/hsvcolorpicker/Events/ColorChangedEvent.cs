using System;
using UnityEngine;
using UnityEngine.Events;
namespace hsvcolorpicker.Events
{
    [Serializable]
    public class ColorChangedEvent : UnityEvent<Color>
    {

    }
}