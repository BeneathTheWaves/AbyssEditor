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

        public IEnumerator QuitEditorCoroutine()
        {
            UIConfirmationWindow.main.OpenWindow(
                Language.main.Get("ConfirmationMessage"),
                Language.main.Get("ConfirmQuit"), 
                Language.main.Get("CancelQuit"), 
                out UIConfirmationWindow.Response response
            );
                
            yield return new WaitUntil(() => response.receivedResponse);

            if (!response.response)
            {
                yield break;
            }
        }
    }
}
