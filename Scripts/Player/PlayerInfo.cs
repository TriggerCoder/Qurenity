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
		// This function takes in our camera position, then goes and walks
		// through the BSP nodes, starting at the root node, finding the leaf node
		// that our camera resides in.  This is done by checking to see if
		// the camera is in front or back of each node's splitting plane.
		// If the camera is in front of the camera, then we want to check
		// the node in front of the node just tested.  If the camera is behind
		// the current node, we check that nodes back node.  Eventually, this
		// will find where the camera is according to the BSP tree.  Once a
		// node index (i) is found to be a negative number, that tells us that
		// that index is a leaf node, not another BSP node.  We can either calculate
		// the leaf node index from -(i + 1) or ~1.  This is because the starting
		// leaf index is 0, and you can't have a negative 0.  It's important
		// for us to know which leaf our camera is in so that we know which cluster
		// we are in.  That way we can test if other clusters are seen from our cluster.

		// Continue looping until we find a negative index
		while (i >= 0)
		{
			// Get the current node, then find the slitter plane from that
			// node's plane index. 
			Node node = MapLoader.nodes[i];
			Plane slitPlane = MapLoader.planes[node.plane];

/*			GameObject SlitPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
			SlitPlane.name = "Node "+ i + " Plane " + node.plane;
			SlitPlane.transform.forward = slitPlane.normal;
			SlitPlane.transform.position = slitPlane.normal * slitPlane.distance;
			SlitPlane.AddComponent<DestroyAfterTime>();
*/			// If the camera is in front of the plane
//			float distance = Vector3.Dot(slitPlane.normal, currentPos) - slitPlane.distance;
//			if (distance >= 0)
			if (slitPlane.GetSide(currentPos))
			{
				// Assign the current node to the node in front of itself
				i = node.front;
			}
			// Else if the camera is behind the plane
			else
			{
				// Assign the current node to the node behind itself
				i = node.back;
			}
		}

		// Return the leaf index (same thing as saying:  return -(i + 1)).
		return ~i;  // Binary operation
	}

	private bool IsClusterVisible(int current, int test)
	{
		// This function is used to test the "current" cluster against
		// the "test" cluster.  If the "test" cluster is seen from the
		// "current" cluster, we can then draw it's associated faces, assuming
		// they aren't frustum culled of course.  Each cluster has their own
		// bitset containing a bit for each other cluster.  For instance, if there
		// is 10 clusters in the whole level (a tiny level), then each cluster
		// would have a bitset of 10 bits that store a 1 (visible) or a 0 (not visible) 
		// for each other cluster.  Bitsets are used because it's faster and saves
		// memory, instead of creating a huge array of booleans.  It seems that
		// people tend to call the bitsets "vectors", so keep that in mind too.

		// Make sure we have valid memory and that the current cluster is > 0.
		// If we don't have any memory or a negative cluster, return a visibility (1).
		if ((MapLoader.visData.bitSets.Length == 0) || current < 0)
			return true;

		// Use binary math to get the 8 bit visibility set for the current cluster
		byte visSet = MapLoader.visData.bitSets[(current * MapLoader.visData.bytesPerCluster) + (test / 8)];

		// Now that we have our vector (bitset), do some bit shifting to find if
		// the "test" cluster is visible from the "current" cluster, according to the bitset.
		bool result = ((visSet & (1 << (test & 7))) != 0);

		// Return the result ( either 1 (visible) or 0 (not visible) )
		return result;
	}
	private void CheckPVS(int currentFrame)
	{
		// Grab the leaf index that our camera is in
		int leafIndex = FindCurrentLeaf();

		// Grab the cluster that is assigned to the leaf
		int cluster = MapLoader.leafs[leafIndex].cluster;

		// Initialize our counter variables (start at the last leaf and work down)
		int i = MapLoader.leafs.Count;

		while (i-- != 0)
		{
			// Get the current leaf that is to be tested for visibility from our camera's leaf
			Leaf leaf = MapLoader.leafs[i];

			// If the current leaf can't be seen from our cluster, go to the next leaf
			if (!IsClusterVisible(cluster, leaf.cluster))
				continue;

/*			Plane forwardPlane = new Plane(transform.forward, transform.position);

			// If the current leaf is not in the camera's frustum, go to the next leaf
			if ((!forwardPlane.GetSide(leaf.bb_Min)) && (!forwardPlane.GetSide(leaf.bb_Max)))
				continue;
*/
			// If we get here, the leaf we are testing must be visible in our camera's view.
			// Get the number of faces that this leaf is in charge of.
			int faceCount = leaf.numOfLeafFaces;

			// Loop through and render all of the faces in this leaf
			while (faceCount-- != 0)
			{
				// Grab the current face index from our leaf faces array
				int faceId = MapLoader.leafsFaces[leaf.leafFace + faceCount];
				if (MapLoader.leafRenderFrame[faceId] == currentFrame)
					continue;

				MapLoader.leafRenderFrame[faceId] = currentFrame;
				ClusterPVSManager.Instance.ActivateClusterByFace(faceId);
			}
		}
	}
}
