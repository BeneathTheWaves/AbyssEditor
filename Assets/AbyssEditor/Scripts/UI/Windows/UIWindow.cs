using UnityEngine;

namespace AbyssEditor.Scripts.UI.Windows {
    [RequireComponent(typeof(RectTransform))]
    public class UIWindow : MonoBehaviour {
        public RectTransform rt { get; private set; }

        [field: SerializeField] public string windowKey { private set; get; }
        [SerializeField] private float minHeight = 300;
        [SerializeField] private float maxHeight = 500;
        [SerializeField] private float minWidth = 450;
        [SerializeField] private float maxWidth = 600;

        public void Awake()
        {
            rt = GetComponent<RectTransform>();
        }

        public virtual void DisableWindow() {
            gameObject.SetActive(false);
        }
        
        public virtual void UpdateScale()
        {
            transform.localScale = Vector3.one * 1;
        }
        
        public void ToggleWindow() {
            if (gameObject.activeInHierarchy) DisableWindow();
            else EnableWindow();
        }
        
        public void EnableWindow() {
            PushToTop();
            gameObject.SetActive(true);
        }

        public void UpdateWindowSize(float left, float top, float bottom, float right)
        {
            float newWidth = rt.rect.width - left + right;
            float newHeight = rt.rect.height - bottom + top;
            
            if (newWidth < minWidth || newWidth > maxWidth)
            {
                left = 0;
                right = 0;
            }

            if (newHeight < minHeight || newHeight > maxHeight)
            {
                bottom = 0;
                top = 0;
            }
            
            rt.offsetMin += new Vector2(left, bottom);
            rt.offsetMax += new Vector2(right, top);
        }

        public void Move(Vector3 newPosition) => rt.position = newPosition;
        public Vector3 GetWindowPos() => rt.position;

        public void PushToTop() {
            transform.SetAsLastSibling();
        }
    }
}