using TMPro;
using UnityEngine;

namespace AbyssEditor.Scripts.UI
{
    public class TaskStatusDisplay : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI text;
        
        [SerializeField] private RectTransform progressBar;
        [SerializeField] private RectTransform progressBarBackground;

        private const float MIN_WIDTH = 20;
        private float maxWidth;

        private void Start()
        {
            maxWidth = progressBarBackground.sizeDelta.x;
        }

        /// <summary>
        /// Set text status
        /// </summary>
        /// <param name="status">text status</param>
        public void SetStatusText(string status)
        {
            text.text = status;
        }
        
        /// <summary>
        /// Visual Display Update
        /// </summary>
        /// <param name="progress">between 0.0 - 1.0</param>
        public void SetProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            float filledWidth = MIN_WIDTH + progress * (maxWidth - MIN_WIDTH);
            progressBar.sizeDelta = new Vector2(filledWidth, progressBar.sizeDelta.y);
        }
    }
}
