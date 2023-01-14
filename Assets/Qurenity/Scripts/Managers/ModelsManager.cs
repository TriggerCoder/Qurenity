using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ModelsManager : MonoBehaviour
{
	public static ModelsManager Instance;

	public static Dictionary<string, MD3> Models = new Dictionary<string, MD3>();
	void Awake()
	{
		Instance = this;	
	}

	public static void CacheModel(string modelName, bool forceSkinAlpha = false)
	{
		if (Models.ContainsKey(modelName))
			return;

		MD3 model = MD3.ImportModel(modelName, forceSkinAlpha);
		if (model == null)
			return;

		Models.Add(modelName, model);
		return;
	}
	public static MD3 GetModel(string modelName, bool forceSkinAlpha = false)
	{
		if (Models.ContainsKey(modelName))
			return Models[modelName];

		MD3 model = MD3.ImportModel(modelName, forceSkinAlpha);
		if (model == null)
			return null;

		Models.Add(modelName, model);
		return model;
	}
}
