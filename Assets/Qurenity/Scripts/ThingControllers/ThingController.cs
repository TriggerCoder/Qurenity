using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThingController : MonoBehaviour
{
	[HideInInspector]
	public Vector3 location;
	[HideInInspector]
	public Quaternion angularrotation;

	public string thingName;
	public string respawnSound = "items/respawn1";
	public float respawnTime;

	public ModelCache[] modelsToCache = new ModelCache[0];
	public PoolObjectCache[] poolObjectsToCache = new PoolObjectCache[0];

	[System.Serializable]
	public struct ModelCache
	{
		public string modelName;
		public bool isTextureTransparent;
	}

	[System.Serializable]
	public struct PoolObjectCache
	{
		public GameObject go;
		public string poolName;
	}

	[System.Serializable]
	public enum ThingType
	{
		Decor, //non-blocking, non-interactive
		Blocking, //blocking or interactive
		Item,
		Teleport,
		JumpPad,
		TargetDestination,
		Trigger,
		Door,
		Player
	}

	public ThingType thingType = ThingType.Decor;

	void Awake()
	{
		foreach (var model in modelsToCache)
			ModelsManager.CacheModel(model.modelName,model.isTextureTransparent);

		foreach (var poolObject in poolObjectsToCache)
		{
			if (!PoolManager.HasObjectPool(poolObject.poolName))
				PoolManager.CreateProjectilePool(poolObject.poolName, poolObject.go, 10);
		}
	}

	void OnDisable()
	{
		if (GameManager.Instance.ready)
			if (respawnTime > 0)
				ThingsManager.AddItemToRespawn(gameObject, respawnSound, respawnTime);
	}
}
