using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.MainMenu
{
    [RequireComponent(typeof(RawImage))]
    public class Vignette : MonoBehaviour
    {
        [SerializeField] private int resolution = 512;
        [SerializeField] [Range(1f, 6f)] private float strength = 4f;
        [SerializeField] [Range(0f, 1f)] private float opacity = 0.35f;

        private void Awake()
        {
            var rt = GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = GetComponent<RawImage>();
            img.texture = GenerateVignetteTexture(resolution, resolution, strength, opacity);
            img.raycastTarget = false;
        }

        private Texture2D GenerateVignetteTexture(int w, int h, float strength, float opacity)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color[w * h];

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float nx = (x / (float)w) * 2f - 1f;
                float ny = (y / (float)h) * 2f - 1f;
                float alpha = Mathf.Clamp01(Mathf.Pow(Mathf.Sqrt(nx * nx + ny * ny), strength)) * opacity;
                pixels[y * w + x] = new Color(0, 0, 0, alpha);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
