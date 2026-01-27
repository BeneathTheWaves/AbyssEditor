using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class UILogOverlayMessageInstance : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float lifetimeDuration;
    [SerializeField] private float fadeOutDuration;

    private float timeSpawned;
    private Color textColor;

    public void InitializeMessage(string message, Color color)
    {
        text.text = message;
        text.color = color;
        timeSpawned = Time.time;
        textColor = color;
    }

    public void ResetTimer()
    {
        timeSpawned = Time.time;
    }

    private void Update()
    {
        if (Time.time > timeSpawned + lifetimeDuration)
        {
            if (Time.time > timeSpawned + lifetimeDuration + fadeOutDuration)
            {
                Destroy(gameObject);
                return;
            }
            var alpha = 1f - ((Time.time - timeSpawned - lifetimeDuration) / fadeOutDuration);
            text.color = new Color(textColor.r, textColor.g, textColor.b, alpha);
        }
    }
}