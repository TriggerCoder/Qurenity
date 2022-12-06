using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialManager : MonoBehaviour
{
	public static MaterialManager Instance;

	public Material illegal;
	public Material skyHole;
	public Material defaultMaterial;
	public Material defaultMaterialLightMap;

	public MaterialOverride[] _OverrideMaterials = new MaterialOverride[0];

	public bool applyLightmaps = true;
	public static int lightMapPropertyId;
	public string lightMapProperty = "_LightMap";
	public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
	public static Dictionary<string, MaterialOverride> OverrideMaterials = new Dictionary<string, MaterialOverride>();
	public static Dictionary<string, QShader> AnimatedTextures = new Dictionary<string, QShader>();

	public static readonly string[] rgbGenTextures = { "_S_Texture", "_W_Texture", "_IW_Texture" };
	public static readonly string[] rgbGenBase = { "_S_Base", "_W_Base", "_IW_Base" };
	public static readonly string[] rgbGenAmp = { "_S_Amp", "_W_Amp", "_IW_Amp" };
	public static readonly string[] rgbGenPhase = { "_S_Phase", "_W_Phase", "_IW_Phase" };
	public static readonly string[] rgbGenFreq = { "_S_Freq", "_W_Freq", "_IW_Freq" };
	void Awake()
	{
		Instance = this;
		lightMapPropertyId = Shader.PropertyToID(lightMapProperty);
		foreach (MaterialOverride mo in _OverrideMaterials)
		{
			OverrideMaterials.Add(mo.overrideName, mo);
			foreach(MaterialAnimation ma in mo.animation)
			{
				foreach (string textureName in ma.textureFrames)
				{
					QShader shader = new QShader(textureName, 0, 0);
					if (!AnimatedTextures.ContainsKey(textureName))
						AnimatedTextures.Add(textureName,shader);
				}
			}
		}
	}

	public static void GetShaderAnimationsTextures()
	{
		List<QShader> list = new List<QShader>(AnimatedTextures.Count);
		foreach (var shader in AnimatedTextures)
			list.Add(shader.Value);

		TextureLoader.LoadJPGTextures(list,true);
		TextureLoader.LoadTGATextures(list, true);
	}

	public static bool GetOverrideMaterials(string textureName, ref Material mat, ref GameObject go)
	{
		if (!OverrideMaterials.ContainsKey(textureName))
			return false;

		MaterialOverride mo = OverrideMaterials[textureName];
		if (mo.material != null)
			mat = Instantiate(mo.material);

		MeshRenderer mr = go.GetComponent<MeshRenderer>();
		MaterialPropertyBlock materialParameters = new MaterialPropertyBlock();

		mr.GetPropertyBlock(materialParameters);
		for (int i = 0; i < mo.animation.Length; i++)
		{
			TextureAnimation anim = go.AddComponent<TextureAnimation>();
			anim.frames = mo.animation[i].textureFrames;
			anim.frameTime = mo.animation[i].fps;
			anim.textureType = i;
			materialParameters.SetFloat(rgbGenBase[i], mo.animation[i].Base);
			materialParameters.SetFloat(rgbGenAmp[i], mo.animation[i].Amp);
			materialParameters.SetFloat(rgbGenPhase[i], mo.animation[i].Phase);
			materialParameters.SetFloat(rgbGenFreq[i], mo.animation[i].Freq);
		}
		mr.SetPropertyBlock(materialParameters);

		return true;
	}

	public static Material GetMaterials(string textureName, int lm_index)
	{
		if (MapLoader.IsSkyTexture(textureName))
			return Instance.skyHole;

		// Load the primary texture for the surface from the texture lump
		// The texture lump itself will have already looked over all
		// available .pk3 files and compiled a dictionary of textures for us.
		Texture tex = TextureLoader.Instance.GetTexture(textureName);

		Material mat;
		// Lightmapping is on, so calc the lightmaps
		if (lm_index >= 0 && Instance.applyLightmaps)
		{
			if (Materials.ContainsKey(textureName + lm_index.ToString()))
				return Materials[textureName + lm_index.ToString()];

			// Lightmapping
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

	[System.Serializable]
	public struct MaterialOverride
	{
		public string overrideName;
		public Material material;
		public MaterialAnimation[] animation;
	}

	[System.Serializable]
	public struct MaterialAnimation
	{
		public string[] textureFrames;
		public int fps;
		public float Base;
		public float Amp;
		public float Phase;
		public float Freq;
	}
}
