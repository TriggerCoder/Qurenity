using System.Collections;
using System.Collections.Generic;
using Assets.MultiAudioListener;
using UnityEngine;

public class PoolObject<T>
{
	public T data { get; set; }
	public GameObject go { get; set; }
	public PoolObject(T dataValue, GameObject obj)
	{
		data = dataValue;
		go = obj;
	}
}
public static class PoolManager
{
	private static Dictionary<string, Queue<object>> ObjectsPool = new Dictionary<string, Queue<object>>();

	public static bool HasObjectPool(string pool)
	{
		if (ObjectsPool.ContainsKey(pool.ToUpper()))
			return true;

		return false;
	}
	public static void CreateObjectPool(string pool, GameObject go, int quantity = 5)
	{
		if (ObjectsPool.ContainsKey(pool.ToUpper()))
			return;

		Queue<object> GameObjectPool = new Queue<object>();
		ObjectsPool.Add(pool.ToUpper(), GameObjectPool);

		for (int i = 0; i < quantity; ++i)
		{
			GameObject poolObject = GameObject.Instantiate(go);
			poolObject.transform.SetParent(GameManager.Instance.BaseThingsHolder);
			poolObject.SetActive(false);
			GameObjectPool.Enqueue(poolObject);
		}
		return;
	}

	public static GameObject GetObjectFromPool(string pool, bool setActive = true)
	{
		if (!ObjectsPool.ContainsKey(pool.ToUpper()))
			return null;

		GameObject go;
		Queue<object> ActivePool = ObjectsPool[pool.ToUpper()];

		do
		{
			go = (GameObject)ActivePool.Dequeue();
			ActivePool.Enqueue(go);
			if (go.activeSelf)
			{
				for (int i = 0; i < 10; ++i)
				{
					GameObject poolObject = GameObject.Instantiate(go);
					poolObject.transform.SetParent(GameManager.Instance.BaseThingsHolder);
					poolObject.SetActive(false);
					ActivePool.Enqueue(poolObject);
				}
			}
		} while (go.activeSelf);

		if (setActive)
			go.SetActive(true);
		return go;
	}
	public static void CreateProjectilePool(string pool, GameObject go, int quantity = 5)
	{
		if (ObjectsPool.ContainsKey(pool.ToUpper()))
			return;
		//Check if Projectile
		{
			Projectile proj = go.GetComponent<Projectile>();
			if (proj == null)
			{
				CreateObjectPool(pool, go, quantity);
				return;
			}
		}
		Queue<object> GameObjectPool = new Queue<object>();
		ObjectsPool.Add(pool.ToUpper(), GameObjectPool);

		for (int i = 0; i < quantity; ++i)
		{
			GameObject poolObject = GameObject.Instantiate(go);
			Projectile proj = poolObject.GetComponent<Projectile>();
			PoolObject<Projectile> poolObjectProj = new PoolObject<Projectile>(proj, poolObject);
			poolObject.transform.SetParent(GameManager.Instance.BaseThingsHolder);
			poolObject.SetActive(false);
			GameObjectPool.Enqueue(poolObjectProj);
		}
		return;
	}
	public static PoolObject<Projectile> GetProjectileFromPool(string pool)
	{
		if (!ObjectsPool.ContainsKey(pool.ToUpper()))
			return null;

		PoolObject<Projectile> poolObjectProj;
		Queue<object> ActivePool = ObjectsPool[pool.ToUpper()];

		do
		{
			poolObjectProj = (PoolObject<Projectile>)ActivePool.Dequeue();
			ActivePool.Enqueue(poolObjectProj);
			if (poolObjectProj.go.activeSelf)
			{
				for (int i = 0; i < 10; ++i)
				{
					GameObject poolObject = GameObject.Instantiate(poolObjectProj.go);
					Projectile proj = poolObject.GetComponent<Projectile>();
					poolObjectProj = new PoolObject<Projectile>(proj, poolObject);
					poolObject.transform.SetParent(GameManager.Instance.BaseThingsHolder);
					poolObject.SetActive(false);
					ActivePool.Enqueue(poolObjectProj);
				}
			}
		} while (poolObjectProj.go.activeSelf);

		poolObjectProj.go.SetActive(true);
		return poolObjectProj;
	}
	public static void Create3DSoundPool(string pool, int quantity = 5)
	{
		if (ObjectsPool.ContainsKey(pool.ToUpper()))
			return;

		Queue<object> GameObjectPool = new Queue<object>();
		ObjectsPool.Add(pool.ToUpper(), GameObjectPool);

		for (int i = 0; i < quantity; ++i)
		{
			GameObject sound = new GameObject("3Dsound");
			MultiAudioSource audioSource = sound.AddComponent<MultiAudioSource>();
			audioSource.Loop = false;
			DisableAfterSoundPlayed disableAfterSoundPlayed = sound.AddComponent<DisableAfterSoundPlayed>();
			PoolObject<MultiAudioSource> poolObjectAudio = new PoolObject<MultiAudioSource>(audioSource, sound);
			sound.transform.SetParent(GameManager.Instance.BaseThingsHolder);
			sound.SetActive(false);
			GameObjectPool.Enqueue(poolObjectAudio);
		}
		return;
	}
	public static PoolObject<MultiAudioSource> Get3DSoundFromPool(string pool)
	{
		if (!ObjectsPool.ContainsKey(pool.ToUpper()))
			return null;

		PoolObject<MultiAudioSource> poolObjectAudio;
		Queue<object> ActivePool = ObjectsPool[pool.ToUpper()];

		do
		{
			poolObjectAudio = (PoolObject<MultiAudioSource>)ActivePool.Dequeue();
			ActivePool.Enqueue(poolObjectAudio);
			if (poolObjectAudio.go.activeSelf)
			{
				for (int i = 0; i < 10; ++i)
				{
					GameObject sound = new GameObject("3Dsound");
					MultiAudioSource audioSource = sound.AddComponent<MultiAudioSource>();
					audioSource.Loop = false;
					DisableAfterSoundPlayed disableAfterSoundPlayed = sound.AddComponent<DisableAfterSoundPlayed>();
					poolObjectAudio = new PoolObject<MultiAudioSource>(audioSource, sound);
					sound.transform.SetParent(GameManager.Instance.BaseThingsHolder);
					sound.SetActive(false);
					ActivePool.Enqueue(poolObjectAudio);
				}
			}
		} while (poolObjectAudio.go.activeSelf);

		poolObjectAudio.go.SetActive(true);
		return poolObjectAudio;
	}
}
