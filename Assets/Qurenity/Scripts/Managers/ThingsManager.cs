using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.MultiAudioListener;
using UnityEngine;

public class ThingsManager : MonoBehaviour
{
	public ThingsManager Instance;
	public List<ThingController> _ThingPrefabs = new List<ThingController>();
	public static Dictionary<string, GameObject> thingsPrefabs = new Dictionary<string, GameObject>();
	public static List<Entity> entitiesOnMap = new List<Entity>();
	public static Dictionary<int, Vector3> targetsOnMap = new Dictionary<int, Vector3>();
	public static Dictionary<int, TriggerController> triggerToActivate = new Dictionary<int, TriggerController>();
	public static Dictionary<int, Dictionary<string, string>> timersOnMap = new Dictionary<int, Dictionary<string, string>>();
	public static Dictionary<int, Dictionary<string, string>> triggersOnMap = new Dictionary<int, Dictionary<string, string>>();
	public static DoubleLinkedList<RespawnItem> itemToRespawn = new DoubleLinkedList<RespawnItem>();

	public static readonly string[] ignoreThings = { "misc_model", "light", "func_group" };

	public static readonly string[] targetThings = { "func_timer", "trigger_multiple", "target_position", "info_notnull", "misc_teleporter_dest" };

	public class Entity
	{
		public string name;
		public Vector3 origin;
		public Dictionary<string, string> entityData;
		public Entity(string name, Vector3 origin, Dictionary<string, string> entityData)
		{
			this.name = name;
			this.origin = origin;
			this.entityData = entityData;
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

				if (ignoreThings.Any(s => s == entityData["classname"]))
					continue;

				if (!thingsPrefabs.ContainsKey(entityData["classname"]))
				{
					Debug.LogWarning(entityData["classname"] + " not found");
					continue;
				}

				Vector3 origin = Vector3.zero;
				if (entityData.ContainsKey("origin"))
				{
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
					origin = new Vector3(-x, z, -y);
					origin.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
				}

				if (targetThings.Any(s => s == entityData["classname"]))
				{
					if (!entityData.TryGetValue("target", out strWord))
						strWord = entityData["targetname"];
					int target = int.Parse(strWord.Trim('t'));

					switch(entityData["classname"])
					{
						default:
							targetsOnMap.Add(target, origin);
						break;
						case "func_timer": //Timers
							timersOnMap.Add(target, entityData);
						break;
						case "trigger_multiple": //Triggers
							triggersOnMap.Add(target, entityData);
						break;
					}
				}
				else
					entitiesOnMap.Add(new Entity(entityData["classname"], origin, entityData));
			}
		}

