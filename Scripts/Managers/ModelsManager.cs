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

	public static MD3 GetModel(string modelName)
	{
		if (Models.ContainsKey(modelName))
			return Models[modelName];

		MD3 model = MD3.ImportModel(modelName);
		if (model == null)
			return null;

		Models.Add(modelName, model);
		return model;
	}
}
