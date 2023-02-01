using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	public string autoloadMap = "";

	public int tessellations = 6;
	public float gamma = 1f;
	public static GameManager Instance;

	public Transform TemporaryObjectsHolder;
	public Transform BaseThingsHolder;

	public List<PlayerInfo> Player = new List<PlayerInfo>();

	public GameObject Blood;
	public GameObject BulletHit;
	public GameObject BulletMark;
	public GameObject BurnMark;
	public GameObject PlasmaMark;
	public GameObject SlugMark;

	public GameObject BulletCase;
	public GameObject ShogunShell;

	// Quake3 also uses Doom and Wolf3d scaling down
	public const float sizeDividor = 1f / 32f;
	public const float modelScale = 1f / 64f;

	public const short DefaultLayer =				0;
	public const short TransparentFXLayer =			1;

	public const short UI_P1Layer =					5;
	public const short UI_P2Layer =					6;
	public const short UI_P3Layer =					7;
	public const short UI_P4Layer =					8;

	public const short ColliderLayer =				9;
	public const short InvisibleBlockerLayer =		10;
	public const short ThingsLayer =				11;

	public const short MapMeshesLayer =				12;
	public const short CombinesMapMeshesLayer =		13;
	public const short MapMeshesPlayer1Layer =		14;
	public const short MapMeshesPlayer2Layer =		15;
	public const short MapMeshesPlayer3Layer =		16;
	public const short MapMeshesPlayer4Layer =		17;

	public const short DamageablesLayer =			18;
	public const short Player1Layer =				19;
	public const short Player2Layer =				20;
	public const short Player3Layer =				21;
	public const short Player4Layer =				22;

	public const short WalkTriggerLayer =			23;
	public const short RagdollLayer =				24;

	public const short UI3D_P1Layer =				25;
	public const short UI3D_P2Layer =				26;
	public const short UI3D_P3Layer =				27;
	public const short UI3D_P4Layer =				28;

	public const short NavMeshWalkableTag = 0;
	public const short NavMeshNotWalkableTag = 1;

	public bool paused = true;
	public static bool Paused { get { return Instance.paused; } }

	public float gravity = 25f;
	public float friction = 6;
	public float terminalVelocity = 100f;
	public float barrierVelocity = 1024f;

	public float PlayerDamageReceive = 1f;
	public int PlayerAmmoReceive = 1;

	public bool ready = false;
	public int skipFrames = 5;
	void Awake()
	{
		Instance = this;
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
		gameObject.AddComponent<PakManager>();
	}

	void Start()
    {
		if (Blood != null)
		{
			if (!PoolManager.HasObjectPool("BloodRed"))
				PoolManager.CreateObjectPool("BloodRed", Blood, 10);
		}
		if (BulletHit != null)
		{
			if (!PoolManager.HasObjectPool("BulletHit"))
				PoolManager.CreateObjectPool("BulletHit", BulletHit, 30);
		}
		if (BulletMark != null)
		{
			if (!PoolManager.HasObjectPool("BulletMark"))
				PoolManager.CreateObjectPool("BulletMark", BulletMark, 30);
		}
		if (BurnMark != null)
		{
			if (!PoolManager.HasObjectPool("BurnMark"))
				PoolManager.CreateObjectPool("BurnMark", BurnMark, 30);
		}
		if (PlasmaMark != null)
		{
			if (!PoolManager.HasObjectPool("PlasmaMark"))
				PoolManager.CreateObjectPool("PlasmaMark", PlasmaMark, 30);
		}
		if (SlugMark != null)
		{
			if (!PoolManager.HasObjectPool("SlugMark"))
				PoolManager.CreateObjectPool("SlugMark", SlugMark, 10);
		}
		//Ammo Catridge Cases
		if (BulletCase != null)
		{
			if (!PoolManager.HasObjectPool("BulletCase"))
				PoolManager.CreateRigidBodyPool("BulletCase", BulletCase, 30);
		}
		if (ShogunShell != null)
		{
			if (!PoolManager.HasObjectPool("ShogunShell"))
				PoolManager.CreateRigidBodyPool("ShogunShell", ShogunShell, 5);
		}

		if (!PoolManager.HasObjectPool("3DSound"))
			PoolManager.Create3DSoundPool("3DSound", 10);

		if (MapLoader.Load(autoloadMap))
		{
			MaterialManager.GetShaderAnimationsTextures();
			ClusterPVSManager.Instance.ResetClusterList();
			MapLoader.GenerateMapCollider();
			MapLoader.GenerateSurfaces();
			ClusterPVSManager.Instance.ResetGroups();
			Mesher.ClearMesherCache();
		}
		ThingsManager.AddThingsToMap();
		ready = true;

		MapLoader.noMarks.Add(Player[0].playerControls.capsuleCollider);
		MusicPlayer.Instance.Play(autoloadMap);
	}
	void OnApplicationFocus(bool hasFocus)
	{
		if (hasFocus)
		{
			Cursor.lockState = CursorLockMode.Locked;
			Cursor.visible = false;
			paused = false;
		}
		else
			paused = true;
	}
	void Update()
    {
		//skip frames are used to easen up Time.deltaTime after loading
		if (ready)
		{
			if (skipFrames > 0)
			{
				skipFrames--;

				if (skipFrames == 0)
				{
					paused = false;
					Player[0].playerThing.InitPlayer();
				}
			}
		}
	}

	//There must be a better way to assign this
	public void UpdatePlayers()
	{
		int numPlayers = Player.Count;

		if (numPlayers == 1)
		{
			Player[0].playerThing.playerCamera.UpdateRect(new Rect(0, 0, 1, 1));
			Player[0].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P1Layer);
		}
		else if (numPlayers == 2)
		{
			Player[0].playerThing.playerCamera.UpdateRect(new Rect(0, 0.5f, 1, 0.5f));
			Player[0].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P1Layer);

			Player[1].playerThing.playerCamera.UpdateRect(new Rect(0, 0, 1, 0.5f));
			Player[1].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P2Layer);
		}
		else if (numPlayers == 3)
		{
			Player[0].playerThing.playerCamera.UpdateRect(new Rect(0, 0.5f, 1, 0.5f));
			Player[0].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P1Layer);

			Player[1].playerThing.playerCamera.UpdateRect(new Rect(0, 0, 0.5f, 0.5f));
			Player[1].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P2Layer);

			Player[2].playerThing.playerCamera.UpdateRect(new Rect(0.5f, 0, 0.5f, 0.5f));
			Player[2].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P3Layer);
		}
		else if (numPlayers == 4)
		{
			Player[0].playerThing.playerCamera.UpdateRect(new Rect(0, 0.5f, 0.5f, 0.5f));
			Player[0].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P1Layer);

			Player[1].playerThing.playerCamera.UpdateRect(new Rect(0, 0, 0.5f, 0.5f));
			Player[1].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P2Layer);

			Player[2].playerThing.playerCamera.UpdateRect(new Rect(0.5f, 0, 0.5f, 0.5f));
			Player[2].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P3Layer);

			Player[3].playerThing.playerCamera.UpdateRect(new Rect(0.5f, 0.5f, 0.5f, 0.5f));
			Player[3].playerThing.playerInfo.playerHUD.UpdateLayer(UI_P4Layer);
		}
	}

	public static void SetLayerAllChildren(Transform root, int layer)
	{
		var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
		foreach (var child in children)
			child.gameObject.layer = layer;
	}
}
