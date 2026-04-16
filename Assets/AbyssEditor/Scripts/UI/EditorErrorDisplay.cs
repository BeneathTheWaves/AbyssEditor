using TMPro;
using UnityEngine;

namespace AbyssEditor.Scripts.UI
{
    public class EditorErrorDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        [SerializeField] private int destroyTime = 10;
        
        private void Start()
        {
            Invoke(nameof(DestroyError), destroyTime);
        }

        private void DestroyError()
        {
            Destroy(gameObject);
        }
        
        /// <summary>
        /// Set text error
        /// </summary>
        /// <param name="errorText">error text</param>
        public void SetErrorText(string errorText)
        {
            text.text = errorText;
        }
    }
}
