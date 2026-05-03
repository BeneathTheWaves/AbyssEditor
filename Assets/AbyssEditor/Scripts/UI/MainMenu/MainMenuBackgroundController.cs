using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AbyssEditor.Scripts.UI.MainMenu
{
    [RequireComponent(typeof(RectTransform))]
    public class UIBackgroundCover : MonoBehaviour
    {
        [Header("Images")]
        [SerializeField] private List<Texture2D> images;

        [Header("Settings")]
        [SerializeField] private float cycleInterval = 3f;
        [SerializeField] private float fadeDuration = 0.75f;
        [SerializeField] private bool autoCycle = true;
        [SerializeField] private Vector2 driftAmount = new Vector2(30f, 20f);
        [SerializeField] private float driftSpeed = 0.2f;
        [SerializeField] private float zoomAmount = 0.05f;
        [SerializeField] private float baseCoverScale = 1.1f;

        private List<CanvasGroup> groups = new List<CanvasGroup>();
        private List<RectTransform> rects = new List<RectTransform>();
        private int currentIndex = 0;

        private void Awake()
        {
            if (images == null || images.Count == 0) { Debug.LogWarning("No textures assigned."); return; }
            
            currentIndex = Random.Range(0, images.Count);
            
            for (int i = 0; i < images.Count; i++)
            {
                Texture2D tex = images[i];
                
                if (tex == null) continue;

                GameObject go = new GameObject("BG_Image", typeof(RawImage), typeof(CanvasGroup), typeof(AspectRatioFitter));
                go.transform.SetParent(transform, false);

                RawImage raw = go.GetComponent<RawImage>();
                raw.texture = tex;
                raw.raycastTarget = false;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one * baseCoverScale;

                AspectRatioFitter fitter = go.GetComponent<AspectRatioFitter>();
                fitter.aspectRatio = (float)tex.width / tex.height;
                fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;

                CanvasGroup cs = go.GetComponent<CanvasGroup>();
                cs.alpha = (i == currentIndex) ? 1f : 0f;
                
                groups.Add(cs);
                rects.Add(rt);
            }
        }

        private void Start()
        {
            if (autoCycle && groups.Count > 0)
                StartCoroutine(CycleRoutine());
        }

        private void Update()
        {
            for (int i = 0; i < rects.Count; i++)
            {
                float t = Time.time * driftSpeed + i * 12.34f;
                rects[i].anchoredPosition = new Vector2(Mathf.Sin(t) * driftAmount.x, Mathf.Cos(t) * driftAmount.y);
                rects[i].localScale = Vector3.one * (baseCoverScale * (1f + Mathf.Sin(t * 0.8f) * zoomAmount));
            }
        }

        private void NextImage()
        {
            if (groups.Count == 0) return;
            StartCoroutine(FadeTo(Random.Range(0, groups.Count)));
        }

        private IEnumerator FadeTo(int nextIndex)
        {
            CanvasGroup prev = groups[currentIndex];
            CanvasGroup next = groups[nextIndex];

            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                float n = t / fadeDuration;
                prev.alpha = 1f - n;
                next.alpha = n;
                yield return null;
            }

            prev.alpha = 0f;
            next.alpha = 1f;
            currentIndex = nextIndex;
        }

        private IEnumerator CycleRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(cycleInterval);
                NextImage();
            }
        }
    }
}