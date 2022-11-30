using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterPVSController : MonoBehaviour
{
	public float visibleTime = 0.0f;
	bool isVisible = false;
	int lastlayer = GameManager.MapMeshesLayer;
	public void RegisterClusterAndFaces(params Face[] faces)
	{
		ClusterPVSManager.Instance.Register(this, faces);
	}
	public void DectivateCluster()
	{
		isVisible = false;
		lastlayer = GameManager.MapMeshesLayer;
		gameObject.layer = lastlayer;
	}

	public void ActivateCluster(int layer)
	{
		if (isVisible)
		{
			if (lastlayer != layer)
				layer = GameManager.CombinesMapMeshesLayer;
		}
		visibleTime = 2.0f;
		lastlayer = layer;
		gameObject.layer = lastlayer;
		isVisible = true;
	}
}

