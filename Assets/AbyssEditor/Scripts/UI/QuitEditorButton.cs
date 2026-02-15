using System.Collections;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.UI.Windows;
using UnityEngine;

namespace AbyssEditor.Scripts.UI
{
    public class QuitEditorButton : MonoBehaviour
    {
        public void QuitEditor()
        {
            StartCoroutine(QuitEditorCoroutine());
        }

        private IEnumerator QuitEditorCoroutine()
        {
            UIConfirmationWindow.main.OpenWindow(
                Language.main.Get("QuitConfirmationMessage"),
                Language.main.Get("CancelQuit"), 
                Language.main.Get("ConfirmQuit"), 
                out UIConfirmationWindow.Response response
            );
                
            yield return new WaitUntil(() => response.receivedResponse);

            if (!response.response)
            {
                yield break;
            }
            
            Application.Quit();
        }
    }
}
