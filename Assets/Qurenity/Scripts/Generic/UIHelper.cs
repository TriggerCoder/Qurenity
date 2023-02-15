using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[System.Serializable]
public class UIItem
{
	public GameObject gameObject;
	public RectTransform trans;
	public CanvasRenderer canvas;
	public Image image;
	public Text text;
}

public class UIHelper
{
	public const float virtualWidth = 1280.0f;
	public const float virtualHeight = 720.0f;

	public static UIItem CreateUIObject(string objectName, Transform parent)
	{
		UIItem uiItem = new UIItem();
		uiItem.gameObject = new GameObject();
		uiItem.gameObject.name = objectName;
		uiItem.gameObject.layer = parent.gameObject.layer;
		uiItem.gameObject.SetActive(false);
		uiItem.gameObject.transform.SetParent(parent);

		uiItem.trans = uiItem.gameObject.AddComponent<RectTransform>();
		uiItem.trans.anchoredPosition = new Vector2(0.5f, 0.5f);
		uiItem.trans.localPosition = Vector3.zero;
		uiItem.trans.localScale = Vector3.one;
		uiItem.trans.anchorMin = new Vector2(0, 0);
		uiItem.trans.anchorMax = new Vector2(1, 1);

		uiItem.canvas = uiItem.gameObject.GetComponent<CanvasRenderer>();
		if (uiItem.canvas == null)
			uiItem.canvas = uiItem.gameObject.AddComponent<CanvasRenderer>();

		return uiItem;
	}

	public static UIItem CreateUIObject(string objectName, Transform parent, Font font, int fontSize, Color color, TextAnchor aligment)
	{
		UIItem uiItem = CreateUIObject(objectName, parent);
		uiItem.text = uiItem.gameObject.AddComponent<Text>();
		uiItem.text.font = font;
		uiItem.text.color = color;
		uiItem.text.fontSize = fontSize;
		uiItem.text.alignment = aligment;

		return uiItem;
	}
	public static UIItem CreateUIObject(string objectName, Transform parent, Texture2D scrTex)
	{
		UIItem uiItem = CreateUIObject(objectName, parent);
		uiItem.image = uiItem.gameObject.AddComponent<Image>();
		uiItem.image.raycastTarget = false;
		uiItem.image.sprite = CreateFromTexture2D(scrTex);
		return uiItem;
	}
	public static void InitUIObject(UIItem uiItem, float sizeDeltaX, float sizeDeltaY, float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY, float anchoredPositionX, float anchoredPositionY, bool setactive = true, float pivotX = 0.5f, float pivotY = 0.5f)
	{
		RectTransform trans = uiItem.gameObject.GetComponent<RectTransform>();
		trans.sizeDelta = new Vector2(sizeDeltaX, sizeDeltaY);
		trans.pivot = new Vector2(pivotX, pivotY);
		trans.anchorMin = new Vector2(anchorMinX, anchorMinY);
		trans.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
		trans.anchoredPosition = new Vector3(anchoredPositionX, anchoredPositionY, 0);
		trans.localScale = new Vector3(1, 1, 1);
		if (setactive)
			uiItem.gameObject.SetActive(true);
		return;
	}
	public static Sprite CreateFromTexture2D(Texture2D scrTex)
	{
		Sprite destSprite = Sprite.Create(scrTex, new Rect(0, 0, scrTex.width, scrTex.height), new Vector2(0.5f, 0.5f));
		return destSprite;
	}
}
