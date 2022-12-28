using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThingsManager : MonoBehaviour
{
	public ThingsManager Instance;
	public List<ThingController> _ThingPrefabs = new List<ThingController>();
	public static Dictionary<string, GameObject> thingsPrefabs = new Dictionary<string, GameObject>();
	public static List<Entity> entitiesOnMap = new List<Entity>();
	public static DoubleLinkedList<RespawnItem> itemToRespawn = new DoubleLinkedList<RespawnItem>();
	public class Entity
	{
		public string name;
		public Vector3 origin;
		public Entity(string name, Vector3 origin)
		{
			this.name = name;
			this.origin = origin;
		}
	}

	public class RespawnItem
	{
		public GameObject go;
		public string respawnSound;
		public float time;

		public RespawnItem(GameObject go, string respawnSound, float time)
		{
			this.go = go;
			this.respawnSound = respawnSound;
			this.time = time;
		}
	}
	void Awake()
	{
		Instance = this;

		foreach (ThingController tc in _ThingPrefabs)
			thingsPrefabs.Add(tc.thingName, tc.gameObject);
	}
	public static void ReadEntities(byte[] entities)
	{
		MemoryStream ms = new MemoryStream(entities);
		StreamReader stream = new StreamReader(ms);
		string strWord;

		stream.BaseStream.Seek(0, SeekOrigin.Begin);

		while (!stream.EndOfStream)
		{
			strWord = stream.ReadLine();

			if (strWord.Length == 0)
				continue;

			if (strWord[0] != '{')
			{
				continue;
			}

			{
				strWord = stream.ReadLine();
				Dictionary<string, string> entityData = new Dictionary<string, string>();
				while (strWord[0] != '}')
				{

					string[] keyValue = strWord.Split('"');
					entityData.Add(keyValue[1].Trim('"'), keyValue[3].Trim('"'));
					strWord = stream.ReadLine();
				}
				if (!entityData.ContainsKey("classname"))
					continue;

				if (!thingsPrefabs.ContainsKey(entityData["classname"]))
				{
					Debug.LogWarning(entityData["classname"] + " not found");
					continue;
				}

				if (!entityData.ContainsKey("origin"))
				{
					Debug.LogWarning(entityData["classname"] + " has no origin");
					continue;
				}

				strWord = entityData["origin"];
				string[] values = new string[3] { "", "", "",};
				bool lastDigit = true;
				for (int i = 0, j = 0; i < strWord.Length; i++)
				{
					if ((char.IsDigit(strWord[i])) || (strWord[i] == '-'))
					{
						if (lastDigit)
							values[j] += strWord[i];
						else
						{
							j++;
							values[j] += strWord[i];
							lastDigit = true;
						}
					}
					else
						lastDigit = false;
					if ((j == 2) && (!lastDigit))
						break;
				}
				int x = int.Parse(values[0]);
				int y = int.Parse(values[1]);
				int z = int.Parse(values[2]);
				Vector3 origin = new Vector3(-x, z, -y);
				origin.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
				entitiesOnMap.Add(new Entity(entityData["classname"], origin));
			}
		}

		stream.Close();
		return;
	}

	public static void AddEntitiesToMap()
	{
		foreach(Entity entity in entitiesOnMap)
		{
			GameObject thingObject = Instantiate(thingsPrefabs[entity.name]);
			thingObject.transform.position = entity.origin;
			thingObject.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
			thingObject.SetActive(true);
		}
	}

	public static void AddItemToRespawn(GameObject go, string respawnSournd, float time)
	{
		itemToRespawn.InsertFront(new RespawnItem(go, respawnSournd, Time.time + time));
	}

	void Update()
	{
		if (GameManager.Paused)
			return;

		DoubleLinkedList<RespawnItem>.ListNode ri = itemToRespawn.Head;
		while (ri != null)
		{
			if (ri.Data != null)
			{
				if (Time.time > ri.Data.time)
				{
					ri.Data.go.SetActive(true);
					if (!string.IsNullOrEmpty(ri.Data.respawnSound))
						AudioManager.Create3DSound(ri.Data.go.transform.position, ri.Data.respawnSound, 1f);
					DoubleLinkedList<RespawnItem>.ListNode del = ri;
					ri = ri.Next;
					itemToRespawn.DestroyContainingNode(del.Data);
					continue;
				}
			}
			ri = ri.Next;
		}
	}
}