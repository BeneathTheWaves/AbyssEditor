using System;
using System.Collections;
using AbyssEditor.Scripts.Essentials;
using AbyssEditor.Scripts.UI.Windows;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
namespace AbyssEditor.Scripts.UI.MainMenu
{
    public class VersionChecker : MonoBehaviour
    {
        private const string PUBLIC_VERSION_URL = "https://raw.githubusercontent.com/BeneathTheWaves/AbyssEditor/refs/heads/master/versionChecking.json";
        
        [SerializeField] private TextMeshProUGUI versionText;

        private void Start()
        {
            versionText.text = $"{Language.main.Get("Version")} {Application.version}";
            StartCoroutine(FetchLatestVersion());
        }

        private IEnumerator DisplayOutOfDateMessage(VersionJson versionJson)
        {
            ConfirmationWindow.main.OpenWindow(
                versionJson.notificationMessage,
                Language.main.Get("RejectUpdate"),
                Language.main.Get("ConfirmUpdate"),//
                out ConfirmationWindow.Response response
            );
            
            yield return new WaitUntil(() => response.receivedResponse);

            if (!response.response)
            {
                yield break;
            }
            
            Debug.Log($"Opening: {versionJson.downloadURL}");
            Application.OpenURL(versionJson.downloadURL);
            Application.Quit();
        }
        
        private IEnumerator FetchLatestVersion() {
            UnityWebRequest request = UnityWebRequest.Get(PUBLIC_VERSION_URL);
            yield return request.SendWebRequest();
 
            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Skipping version check. Failed to connect to {PUBLIC_VERSION_URL} due to: {request.error}");
                yield break;
            }
            
            VersionJson versionJson = VersionJson.ConvertFromBlob(request.downloadHandler.text);
            
            if (versionJson == null) yield break;
            try
            {
                CheckVersion(versionJson);
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to parse latest version information: " + e);
            }
        }

        private void CheckVersion(VersionJson versionJson)
        {
            if (Version.TryParse(Application.version, out Version appVersion) && Version.TryParse(versionJson.latestVersion, out Version latestVersion))
            {
                if (appVersion < latestVersion)
                {
                    StartCoroutine(DisplayOutOfDateMessage(versionJson));
                }
                else
                {
                    Debug.Log($"Up to date running {Application.version}");
                }
            }
            else
            {
                Debug.LogError($"Failed to parse versions into comparable containers. App Version: {Application.version} Fetched Lastest Version: {versionJson.latestVersion}");
            }
        }
    }
}
