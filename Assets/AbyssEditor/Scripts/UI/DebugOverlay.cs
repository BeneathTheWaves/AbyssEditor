using UnityEngine;

namespace AbyssEditor.Scripts.UI
{
    public class DebugOverlay : MonoBehaviour
    {
        private static DebugOverlay instance;

        [SerializeField] private GameObject messageInstancePrefab;
        [SerializeField] private RectTransform messageParent;

        private void Awake()
        {
            instance = this;
        }
    
        private static void CheckInstance()
        {
            if (instance == null) instance = FindFirstObjectByType<DebugOverlay>();
        }

        public static void LogMessage(string message)
        {
            CheckInstance();
            instance.SendMessageInternal(message, Color.white);
            Debug.Log(message);
        }

        public static void LogWarning(string message)
        {
            CheckInstance();
            instance.SendMessageInternal(message, Color.yellow);
            Debug.LogWarning(message);
        }

        public static void LogError(string message)
        {
            CheckInstance();
            instance.SendMessageInternal(message, Color.red);
            Debug.LogError(message);
        }

        private void SendMessageInternal(string message, Color color)
        {
            var messageInstance = Instantiate(messageInstancePrefab);
            messageInstance.GetComponent<RectTransform>().SetParent(messageParent);
            messageInstance.GetComponent<UILogOverlayMessageInstance>().InitializeMessage(message, color);
            messageInstance.SetActive(true);
        }
    }
}