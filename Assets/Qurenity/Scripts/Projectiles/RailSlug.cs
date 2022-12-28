using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class RailSlug : MonoBehaviour
{
	public string textureName;
	public Color railRimColor;
	public Color railColor;
	public Material material;

	public float _lifeTime = 1;
	float time = 0f;
	float initAlpha;
	float alpha = 1;
	Material railmat;
	private void Awake()
	{
		if (!TextureLoader.HasTexture(textureName))
			TextureLoader.AddNewTexture(textureName, true);

		Texture tex = TextureLoader.Instance.ColorizeTexture(null, textureName, railColor);

		railmat = Instantiate(material);
		railmat.SetTexture(MaterialManager.opaqueTexPropertyId, tex);
		railmat.SetColor("_RimColor", railRimColor);
		railmat.SetColor("_Color", railColor);
		initAlpha = railmat.GetFloat("_Alpha");
		alpha = initAlpha;
		MeshRenderer mr = GetComponent<MeshRenderer>();
		mr.material = railmat;
	}

	void Update()
	{
		if (GameManager.Paused)
			return;

		time += Time.deltaTime;

		alpha = Mathf.Lerp(initAlpha, 0, time / _lifeTime);
		railmat.SetFloat("_Alpha", alpha);
		if (time >= _lifeTime)
		{
			//Reset Timer
			time = 0f;
			gameObject.SetActive(false);
		}
	}
}

