using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IconGenerator : MonoBehaviour
{
    public Camera cam;
    public GameObject sceneParent;
    public RenderTexture renderTexture;

    private static IconGenerator instance;

    private void Awake()
    {
        instance = this;
    }

    public static IEnumerator GenerateIcon(GameObject prefab, IconOutput output)
    {
        instance.sceneParent.SetActive(true);
        var model = Instantiate(prefab);
        model.SetActive(true);
        model.transform.position = instance.transform.position;
        var bounds = GetObjectBounds(model);
        instance.cam.transform.position = model.transform.position + Vector3.ClampMagnitude(bounds.extents, 30) + new Vector3(1.5f, -0.5f, 2);
        yield return null;
        RenderTexture.active = instance.renderTexture;
        Texture2D tex = new Texture2D(256, 256, TextureFormat.RGBA32, false, true);
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0, false);
        tex.Apply(false);
        instance.sceneParent.SetActive(false);
        Destroy(model);
        output.OutputSprite = Sprite.Create(tex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f));
    }

    private static Bounds GetObjectBounds(GameObject obj)
    {
        Bounds bounds = new Bounds(obj.transform.position, Vector3.one * 1f);
        foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
        {
            bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }

    public class IconOutput
    {
        public Sprite OutputSprite { get; set; }
    } 
}