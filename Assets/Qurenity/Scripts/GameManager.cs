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

	public PlayerInfo[] Player = new PlayerInfo[1];

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

	public const short DefaultLayer = 0;
	public const short TransparentFXLayer = 1;
	public const short UILayer = 5;
	public const short MapMeshesLayer = 8;
	public const short InvisibleBlockerLayer = 9;
	public const short PlayerLayer = 10;
	public const short DamageablesLayer = 11;
	public const short ThingsLayer = 12;
	public const short WalkTriggerLayer = 13;
	public const short RagdollLayer = 14;
	public const short ColliderLayer = 15;
	public const short CombinesMapMeshesLayer = 16;
	public const short MapMeshesPlayer1Layer = 17;
	public const short MapMeshesPlayer2Layer = 18;
	public const short MapMeshesPlayer3Layer = 19;
	public const short MapMeshesPlayer4Layer = 20;
	public const short CombinesThingsLayer = 21;
	public const short ThingsPlayer1Layer = 22;
	public const short ThingsPlayer2Layer = 23;
	public const short ThingsPlayer3Layer = 24;
	public const short ThingsPlayer4Layer = 25;
	public const short CombinesDamageablesLayer = 26;
	public const short DamageablesPlayer1Layer = 27;
	public const short DamageablesPlayer2Layer = 28;
	public const short DamageablesPlayer3Layer = 29;
	public const short DamageablesPlayer4Layer = 30;

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
					Player[0].playerThing.InitPlayer(0);
				}
			}
		}
	}
}
