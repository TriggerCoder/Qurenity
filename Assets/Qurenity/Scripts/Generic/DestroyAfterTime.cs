using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
	public float _lifeTime = 1;
	float time = 0f;

	void Update()
	{
		//so we clear visual effects at the end of campaign
		if (GameManager.Paused)
			return;

		time += Time.deltaTime;

		if (time >= _lifeTime)
			Destroy(gameObject);
	}

	public static void DestroyObject(GameObject gO)
	{
		Transform temp;
		MeshFilter meshFilter;
		MeshRenderer meshRenderer;

		temp = gO.transform;
		for (int d = 0; d < temp.childCount; d++)
		{
			meshFilter = temp.GetChild(d).gameObject.transform.GetComponent<MeshFilter>();
			if (meshFilter)
			{
				//				Debug.Log("DestroyAfterTime: meshFilter is \"" + meshFilter.name + "\"");
				Destroy(meshFilter.mesh);
				Destroy(meshFilter);
			}

			meshRenderer = temp.GetChild(d).gameObject.transform.GetComponent<MeshRenderer>();
			if (meshRenderer)
			{
				//				Debug.Log("DestroyAfterTime: meshRenderer is \"" + meshRenderer.name + "\"");
				Destroy(meshRenderer.material);
				Destroy(meshRenderer);
			}
		}

		meshFilter = temp.GetComponent<MeshFilter>();
		if (meshFilter)
		{
			//			Debug.Log("DestroyAfterTime: meshFilter is \"" + meshFilter.name + "\"");
			Destroy(meshFilter.mesh);
			Destroy(meshFilter);
		}

		meshRenderer = temp.GetComponent<MeshRenderer>();
		if (meshRenderer)
		{
			//			Debug.Log("DestroyAfterTime: meshRenderer is \"" + meshRenderer.name + "\"");
			Destroy(meshRenderer.material);
			Destroy(meshRenderer);
		}
		Destroy(gO);
		return;
	}
}
