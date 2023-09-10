using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialManager : MonoBehaviour
{
	public static MaterialManager Instance;

	public Material illegal;
	public Material skyHole;
	public Material opaqueMaterial;
	public Material defaultMaterial;
	public Material billBoardMaterial;
	public Material spriteMaterial;
	public Material defaultTransparentMaterial;
	public Material defaultLightMapMaterial;
	public Material defaultTransparentLightMapMaterial;
	public Material debug;
	public Material rgbGenIdentity;
	public Material tcGenEnvironment;
	public Material tcModRotate;
	public Material tcModScroll;

	public MaterialOverride[] _OverrideMaterials = new MaterialOverride[0];

	public bool applyLightmaps = true;

	public string lightMapProperty = "_LightMap";
	public string opaqueTexProperty = "_MainTex";
	public string colorProperty = "_Color";

	[HideInInspector]
	public static int lightMapPropertyId;
	[HideInInspector]
	public static int opaqueTexPropertyId;
	[HideInInspector]
	public static int colorPropertyId;

	public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();
	public static Dictionary<string, MaterialOverride> OverrideMaterials = new Dictionary<string, MaterialOverride>();
	public static Dictionary<string, QShader> AditionalTextures = new Dictionary<string, QShader>();

	public static readonly string[] rgbGenTextures = { "_S_Texture", "_W_Texture", "_IW_Texture" };
	public static readonly string[] rgbGenBase = { "_S_Base", "_W_Base", "_IW_Base" };
	public static readonly string[] rgbGenAmp = { "_S_Amp", "_W_Amp", "_IW_Amp" };
	public static readonly string[] rgbGenPhase = { "_S_Phase", "_W_Phase", "_IW_Phase" };
	public static readonly string[] rgbGenFreq = { "_S_Freq", "_W_Freq", "_IW_Freq" };
	void Awake()
	{
		Instance = this;
		lightMapPropertyId = Shader.PropertyToID(lightMapProperty);
		opaqueTexPropertyId = Shader.PropertyToID(opaqueTexProperty);
		colorPropertyId = Shader.PropertyToID(colorProperty);

		foreach (MaterialOverride mo in _OverrideMaterials)
		{
			OverrideMaterials.Add(mo.overrideName, mo);
			if (!string.IsNullOrEmpty(mo.opaqueTextureName))
			{
				if (mo.opaque)
					AddAditionalTextures(mo.opaqueTextureName);
				else
					AddAditionalTextures(mo.opaqueTextureName, true);
			}
			foreach (MaterialAnimation ma in mo.animation)
			{
				foreach (string textureName in ma.textureFrames)
					AddAditionalTextures(textureName, ma.addAlpha);
			}
		}
	}

	void AddAditionalTextures(string textureName, bool addAlpha = false)
	{
		QShader shader = new QShader(textureName, 0, 0, addAlpha);
		if (!AditionalTextures.ContainsKey(textureName))
			AditionalTextures.Add(textureName, shader);
	}
	public static void GetShaderAnimationsTextures()
	{
		List<QShader> list = new List<QShader>(AditionalTextures.Count);
		foreach (var shader in AditionalTextures)
			list.Add(shader.Value);

		TextureLoader.LoadJPGTextures(list);
		TextureLoader.LoadTGATextures(list);
	}

	public static bool GetOverrideMaterials(string textureName, int lm_index, ref Material mat, ref GameObject go)
	{
		if (!OverrideMaterials.ContainsKey(textureName))
			return false;


		if (textureName == "models/weapons2/plasma/plasma_glo")
		{
			Texture tex = TextureLoader.Instance.GetTexture("models/weapons2/plasma/plasma_glo");
			mat = Instantiate(Instance.rgbGenIdentity);
			ShaderStack shaderStack = go.AddComponent<ShaderStack>();

			Material rotate = Instantiate(Instance.tcModRotate);
			Material scroll = Instantiate(Instance.tcModScroll);
			float[] rotateparams = new float[1] { 33f };
			float[] scrolleparams = new float[2] { .7f, .1f };

			shaderStack.AddStackMaterial(tex, scroll, scrolleparams);
			shaderStack.AddStackMaterial(null, rotate, rotateparams);
			shaderStack.SetLastMaterial(mat);
/*
			mat = Instantiate(Instance.rgbGenIdentity);
			mat.mainTexture = tex;
*/
			return true;
		}

		MaterialOverride mo = OverrideMaterials[textureName];
		if (mo.material != null)
			mat = Instantiate(mo.material);

		if (!string.IsNullOrEmpty(mo.opaqueTextureName))
		{
			// Load the opaque texture for the surface
			Texture tex = TextureLoader.Instance.GetTexture(mo.opaqueTextureName);
			mat.SetTexture(opaqueTexPropertyId, tex);

			if (lm_index >= 0 && Instance.applyLightmaps)
			{
				// Lightmapping
				Texture2D lmap = MapLoader.lightMaps[lm_index];
				lmap.Compress(true);
				lmap.Apply();
				mat.SetTexture(lightMapPropertyId, lmap);
			}
		}

		for (int i = 0; i < mo.animation.Length; i++)
		{
			if (mo.animation[i].textureFrames.Length != 0)
			{
				TextureAnimation anim = go.AddComponent<TextureAnimation>();
				anim.frames = mo.animation[i].textureFrames;
				anim.frameTime = mo.animation[i].fps;
				anim.textureType = i;
				anim.Init();
			}
			mat.SetFloat(rgbGenBase[i], mo.animation[i].Base);
			mat.SetFloat(rgbGenAmp[i], mo.animation[i].Amp);
			mat.SetFloat(rgbGenPhase[i], mo.animation[i].Phase);
			mat.SetFloat(rgbGenFreq[i], mo.animation[i].Freq);
		}
		return true;
	}
	public static Material GetMaterials(string textureName, int lm_index, bool forceSkinAlpha = false)
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
			if (forceSkinAlpha)
				mat = Instantiate(Instance.defaultTransparentLightMapMaterial);
			else
				mat = Instantiate(Instance.defaultLightMapMaterial);
			mat.mainTexture = tex;
			mat.SetTexture(lightMapPropertyId, lmap);
			Materials.Add(textureName + lm_index.ToString(), mat);
			return mat;
		}

		if (Materials.ContainsKey(textureName))
			return Materials[textureName];
		// Lightmapping is off, so don't.
		if (forceSkinAlpha)
			mat = Instantiate(Instance.defaultTransparentMaterial);
		else
			mat = Instantiate(Instance.defaultMaterial);
		mat.mainTexture = tex;
		Materials.Add(textureName, mat);
		return mat;
	}

	[System.Serializable]
	public struct MaterialOverride
	{
		public string overrideName;
		public bool opaque;
		public string opaqueTextureName;
		public Material material;
		public MaterialAnimation[] animation;
	}

	[System.Serializable]
	public struct MaterialAnimation
	{
		public string[] textureFrames;
		public bool addAlpha;
		public int fps;
		public float Base;
		public float Amp;
		public float Phase;
		public float Freq;
	}
}
