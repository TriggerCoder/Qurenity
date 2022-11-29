using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using Pathfinding.Ionic.Zip;
using UnityEngine;
public static class MapLoader
{
	public static string CurrentMap;


	private static BinaryReader BSPMap;

	public static EntityLump entityLump;

	public static BSPHeader header;

	private static Transform DynamicMeshes;

	public static List<Face> faces;
	public static List<Texture2D> lightMaps;
	public static List<Vertex> verts;
	public static List<int> vertIndices;
	public static List<Plane> planes;
	public static List<Node> nodes;
	public static List<Leaf> leafs;
	public static List<int> leafsFaces;
	public static int[] leafRenderFrame;
	public static List<BSPTexture> mapTextures;
	public static VisData visData;
	
	public static bool IsSkyTexture(string textureName)
	{
		if (textureName == "F_SKY1")
			return true;
		return false;
	}
	public static bool Load(string mapName)
	{

		string path = Application.streamingAssetsPath + "/maps/" + mapName + ".bsp";
		if (File.Exists(path))
			BSPMap = new BinaryReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.ContainsKey(path = ("maps/" + mapName + ".bsp").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			ZipFile zip = ZipFile.Read(stream);
			ZipEntry map = zip[path];
			MemoryStream ms = new MemoryStream();
			map.Extract(ms);
			BSPMap = new BinaryReader(ms);
		}
		else
			return false;

		//header
		{
			header = new BSPHeader(BSPMap);
		}

		//entities
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Entities].Offset, SeekOrigin.Begin);
			entityLump = new EntityLump(new string(BSPMap.ReadChars(header.Directory[LumpType.Entities].Length)));
			Debug.Log(entityLump.ToString());
		}

		//textures
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Textures].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Textures].Length / 72;
			mapTextures = new List<BSPTexture>(num);
			for (int i = 0; i < num; i++)
			{
				mapTextures.Add(new BSPTexture(new string(BSPMap.ReadChars(64)), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//planes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Planes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Planes].Length / 16;
			planes = new List<Plane>(num);
			for (int i = 0; i < num; i++)
			{
				planes.Add(new Plane(new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), BSPMap.ReadSingle()));
			}
		}

		//vertices
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Vertexes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Vertexes].Length / 44;
			verts = new List<Vertex>(num);
			for (int i = 0; i < num; i++)
			{
				verts.Add(new Vertex(new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()),
													BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle(),
													new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), BSPMap.ReadBytes(4)));
			}
		}

		//faces
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Faces].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Faces].Length / 104;
			faces = new List<Face>(num);
			for (int i = 0; i < num; i++)
			{
				faces.Add(new Face(i, BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(),
					BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), new[]
					{
						BSPMap.ReadInt32(),
						BSPMap.ReadInt32()
					}, new[]
					{
						BSPMap.ReadInt32(),
						BSPMap.ReadInt32()
					}, new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), new[]
					{
						new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()),
						new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle())
					}, new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), new[]
					{
						BSPMap.ReadInt32(),
						BSPMap.ReadInt32()
					}));
			}
		}

		//vertex indices
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.VertIndices].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.VertIndices].Length / 4;
			vertIndices = new List<int>(num);
			for (int i = 0; i < num; i++)
			{
				vertIndices.Add(BSPMap.ReadInt32());
			}
		}

		//lightmaps (128x128x3)
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.LightMaps].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LightMaps].Length / 49152;
			lightMaps = new List<Texture2D>(num);
			for (int i = 0; i < num; i++)
			{
				lightMaps.Add(TextureLoader.CreateLightmapTexture(BSPMap.ReadBytes(49152)));
			}
		}

		//nodes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Nodes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Nodes].Length / 36;
			nodes = new List<Node>(num);
			for (int i = 0; i < num; i++)
			{
				nodes.Add(new Node(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32())));
			}
		}

		//leafs
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Leafs].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Leafs].Length / 48;
			leafs = new List<Leaf>(num);
			for (int i = 0; i<num; i++)
			{
				leafs.Add(new Leaf(BSPMap.ReadInt32(), BSPMap.ReadInt32(), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//leafs faces
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.LeafFaces].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LeafFaces].Length / 4;
			leafsFaces = new List<int>(num);
			leafRenderFrame = new int[num];
			for (int i = 0; i < num; i++)
			{
				leafsFaces.Add(BSPMap.ReadInt32());
			}
		}
		
		//vis data
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.VisData].Offset, SeekOrigin.Begin);
			if (header.Directory[LumpType.VisData].Length > 0)
			{
				visData = new VisData(BSPMap.ReadInt32(), BSPMap.ReadInt32());
				visData.bitSets = BSPMap.ReadBytes(visData.numOfClusters * visData.bytesPerCluster);
			}
		}

		GetMapTextures();
		BSPMap.Close();

		return true;
	}

	public static void GetMapTextures()
	{
		TextureLoader.LoadJPGTextures(mapTextures);
		TextureLoader.LoadTGATextures(mapTextures);
	}

	public static void GenerateFaces()
	{
		GameObject MapMesh = new GameObject("MapMeshes");
		MapMesh.layer = GameManager.MapMeshesLayer;
		Transform holder = MapMesh.transform;
		Mesher.MapMeshes = holder;

		holder.transform.SetParent(GameManager.Instance.transform);


		// Each face group is its own gameobject
		var groups = faces.GroupBy(x => new { x.type, x.textureID, x.lightMapID });
		foreach (var group in groups)
		{
			Face[] faces = group.ToArray();
			if (faces.Length == 0)
				continue;

			Material mat = MaterialManager.GetMaterials(mapTextures[faces[0].textureID].Name, faces[0].lightMapID);

			switch (group.Key.type)
			{
				case FaceType.Patch:
					{
						Mesher.GenerateBezObject(mat, faces);
						break;
					}

				case FaceType.Polygon:
				case FaceType.Mesh:
					{
						Mesher.GeneratePolygonObject(mat, faces);
						break;
					}

				default:
					Debug.Log($"Skipped face because it was not a polygon, mesh, or bez patch ({group.Key.type}).");
					break;
			}
		}

		System.GC.Collect(2, System.GCCollectionMode.Forced);
	}

	public static string PrintFacesInfo()
	{
		StringBuilder blob = new StringBuilder();
		int count = 0;
		foreach (Face face in faces)
		{
			blob.AppendLine("Face " + count + "\t Tex: " + face.textureID + "\tType: " + face.type + "\tVertIndex: " +
							face.startVertIndex + "\tNumVerts: " + face.numOfVerts + "\tMeshVertIndex: " + face.startIndex +
							"\tMeshVerts: " + face.numOfIndices + "\r\n");
			count++;
		}

		return blob.ToString();
	}

	public static string PrintVertexInfo()
	{
		StringBuilder blob = new StringBuilder();
		for (int i = 0; i < verts.Count; i++)
		{
			blob.Append("Vertex " + i + " Pos: " + verts[i].position + " Normal: " + verts[i].normal + "\r\n");
		}

		return blob.ToString();
	}
}
