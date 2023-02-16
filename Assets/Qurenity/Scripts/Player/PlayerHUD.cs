using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
	public GameObject player;
	public GameObject statusBar;
	[HideInInspector]
	public Text displayText;
	[HideInInspector]
	public PlayerInfo playerInfo;
	[HideInInspector]
	public PlayerThing playerThing;
	[HideInInspector]
	public PlayerControls playerControls;
	[HideInInspector]
	public PlayerCamera playerCamera;
	[HideInInspector]
	public Canvas canvas;

	public Texture2D pickupTexture;
	public Texture2D painTexture;
	public Texture2D crosshairTexture;

	public Font GuiFont;
	public Material FontMat;

	public float pickupFlashTime;
	public float painFlashTime;

	Image currentcrosshair;

	CanvasScaler canvasScaler;
	const float WideScreen = 16.0f / 9.0f;

	Color TargetColor = new Color(1f, 1f, 1f, 0f);

	private UIItem pickupEffect;
	private UIItem painEffect;
	private UIItem crosshair;
	private UIItem AmmoBar;
	private UIItem HealthBar;
	private UIItem ArmorBar;

	void Awake()
	{
		playerThing = player.GetComponent<PlayerThing>();
		playerInfo = player.GetComponentInChildren<PlayerInfo>();
		playerControls = player.GetComponentInChildren<PlayerControls>();
		playerCamera = player.GetComponentInChildren<PlayerCamera>();
		canvas = GetComponentInChildren<Canvas>();

		pickupEffect = UIHelper.CreateUIObject("pickupEffect", transform, pickupTexture);
		painEffect = UIHelper.CreateUIObject("painEffect", transform, painTexture);
		crosshair = UIHelper.CreateUIObject("crosshairObject", transform, crosshairTexture);

		AmmoBar = UIHelper.CreateUIObject("AmmoBar", statusBar.transform, GuiFont, 100, new Color(1, 1, 1, .5f), TextAnchor.LowerLeft);
		AmmoBar.text.material = FontMat;
		HealthBar = UIHelper.CreateUIObject("HealthBar", statusBar.transform, GuiFont, 100, new Color(1, 1, 1, .5f), TextAnchor.LowerLeft);
		HealthBar.text.material = FontMat;
		ArmorBar = UIHelper.CreateUIObject("ArmorBar", statusBar.transform, GuiFont, 100, new Color(1, 1, 1, .5f), TextAnchor.LowerLeft);
		ArmorBar.text.material = FontMat;

#if UNITY_EDITOR
		if ((Screen.width / Screen.height) > WideScreen)
#else
		if ((Screen.currentResolution.width / Screen.currentResolution.height) > WideScreen)
#endif
		{
			canvasScaler = GetComponent<CanvasScaler>();
			if (canvasScaler != null)
				canvasScaler.matchWidthOrHeight = 1;
		}
	}

	private void Start()
	{
		AmmoBar.gameObject.SetActive(true);
		HealthBar.gameObject.SetActive(true);
		ArmorBar.gameObject.SetActive(true);
		UIHelper.InitUIObject(crosshair, 64, 64, 0.5f, 0.5f, 0.5f, 0.5f, 0, 0);
	}

	public void UpdateLayer(int layer, bool is3D = false)
	{
		gameObject.layer = layer;
		GameManager.SetLayerAllChildren(transform, layer);
	}

	public void DisableLayout()
	{
		HorizontalLayoutGroup horizontalLayout = statusBar.GetComponent<HorizontalLayoutGroup>();
		horizontalLayout.childScaleHeight = false;
		horizontalLayout.childScaleWidth = false;
		horizontalLayout.childControlHeight = false;
		horizontalLayout.childControlWidth = false;
		horizontalLayout.childForceExpandHeight = false;
		horizontalLayout.childForceExpandWidth = false;
	}

	int skipFrames = 10;
	void Update()
	{
		if (GameManager.Paused)
			return;

		if (!GameManager.Instance.ready)
			return;

		UpdateHUDTimes();
		if (skipFrames > 0)
		{
			skipFrames--;

			if (skipFrames == 0)
			{
				playerInfo.playerHUD.DisableLayout();
			}
		}
		
	}
	void UpdateHUDTimes()
	{
		if (painFlashTime > 0f)
		{
			if (painEffect.gameObject.activeSelf == false)
			{
				painEffect.canvas.SetColor(Color.white * (painFlashTime * .4f));
				painEffect.gameObject.SetActive(true);
			}
			painFlashTime -= Time.deltaTime;
			painEffect.canvas.SetColor(Color.Lerp(painEffect.canvas.GetColor(), TargetColor, 3 * Time.deltaTime));
		}
		else if (painEffect.gameObject.activeSelf == true)
		{
			painFlashTime = 0;
			painEffect.gameObject.SetActive(false);
		}

		if (pickupFlashTime > 0f)
		{
			if (pickupEffect.gameObject.activeSelf == false)
			{
				pickupEffect.canvas.SetColor(Color.white * (pickupFlashTime * .4f));
				pickupEffect.gameObject.SetActive(true);
			}
			pickupFlashTime -= Time.deltaTime;
			pickupEffect.canvas.SetColor(Color.Lerp(pickupEffect.canvas.GetColor(), TargetColor, 10 * Time.deltaTime));

		}
		else if (pickupEffect.gameObject.activeSelf == true)
		{
			pickupFlashTime = 0;
			pickupEffect.gameObject.SetActive(false);
		}
	}

	public void HUDUpdateAmmoNum()
	{
		string Ammo;
		switch (playerControls.CurrentWeapon)
		{
			default:
			case 0:
				Ammo = "";
				break;
			case 1:
				Ammo = "" + playerInfo.Ammo[0];
				break;
			case 2:
				Ammo = "" + playerInfo.Ammo[1];
			break;
			case 3:
				Ammo = "" + playerInfo.Ammo[2];
				break;
			case 4:
				Ammo = "" + playerInfo.Ammo[3];
				break;
			case 5:
				Ammo = "" + playerInfo.Ammo[4];
				break;
			case 6:
				Ammo = "" + playerInfo.Ammo[5];
				break;
			case 7:
				Ammo = "" + playerInfo.Ammo[6];
				break;
			case 8:
				Ammo = "" + playerInfo.Ammo[7];
				break;
		}
		AmmoBar.text.text = " " + Ammo;
	}
	public void HUDUpdateHealthNum()
	{
		HealthBar.text.text = " " + playerThing.hitpoints;
	}

	public void HUDUpdateArmorNum()
	{
		ArmorBar.text.text = " "+playerThing.armor;
	}
}
