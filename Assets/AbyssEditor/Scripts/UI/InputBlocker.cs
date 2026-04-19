using System;
using UnityEngine;
namespace AbyssEditor.Scripts.UI
{
    public class InputBlocker : MonoBehaviour
    {
        public static InputBlocker inst { get; private set; }
        
        [SerializeField] private GameObject inputBlocker;

        private void Start()
        {
            if (inst != null)
            {
                Destroy(this);
                Debug.LogError("Duplicate UI InputBlocker");
            }
            inst = this;
        }

        private void OnDestroy()
        {
            inst = null;
        }

        public void BlockUIInput()
        {
            inputBlocker.SetActive(true);
        }
        
        public void UnBlockUIInput()
        {
            inputBlocker.SetActive(false);
        }
    }
}
