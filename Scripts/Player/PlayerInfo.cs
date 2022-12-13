using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInfo : MonoBehaviour
{
	[HideInInspector]
	public PlayerControls playerControls;
	[HideInInspector]
	public PlayerCamera playerCamera;
	const int checkUpdateRate = 4;

	public int[] Ammo = new int[8] { 10, 0, 0, 0, 0, 0, 0, 0 }; //bullets, shells, grenades, rockets, lightning, slugs, cells, bfgammo
	public bool[] Weapon = new bool[9] { true, false, true, false, false, false, false, false, false }; //gauntlet, machinegun, shotgun, grenade launcher, rocket launcher, lightning gun, railgun, plasma gun, bfg10k
	public int[] MaxAmmo = new int[8] { 200, 200, 200, 200, 200, 200, 200, 200 };

	public bool godMode = false;
	public int MaxHealth = 100;
	public int MaxBonusHealth = 200;
	public int MaxArmor = 100;
	public int MaxBonusArmor = 200;

//	public PlayerWeapon[] WeaponPrefabs = new PlayerWeapon[9];

	void Awake()
	{
		playerControls = GetComponent<PlayerControls>();
		playerCamera = GetComponentInChildren<PlayerCamera>();
	}

	void Update()
    {
		int currentFrame = Time.frameCount;
		if (GameManager.Paused)
			return;
		if ((currentFrame & checkUpdateRate) == 0)
			CheckPVS(currentFrame);
	}

	private int FindCurrentLeaf()
	{
		// Cache the current position of player.
		Vector3 currentPos = transform.position;

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
	private void CheckPVS(int currentFrame)
	{
		// Find the index of the current leaf
		int leafIndex = FindCurrentLeaf();

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

				// Check if the surface has already been rendered in the current frame
				if (MapLoader.leafRenderFrame[surfaceId] == currentFrame)
					continue;

				// Set the surface as rendered in the current frame
				MapLoader.leafRenderFrame[surfaceId] = currentFrame;

				// Activate the cluster associated with the surface
				ClusterPVSManager.Instance.ActivateClusterBySurface(surfaceId);
			}
		}
	}
}
