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
		int i = 0;
		Vector3 currentPos = transform.position;
	
		while (i >= 0)
		{
			QNode node = MapLoader.nodes[i];
			QPlane slitPlane = MapLoader.planes[node.plane];

			if (slitPlane.GetSide(currentPos))
			{
				i = node.front;
			}
			else
			{
				i = node.back;
			}
		}

		return ~i;
	}

	private bool IsClusterVisible(int current, int test)
	{
		if (MapLoader.visData.bitSets.Length == 0)
			return true;

//no-clipping don't draw
		if (current < 0)
			return false;

		int testIndex = test / 8;
		byte visTest = MapLoader.visData.bitSets[(current * MapLoader.visData.bytesPerCluster) + (testIndex)];

		bool visible = ((visTest & (1 << (test & 7))) != 0);

		return visible;
	}
	private void CheckPVS(int currentFrame)
	{
		int leafIndex = FindCurrentLeaf();

		int cluster = MapLoader.leafs[leafIndex].cluster;

		int i = MapLoader.leafs.Count;

		while (i-- != 0)
		{
			QLeaf leaf = MapLoader.leafs[i];

			if (!IsClusterVisible(cluster, leaf.cluster))
				continue;

			int surfaceCount = leaf.numOfLeafFaces;

			while (surfaceCount-- != 0)
			{
				int surfaceId = MapLoader.leafsSurfaces[leaf.leafSurface + surfaceCount];
				if (MapLoader.leafRenderFrame[surfaceId] == currentFrame)
					continue;

				MapLoader.leafRenderFrame[surfaceId] = currentFrame;
				ClusterPVSManager.Instance.ActivateClusterBySurface(surfaceId);
			}
		}
	}
}
