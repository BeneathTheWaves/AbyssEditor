using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.Windows
{
    public class UIConfirmationWindow : MonoBehaviour
    {
        [SerializeField] private GameObject holder;
        [SerializeField] private Button closeWindowButton;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        public static UIConfirmationWindow main;
    
        private Response responseContainer;

        private void Start()
        {
            if (main != null)
            {
                Destroy(this);
                Debug.LogError("Duplicate UI ConfirmationWindow");
            }
            main = this;
            confirmButton.onClick.AddListener(OnConfirm);
            cancelButton.onClick.AddListener(OnCancel);
            closeWindowButton.onClick.AddListener(OnCancel);
        }

        private void OnDestroy()
        {
            main = null;
        }
        
        /// <summary>
        /// Opens a confirmation dialog. You must handle language keys before putting into this function
        /// </summary>
        public void OpenWindow(string description, string cancelButtonText, string confirmButtonText, out Response response)
        {
            if (responseContainer != null)
            {
                responseContainer.receivedResponse = true;
                responseContainer.response = false;
            }
            
            holder.SetActive(true);
            responseContainer = new Response();
            response = responseContainer;
            
            descriptionText.text = description;
            confirmButton.GetComponentInChildren<TextMeshProUGUI>().text = confirmButtonText;
            cancelButton.GetComponentInChildren<TextMeshProUGUI>().text = cancelButtonText;
            
            InputBlocker.inst.BlockUIInput();
        }

        private void CloseWindow()
        {
            holder.SetActive(false);
            responseContainer = null;
            InputBlocker.inst.UnBlockUIInput();
        }

        private void OnCancel()
        {
            responseContainer.receivedResponse = true;
            responseContainer.response = false;
            CloseWindow();
        }
    
        private void OnConfirm()
        {
            responseContainer.receivedResponse = true;
            responseContainer.response = true;
            CloseWindow();
        }

        public class Response
        {
            public bool receivedResponse;
            public bool response;
        }
    }
}