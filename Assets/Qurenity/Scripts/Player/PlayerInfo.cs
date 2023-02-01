using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
	[HideInInspector]
	public PlayerControls playerControls;
	[HideInInspector]
	public PlayerCamera playerCamera;
	[HideInInspector]
	public PlayerThing playerThing;
	[HideInInspector]
	public PlayerHUD playerHUD;
	[HideInInspector]
	public GameObject player;
	[HideInInspector]
	public int playerLayer = GameManager.DamageablesLayer;
	public Canvas UICanvas;
	public Transform WeaponHand;

	const int checkUpdateRate = 4;

	public int[] Ammo = new int[8] { 100, 0, 0, 0, 0, 0, 0, 0 }; //bullets, shells, grenades, rockets, lightning, slugs, cells, bfgammo
	public bool[] Weapon = new bool[9] { false, true, false, false, false, false, false, false, false }; //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k
	public int[] MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

	public bool godMode = false;
	public int MaxHealth = 100;
	public int MaxBonusHealth = 200;
	public int MaxArmor = 100;
	public int MaxBonusArmor = 200;

	public PlayerWeapon[] WeaponPrefabs = new PlayerWeapon[9];

	void Awake()
	{
		playerControls = GetComponent<PlayerControls>();
		playerThing = GetComponentInParent<PlayerThing>();
		playerCamera = GetComponentInChildren<PlayerCamera>();
		playerHUD  = UICanvas.GetComponent<PlayerHUD>();
		player = playerThing.gameObject;
	}

	void Start()
	{
		playerLayer += GameManager.Instance.Player.Count;
		if (!GameManager.Instance.Player.Contains(this))
		{
			playerLayer++;
			GameManager.Instance.Player.Add(this);
			MapLoader.noMarks.Add(playerControls.capsuleCollider);

			if (GameManager.Instance.Player.Count == 3)
				playerThing.modelName = "Visor";
			playerThing.InitPlayer();
		}
		GameManager.Instance.UpdatePlayers();
	}

	public void Reset()
	{
		Ammo = new int[8] { 100, 0, 0, 0, 0, 100, 0, 0 };
		Weapon = new bool[9] { false, true, false, false, false, false, true, false, false };
		MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

		godMode = false;

		playerHUD.pickupFlashTime = 0f;
		playerHUD.painFlashTime = 0f;

		playerThing.hitpoints = 100;
		playerThing.armor = 0;

		playerControls.impulseVector = Vector3.zero;
		playerControls.CurrentWeapon = -1;
		playerControls.SwapWeapon = -1;
		playerControls.SwapToBestWeapon();

		playerHUD.HUDUpdateAmmoNum();
		playerHUD.HUDUpdateHealthNum();
		playerHUD.HUDUpdateArmorNum();
	}
	void Update()
    {
		int currentFrame = Time.frameCount;

		if (GameManager.Paused)
			return;

		if ((currentFrame & checkUpdateRate) == 0)
			CheckPVS(currentFrame,transform.position);
	}

	private int FindCurrentLeaf(Vector3 currentPos)
	{
		// Search trought the BSP tree until the index is negative, and indicate it's a leaf.
		int i = 0;
		while (i >= 0)
		{
			// Retrieve the node and slit Plane
			QNode node = MapLoader.nodes[i];
			QPlane slitPlane = MapLoader.planes[node.plane];

			// Determine whether the current position is on the front or back side of this plane.
			if (slitPlane.GetSide(currentPos))
			{
				// If the current position is on the front side this is the index our new tree node
				i = node.front;
			}
			else
			{
				// Otherwise, the back is the index our new tree node
				i = node.back;
			}
		}
		//  abs(index value + 1) is our leaf
		return ~i;
	}

	private bool IsClusterVisible(int current, int test)
	{
		// If the bitSets array is empty, make all the clusters as visible
		if (MapLoader.visData.bitSets.Length == 0)
			return true;

		// If the player is no-clipping then don't draw
		if (current < 0)
			return false;

		// Calculate the index of the test cluster in the bitSets array
		int testIndex = test / 8;

		// Retrieve the appropriate byte from the bitSets array
		byte visTest = MapLoader.visData.bitSets[(current * MapLoader.visData.bytesPerCluster) + (testIndex)];

		// Check if the test cluster is marked as visible in the retrieved byte
		bool visible = ((visTest & (1 << (test & 7))) != 0);

		// Return whether or not the cluster is visible
		return visible;
	}
	public void CheckPVS(int currentFrame, Vector3 currentPos)
	{
		// Find the index of the current leaf
		int leafIndex = FindCurrentLeaf(currentPos);

		// Get the cluster the current leaf belongs to
		int cluster = MapLoader.leafs[leafIndex].cluster;

		// Loop through all leafs in the map
		int i = MapLoader.leafs.Count;
		while (i-- != 0)
		{
			QLeaf leaf = MapLoader.leafs[i];

			// Check if the leaf's cluster is visible from the current leaf's cluster
			if (!IsClusterVisible(cluster, leaf.cluster))
				continue;

			// Loop through all the surfaces in the leaf
			int surfaceCount = leaf.numOfLeafFaces;
			while (surfaceCount-- != 0)
			{
				int surfaceId = MapLoader.leafsSurfaces[leaf.leafSurface + surfaceCount];

				// Check if the surface has already been rendered in the current frame and that the layer is not the same
				if ((MapLoader.leafRenderFrame[surfaceId] == currentFrame) && ((MapLoader.leafRenderLayer[surfaceId] == playerLayer - 5) || (MapLoader.leafRenderLayer[surfaceId] == GameManager.CombinesMapMeshesLayer)))
					continue;

				// Check if the surface has already been rendered in the current frame, assign layer
				if (MapLoader.leafRenderFrame[surfaceId] == currentFrame)
					MapLoader.leafRenderLayer[surfaceId] = GameManager.CombinesMapMeshesLayer;
				else
					MapLoader.leafRenderLayer[surfaceId] = playerLayer - 5;
			
				// Set the surface as rendered in the current frame
				MapLoader.leafRenderFrame[surfaceId] = currentFrame;

				// Activate the cluster associated with the surface
				ClusterPVSManager.Instance.ActivateClusterBySurface(surfaceId, MapLoader.leafRenderLayer[surfaceId]);
			}
		}
	}
}
