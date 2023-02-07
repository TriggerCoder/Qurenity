using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShaderStack : MonoBehaviour
{
	public List<CustomRenderTexture> stackTextures = new List<CustomRenderTexture>();
	public void AddStackMaterial(Texture texture, Material material, params float[] floatParams)
	{
		if (texture == null)
		{
			if (stackTextures.Count == 0)
				return;
			texture = stackTextures[stackTextures.Count - 1];
		}
		CustomRenderTexture custom = new CustomRenderTexture(texture.width, texture.height, RenderTextureFormat.ARGB32);
		for (int i = 0; i < floatParams.Length; i++)
		{
			float param = floatParams[i];
			material.SetFloat("_Param" + i, param);
		}
		custom.wrapMode = TextureWrapMode.Repeat;
		custom.updateMode = CustomRenderTextureUpdateMode.Realtime;
		custom.updatePeriod = 0;
		custom.material = material;
		custom.material.mainTexture = texture;
		custom.initializationSource = CustomRenderTextureInitializationSource.TextureAndColor;
		custom.initializationTexture = texture;
		custom.initializationColor = Color.white;
		custom.doubleBuffered = true;
		stackTextures.Add(custom);
	}

	public void SetLastMaterial(Material material)
	{
		if (stackTextures.Count == 0)
			return;
		material.mainTexture = stackTextures[stackTextures.Count - 1];
	}

	void Update()
    {
        for (int i = 0; i < stackTextures.Count; i++)
		{
			stackTextures[i].Initialize();
			stackTextures[i].Update();
		}
	}
}
