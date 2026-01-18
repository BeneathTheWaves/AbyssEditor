using ReefEditor.ContentLoading;
using UnityEngine;
using UnityEngine.UI;

namespace ReefEditor.UI
{
	internal class UIBlocktypePreview : MonoBehaviour
	{
		public Image matImage { get; private set; }
		public Image matImageTwo { get; private set; }
		public int matNumber { get; private set; }

		public void Start()
		{
			matImage = gameObject.GetComponent<Image>();
			matImageTwo = gameObject.transform.GetChild(0).GetComponent<Image>();

			matImageTwo.gameObject.SetActive(false);
		}

		public void UpdatePreview(int materialNumber, BlocktypeMaterial blockMaterial)
		{
			matNumber = materialNumber;

			Texture2D mainTexture = blockMaterial.MainTexture;
			Texture2D sideTexture = blockMaterial.SideTexture;

			if(mainTexture != null)
			{
				matImage.sprite = Sprite.Create(MakeOpaque(mainTexture), new Rect(0f, 0f, mainTexture.width, mainTexture.height), new Vector2(0.5f, 0.5f), mainTexture.width);
			}
			
			if(sideTexture != null)
			{
				matImageTwo.sprite = Sprite.Create(MakeOpaque(sideTexture), new Rect(0f, 0f, sideTexture.width, sideTexture.height), new Vector2(0.5f, 0.5f), sideTexture.width);
				matImageTwo.gameObject.SetActive(true);
			}else if (matImageTwo.gameObject.activeSelf)
			{
				matImageTwo.gameObject.SetActive(false);
			}
		}

		private Texture2D MakeOpaque(Texture2D texture)
		{
			Texture2D returnTexture = new Texture2D(texture.width, texture.height);

			Color[] colors = texture.GetPixels();

			for(int i = 0; i < colors.Length; i++)
			{
				colors[i].a = 1;
			}

			returnTexture.SetPixels(colors);
			returnTexture.Apply();

			return returnTexture;
		}
	}
}
