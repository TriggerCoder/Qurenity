using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterPVSManager : MonoBehaviour
{
	public static ClusterPVSManager Instance;
	private List<ClusterPVSController> AllClusters = new List<ClusterPVSController>();
	private ClusterPVSController[] SurfaceToCluster;
	public int groups = -1;
	public const int maxGroupSize = 100;
	int currentGroup = 0;
	float[] updateDelta;
	void Awake()
	{
		Instance = this;
	}

	void FixedUpdate()
	{
		int start, end;

		if (GameManager.Paused)
			return;

		start = currentGroup++ * maxGroupSize;
		if (currentGroup > groups)
			end = AllClusters.Count;
		else
			end = currentGroup * maxGroupSize;

		for (int i = start; i < end; i++)
		{
			if (AllClusters[i] != null)
			{
				if (AllClusters[i].visibleTime > 0)
					AllClusters[i].visibleTime -= updateDelta[currentGroup - 1];
				if (AllClusters[i].visibleTime < 0)
				{
					AllClusters[i].visibleTime = 0;
					AllClusters[i].DectivateCluster();
				}
			}
		}

		for (int i = 0; i < groups; ++i)
			updateDelta[i] += Time.fixedDeltaTime;

		updateDelta[currentGroup - 1] = 0;
		if (currentGroup > groups)
			currentGroup = 0;
	}
	public void ResetGroups()
	{
		groups = Mathf.FloorToInt(AllClusters.Count / maxGroupSize);
		updateDelta = new float[groups + 1];
		currentGroup = 0;
	}
	public void ResetClusterList()
	{
		AllClusters = new List<ClusterPVSController>();
		SurfaceToCluster = new ClusterPVSController[MapLoader.surfaces.Count];
	}

	public void Register(ClusterPVSController cluster, params QSurface[] surfaces)
	{
		for (int i = 0; i < surfaces.Length; i++)
		{
			SurfaceToCluster[surfaces[i].surfaceId] = cluster;
		}
		AllClusters.Add(cluster);
	}
	public void ActivateClusterBySurface(int surface, int layer)
	{
		ClusterPVSController cluster = SurfaceToCluster[surface];
		if (cluster == null)
		{
//			Debug.LogWarning("Cluster not found for surface: " + surface);
			return;
		}
		cluster.ActivateCluster(layer);
	}
}