		stream.Close();
		return;
	}

	public static void AddThingsToMap()
	{
		AddTriggersOnMap();
		AddEntitiesToMap();
		AddTimersToMap();
	}
	public static void AddTriggersOnMap()
	{
		foreach (KeyValuePair<int, Dictionary<string, string>> trigger in triggersOnMap)
		{
			int target = trigger.Key;
			Dictionary<string, string> entityData = trigger.Value;
			GameObject thingObject = Instantiate(thingsPrefabs[entityData["classname"]]);
			if (thingObject == null)
				continue;
			thingObject.name = "Trigger " + target;

			TriggerController tc = thingObject.GetComponent<TriggerController>();
			if (tc == null)
				continue;

			string strWord = entityData["model"];
			int model = int.Parse(strWord.Trim('*'));

			if (entityData.TryGetValue("wait", out strWord))
			{
				tc.AutoReturn = true;
				tc.AutoReturnTime = int.Parse(strWord);
			}

			MapLoader.GenerateGeometricCollider(thingObject, model, ContentFlags.Trigger);
			triggerToActivate.Add(target, tc);
			thingObject.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
			thingObject.SetActive(true);
		}
	}

	public static void AddTimersToMap()
	{
		foreach (KeyValuePair<int, Dictionary<string, string>> timer in timersOnMap)
		{
			int target = timer.Key;
			TriggerController tc;
			if (!triggerToActivate.TryGetValue(target, out tc))
				continue;

			Dictionary<string, string> entityData = timer.Value;
			GameObject go = new GameObject("Timer "+ target);
			string strWord = entityData["random"];
			int random = int.Parse(strWord);
			strWord = entityData["wait"];
			int wait = int.Parse(strWord);
			TimerController timerController = go.AddComponent<TimerController>();
			timerController.Init(wait, random, tc);
			go.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
			go.SetActive(true);
		}
	}
	public static void AddEntitiesToMap()
	{
		foreach(Entity entity in entitiesOnMap)
		{
			GameObject thingObject = Instantiate(thingsPrefabs[entity.name]);
			if (thingObject == null)
				continue;
			
			switch(entity.name)
			{
				default:
					thingObject.transform.position = entity.origin;
				break;
				//Switch
				case "func_button":
				{
					string strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					int angle = 0, hitpoints = 0, speed = 40, wait = 1, lip = 4;
					SwitchController sw = thingObject.GetComponent<SwitchController>();
					if (sw == null)
						sw = thingObject.AddComponent<SwitchController>();
					if (entity.entityData.TryGetValue("angle", out strWord))
						angle = int.Parse(strWord);
					if (entity.entityData.TryGetValue("health", out strWord))
						hitpoints = int.Parse(strWord);
					if (entity.entityData.TryGetValue("speed", out strWord))
						speed = int.Parse(strWord);
					if (entity.entityData.TryGetValue("wait", out strWord))
						wait = int.Parse(strWord);
					if (entity.entityData.TryGetValue("lip", out strWord))
						lip = int.Parse(strWord);

					MapLoader.GenerateGeometricSurface(thingObject, model);
					MapLoader.GenerateGeometricCollider(thingObject.transform, model);

					MeshFilter[] meshFilterChildren = thingObject.GetComponentsInChildren<MeshFilter>(includeInactive: true);
					CombineInstance[] combine = new CombineInstance[meshFilterChildren.Length];
					for (int i = 0; i < combine.Length; i++)
						combine[i].mesh = meshFilterChildren[i].mesh;

					var mesh = new Mesh();
					mesh.CombineMeshes(combine, true, false, false);
					Bounds bounds = mesh.bounds;
					sw.Init(angle, hitpoints, speed, wait, lip, bounds);
						
					//If it's not damagable, then create trigger collider
					if (hitpoints == 0)
					{
						float max = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
						SphereCollider sc = thingObject.AddComponent<SphereCollider>();
						sc.radius = max;
						sc.isTrigger = true;
					}
					//If it is, then we need to create a damage interface for the colliders
					else
					{
						Collider[] collidersChildren = thingObject.GetComponentsInChildren<Collider>(includeInactive: true);
						for (var i = 0; i < collidersChildren.Length; i++)
						{
							ParentIsDamageable parentIsDamageable = collidersChildren[i].gameObject.AddComponent<ParentIsDamageable>();
							parentIsDamageable.parent = sw;
						}
					}

					if (entity.entityData.ContainsKey("target"))
					{
						strWord = entity.entityData["target"];
						int target = int.Parse(strWord.Trim('t'));

						TriggerController tc;
						if (!triggerToActivate.TryGetValue(target, out tc))
							tc = null;
						sw.tc = tc;

						sw.internalSwitch.SetController(target, (p) =>
						{
							if (sw.tc == null)
							{
								TriggerController swTrigger;
								if (!triggerToActivate.TryGetValue(sw.internalSwitch.triggerNum, out swTrigger))
									return;
								sw.tc = swTrigger;
							}
							sw.CurrentState = DoorController.State.Opening;
							sw.tc.Activate(null);
						});
					}
				}
				break;
				//Door
				case "func_door":
				{
					string strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					int angle = 0, hitpoints = 0, speed = 200, wait = 2, lip = 8, dmg = 4;
					DoorController door = thingObject.GetComponent<DoorController>();
					if (door == null)
						door = thingObject.AddComponent<DoorController>();
					if (entity.entityData.TryGetValue("angle", out strWord))
						angle = int.Parse(strWord);
					if (entity.entityData.TryGetValue("health", out strWord))
						hitpoints = int.Parse(strWord);
					if (entity.entityData.TryGetValue("speed", out strWord))
						speed = int.Parse(strWord);
					if (entity.entityData.TryGetValue("wait", out strWord))
						wait = int.Parse(strWord);
					if (entity.entityData.TryGetValue("lip", out strWord))
						lip = int.Parse(strWord);
					if (entity.entityData.TryGetValue("dmg", out strWord))
						dmg = int.Parse(strWord);

					MapLoader.GenerateGeometricSurface(thingObject, model);
					MapLoader.GenerateGeometricCollider(thingObject.transform, model);

					MeshFilter[] meshFilterChildren = thingObject.GetComponentsInChildren<MeshFilter>(includeInactive: true);
					CombineInstance[] combine = new CombineInstance[meshFilterChildren.Length];
					for (var i = 0; i < combine.Length; i++)
						combine[i].mesh = meshFilterChildren[i].mesh;

					var mesh = new Mesh();
					mesh.CombineMeshes(combine, true, false, false);
					Bounds bounds = mesh.bounds;
					door.Init(angle, hitpoints, speed, wait, lip, bounds, dmg);

					//Need to change the rb to non kinematics in order for collision detection to work
					Rigidbody[] rigidbodiesChildren = thingObject.GetComponentsInChildren<Rigidbody>(includeInactive: true);
					for (int i = 0; i < rigidbodiesChildren.Length; i++)
					{
						rigidbodiesChildren[i].useGravity = false;
						rigidbodiesChildren[i].isKinematic = false;
						rigidbodiesChildren[i].constraints = RigidbodyConstraints.FreezeAll;
						DoorCollider doorCollider = rigidbodiesChildren[i].gameObject.AddComponent<DoorCollider>();
						doorCollider.door = door;
					}

					if (entity.entityData.ContainsKey("targetname"))
					{
						strWord = entity.entityData["targetname"];
						int target = int.Parse(strWord.Trim('t'));

						TriggerController tc;
						if (!triggerToActivate.TryGetValue(target, out tc))
						{
							tc = thingObject.AddComponent<TriggerController>();
							triggerToActivate.Add(target, tc);
						}
						tc.Repeatable = true;
						tc.AutoReturn = true;
						tc.AutoReturnTime = wait;
						tc.SetController(target, (p) =>
						{
							door.CurrentState = DoorController.State.Opening;
						});
					}
					else //If it's not external trigger
					{
						if (hitpoints == 0)//If  not damagable, then create a trigger and collider
						{
							float max = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
							SphereCollider sc = thingObject.AddComponent<SphereCollider>();
							sc.radius = max;
							sc.isTrigger = true;
							
							TriggerController tc = thingObject.AddComponent<TriggerController>();
							tc.Repeatable = true;
							tc.AutoReturn = true;
							tc.AutoReturnTime = wait;
							tc.SetController(0, (p) =>
							{
								door.CurrentState = DoorController.State.Opening;
							});
						}
						else //If it is, then we need to create a damage interface for the colliders
						{
							for (int i = 0; i < rigidbodiesChildren.Length; i++)
							{
								ParentIsDamageable parentIsDamageable = rigidbodiesChildren[i].gameObject.AddComponent<ParentIsDamageable>();
								parentIsDamageable.parent = door;
							}
						}
					}
				}
				break;
				//Trigger Hurt
				case "trigger_hurt":
				{
					string strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					strWord = entity.entityData["dmg"];
					int dmg = int.Parse(strWord);
					MapLoader.GenerateGeometricCollider(thingObject, model, ContentFlags.Trigger);
					TriggerController tc = thingObject.GetComponent<TriggerController>();
					if (tc == null)
						tc = thingObject.AddComponent<TriggerController>();
					tc.Repeatable = true;
					tc.SetController(0, (p) =>
					{
						p.Damage(dmg,DamageType.Generic);
					});
				}
				break;
				//Remove PowerUps
				case "target_remove_powerups":
				{
					if (entity.entityData.ContainsKey("targetname"))
					{
						string strWord = entity.entityData["targetname"];
						int target = int.Parse(strWord.Trim('t'));

						TriggerController tc;
						if (!triggerToActivate.TryGetValue(target, out tc))
						{
							tc = thingObject.AddComponent<TriggerController>();
							triggerToActivate.Add(target, tc);
						}
						else
							Destroy(thingObject);
						tc.Repeatable = true;
						tc.SetController(target, (p) =>
						{
							p.RemovePowerUps();
						});
					}
				}
				break;
				//JumpPad
				case "trigger_push":
				{
					JumpPadThing thing = thingObject.GetComponent<JumpPadThing>();
					if (thing == null)
						continue;

					string strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					strWord = entity.entityData["target"];
					int target = int.Parse(strWord.Trim('t'));
					Vector3 destination = targetsOnMap[target];
					MapLoader.GenerateJumpPadCollider(thingObject, model);
					thing.Init(destination);
				}
				break;
				//Teleporter
				case "trigger_teleport":
				{
					TeleporterThing thing = thingObject.GetComponent<TeleporterThing>();
					if (thing == null)
						continue;

					string strWord = entity.entityData["model"];
					int model = int.Parse(strWord.Trim('*'));
					strWord = entity.entityData["target"];
					int target = int.Parse(strWord.Trim('t'));
					Vector3 destination = targetsOnMap[target];
					MapLoader.GenerateGeometricCollider(thingObject, model, ContentFlags.Teleporter);
					thing.Init(destination);
				}
				break;
				//Speaker
				case "target_speaker":
				{
					bool isAudio3d = true;
					string strWord = entity.entityData["noise"];
					string[] keyValue = strWord.Split('.');
					strWord = keyValue[0];
					if (!strWord.Contains('*'))
					{
						keyValue = strWord.Split('/');
						strWord = "";
						for (int i = 1; i < keyValue.Length; i++)
						{
							if (i > 1)
								strWord += "/";
							strWord += keyValue[i];
						}
					}
					AudioSource audioSource2D = null;
					MultiAudioSource audioSource = null;
					string audioFile = strWord;
					AudioClip audio = SoundLoader.LoadSound(audioFile);
					if (audio != null)
					{
						audioSource = thingObject.AddComponent<MultiAudioSource>();
						audioSource.AudioClip = audio;
					}

					thingObject.transform.position = entity.origin;
					if (entity.entityData.ContainsKey("spawnflags"))
					{
						strWord = entity.entityData["spawnflags"];
						int spawnflags = int.Parse(strWord);
						if ((spawnflags & 3) != 0)
						{
							audioSource.Loop = true;
							if ((spawnflags & 1) != 0)
								audioSource.Play();
						}
						else
						{
							if ((spawnflags & 8) != 0) //Activator Sound
							{
								if (entity.entityData.ContainsKey("targetname"))
								{
									strWord = entity.entityData["targetname"];
									int target = int.Parse(strWord.Trim('t'));

									TriggerController tc;
									if (!triggerToActivate.TryGetValue(target, out tc))
									{
										tc = thingObject.AddComponent<TriggerController>();
										triggerToActivate.Add(target, tc);
									}
									tc.Repeatable = true;
									tc.SetController(target, (p) =>
									{
										if (audioFile.Contains('*'))
											p.PlayModelSound(audioFile.Trim('*'));
										else if (audioSource != null)
											audioSource.Play();
									});
								}
							}
							else
							{
								if ((spawnflags & 4) != 0) //Global 2D Sound
								{
									isAudio3d = false;
									Destroy(audioSource);
									audioSource2D = thingObject.AddComponent<AudioSource>();
									audioSource2D.clip = audio;
									audioSource2D.spatialBlend = 0;
									audioSource2D.minDistance = 1;
									audioSource2D.playOnAwake = false;
									audioSource2D.volume = GameOptions.MainVolume * GameOptions.SFXVolume;
								}
								if (entity.entityData.ContainsKey("targetname"))
								{
									strWord = entity.entityData["targetname"];
									int target = int.Parse(strWord.Trim('t'));

									TriggerController tc;
									if (!triggerToActivate.TryGetValue(target, out tc))
									{
										tc = thingObject.AddComponent<TriggerController>();
										triggerToActivate.Add(target, tc);
									}	
									tc.Repeatable = true;
									tc.SetController(target, (p) =>
									{
										if (isAudio3d)
											audioSource.Play();
										else
											audioSource2D.Play();
									});
								}
							}
						}
					}
					else if (entity.entityData.ContainsKey("random"))
						AddRandomTimeToSound(thingObject, entity.entityData);
				}
				break;
			}
			thingObject.transform.SetParent(GameManager.Instance.TemporaryObjectsHolder);
			thingObject.SetActive(true);
		}
	}
	public static void AddRandomTimeToSound(GameObject go, Dictionary<string, string> entityData)
	{
		PlayAfterRandomTime playRandom = go.AddComponent<PlayAfterRandomTime>();
		string strWord = entityData["random"];
		int random = int.Parse(strWord);
		strWord = entityData["wait"];
		int wait = int.Parse(strWord);
		playRandom.Init(wait, random);
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