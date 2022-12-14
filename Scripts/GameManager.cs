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

	// Quake3 also uses Doom and Wolf3d scaling down
	public const float sizeDividor = 1f / 32f;
	public const short DefaultLayer = 0;
	public const short TransparentFXLayer = 1;
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
	void Awake()
	{
		Instance = this;
		gameObject.AddComponent<PakManager>();
	}

	void Start()
    {
		if (MapLoader.Load(autoloadMap))
		{
			MaterialManager.GetShaderAnimationsTextures();
			ClusterPVSManager.Instance.ResetClusterList();
			MapLoader.GenerateSurfaces();
			MapLoader.GenerateMapCollider();
			ClusterPVSManager.Instance.ResetGroups();
			Mesher.ClearMesherCache();
		}
		paused = false;
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
        
    }
}
