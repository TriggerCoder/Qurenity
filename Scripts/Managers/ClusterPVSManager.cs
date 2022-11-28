using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterPVSManager : MonoBehaviour
{
	public static ClusterPVSManager Instance;
	private List<ClusterPVSController> AllClusters = new List<ClusterPVSController>();
	private Dictionary<int, List<ClusterPVSController>> FacesToCluster = new Dictionary<int, List<ClusterPVSController>>();
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
		FacesToCluster = new Dictionary<int, List<ClusterPVSController>>();
	}
	public void Register(ClusterPVSController cluster, List<int> facesIndices)
	{
		foreach (int index in facesIndices)
		{
			if (!FacesToCluster.ContainsKey(index))
			{
				List<ClusterPVSController> listCluster = new List<ClusterPVSController>();
				listCluster.Add(cluster);
				FacesToCluster.Add(index, listCluster);
			}
			else if (!FacesToCluster[index].Contains(cluster))
				FacesToCluster[index].Add(cluster);
		}
		AllClusters.Add(cluster);
	}

	public void ActivateClusters(List<int> facesToActivate)
	{
		foreach (int face in facesToActivate)
		{
			if (FacesToCluster.ContainsKey(face))
			{
				foreach (ClusterPVSController cluster in FacesToCluster[face])
				{
					cluster.ActivateCluster(GameManager.MapMeshesPlayer1Layer);
				}
			}
		}
	}
}
