using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterPVSController : MonoBehaviour
{
	public float visibleTime = 0.0f;
	bool isVisible = false;
	int lastlayer = GameManager.MapMeshesLayer;
	GameObject go;
	float lastCombineTime = 0;
	private void Awake()
	{
		go = gameObject;
	}
	public void RegisterClusterAndSurfaces(params QSurface[] surfaces)
	{
		ClusterPVSManager.Instance.Register(this, surfaces);
	}
	public void DectivateCluster()
	{
		isVisible = false;
		ChangeLayer(GameManager.MapMeshesLayer);
	}

	public void ChangeLayer(int layer)
	{
		lastlayer = layer;
		go.layer = lastlayer;
	}

	public void ActivateCluster(int layer)
	{
		visibleTime = 2.0f;
		if (isVisible)
		{
			if ((lastCombineTime > 0) && (lastCombineTime + 2f < Time.time))
				lastCombineTime = 0;
			else
			{
				if ((lastlayer == layer) || (lastlayer == GameManager.CombinesMapMeshesLayer))
					return;
				layer = GameManager.CombinesMapMeshesLayer;
				lastCombineTime = Time.time;
			}
		}
		ChangeLayer(layer);
		isVisible = true;
	}
}

