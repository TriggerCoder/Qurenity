using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialManager : MonoBehaviour
{
	public static MaterialManager Instance;

	public Material illegal;
	public Material defaultMaterial;
	public Material defaultMaterialLightMap;

	public bool applyLightmaps = true;
	public static int lightMapPropertyId;
	public string lightMapProperty = "_LightMap";
	public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
	void Awake()
	{
		Instance = this;
		lightMapPropertyId = Shader.PropertyToID(lightMapProperty);
	}

	public static Material GetMaterials(string textureName, int lm_index)
	{
		// Load the primary texture for the face from the texture lump
		// The texture lump itself will have already looked over all
		// available .pk3 files and compiled a dictionary of textures for us.
		Texture tex = TextureLoader.Instance.GetTexture(textureName);

		Material mat;
		// Lightmapping is on, so calc the lightmaps
		if (lm_index >= 0 && Instance.applyLightmaps)
		{
			if (Materials.ContainsKey(textureName + lm_index.ToString()))
				return Materials[textureName + lm_index.ToString()];

			// LM experiment
			Texture2D lmap = MapLoader.lightMaps[lm_index];
			lmap.Compress(true);
			lmap.Apply();
			mat = Instantiate(Instance.defaultMaterialLightMap);
			mat.mainTexture = tex;
			mat.SetTexture(lightMapPropertyId, lmap);
			Materials.Add(textureName + lm_index.ToString(), mat);
			return mat;
		}

		if (Materials.ContainsKey(textureName))
			return Materials[textureName];
		// Lightmapping is off, so don't.
		mat = Instantiate(Instance.defaultMaterial);
		mat.mainTexture = tex;
		Materials.Add(textureName, mat);
		return mat;
	}
}
