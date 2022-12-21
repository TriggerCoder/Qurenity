using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class SpriteFadeOut : MonoBehaviour
{
	public float _fadeTime = 1;
	public Color startColor;
	public Color fadeColor;
	Color currentColor;
	float time = 0f;

	MeshRenderer mr;
	Material mat = null;
	MaterialPropertyBlock materialProperties;
	void Start()
    {
		mr = GetComponent<MeshRenderer>();
		materialProperties = new MaterialPropertyBlock();

		mat = mr.material;
		startColor = mat.GetColor(MaterialManager.colorPropertyId);
		currentColor = startColor;
	}

	private void OnEnable()
	{
		time = 0f;
		if (mat != null)
		{
			mr.GetPropertyBlock(materialProperties);
			materialProperties.SetColor(MaterialManager.colorPropertyId, startColor);
			mr.SetPropertyBlock(materialProperties);
		}
	}
	// Update is called once per frame
	void Update()
    {
		if (GameManager.Paused)
			return;

		if (time >= _fadeTime)
			return;

		float t = Time.deltaTime;

		mr.GetPropertyBlock(materialProperties);
		currentColor = Color.Lerp(currentColor, fadeColor, t);
		materialProperties.SetColor(MaterialManager.colorPropertyId, currentColor);
		mr.SetPropertyBlock(materialProperties);
	}
}
