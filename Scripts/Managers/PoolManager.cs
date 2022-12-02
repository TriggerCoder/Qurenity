using System.Collections;
using System.Collections.Generic;
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
}
