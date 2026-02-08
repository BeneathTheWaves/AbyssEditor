using System;
using UnityEngine;
using UnityEngine.Events;

namespace com.judahperez.hsvcolorpicker.Events
{
    [Serializable]
    public class ColorChangedEvent : UnityEvent<Color>
    {

    }
}