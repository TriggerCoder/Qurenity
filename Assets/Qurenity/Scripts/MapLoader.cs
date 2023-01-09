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

	public static BSPHeader header;

	public static Transform ColliderGroup;

	public static List<QSurface> surfaces;
	public static List<Texture2D> lightMaps;
	public static List<QVertex> verts;
	public static List<int> vertIndices;
	public static List<QPlane> planes;
	public static List<QNode> nodes;
	public static List<QLeaf> leafs;
	public static List<int> leafsSurfaces;
	public static int[] leafRenderFrame;
	public static List<QModel> models;
	public static List<QBrush> brushes;
	public static List<int> leafsBrushes;
	public static List<QBrushSide> brushSides;
	public static List<QShader> mapTextures;
	public static QVisData visData;

	public static HashSet<Collider> noMarks;

	public static GameObject MapMesh;
	public static GameObject MapColliders;

	public static bool IsSkyTexture(string textureName)
	{
		if (textureName.ToUpper().Contains("/SKIES/"))
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

		noMarks = new HashSet<Collider>();

		//header
		{
			header = new BSPHeader(BSPMap);
		}

		//entities
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Entities].Offset, SeekOrigin.Begin);
			ThingsManager.ReadEntities(BSPMap.ReadBytes(header.Directory[LumpType.Entities].Length));
		}

		//shaders (textures)
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Shaders].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Shaders].Length / 72;
			mapTextures = new List<QShader>(num);
			for (int i = 0; i < num; i++)
			{
				mapTextures.Add(new QShader(new string(BSPMap.ReadChars(64)), BSPMap.ReadUInt32(), BSPMap.ReadUInt32(), false));
			}
		}

		//planes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Planes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Planes].Length / 16;
			planes = new List<QPlane>(num);
			for (int i = 0; i < num; i++)
			{
				planes.Add(new QPlane(new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), BSPMap.ReadSingle()));
			}
		}

		//nodes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Nodes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Nodes].Length / 36;
			nodes = new List<QNode>(num);
			for (int i = 0; i < num; i++)
			{
				nodes.Add(new QNode(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32())));
			}
		}

		//leafs
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Leafs].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Leafs].Length / 48;
			leafs = new List<QLeaf>(num);
			for (int i = 0; i < num; i++)
			{
				leafs.Add(new QLeaf(BSPMap.ReadInt32(), BSPMap.ReadInt32(), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), new Vector3Int(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//leafs faces
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.LeafSurfaces].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LeafSurfaces].Length / 4;
			leafsSurfaces = new List<int>(num);
			leafRenderFrame = new int[num];
			for (int i = 0; i < num; i++)
			{
				leafsSurfaces.Add(BSPMap.ReadInt32());
			}
		}

		//leafs brushes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.LeafBrushes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LeafBrushes].Length / 4;
			leafsBrushes = new List<int>(num);
			for (int i = 0; i < num; i++)
			{
				leafsBrushes.Add(BSPMap.ReadInt32());
			}
		}

		//models (map geometry)
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Models].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Models].Length / 40;
			models = new List<QModel>(num);
			for (int i = 0; i < num; i++)
			{
				models.Add(new QModel(new Vector3(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()),
										new Vector3(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()),
										BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//brushes
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Brushes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Brushes].Length / 12;
			brushes = new List<QBrush>(num);
			for (int i = 0; i < num; i++)
			{
				brushes.Add(new QBrush(BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//brush sides
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.BrushSides].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.BrushSides].Length / 8;
			brushSides = new List<QBrushSide>(num);
			for (int i = 0; i < num; i++)
			{
				brushSides.Add(new QBrushSide(BSPMap.ReadInt32(), BSPMap.ReadInt32()));
			}
		}

		//vertices
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Vertexes].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Vertexes].Length / 44;
			verts = new List<QVertex>(num);
			for (int i = 0; i < num; i++)
			{
				verts.Add(new QVertex(i, new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()),
													BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle(),
													new Vector3(BSPMap.ReadSingle(), BSPMap.ReadSingle(), BSPMap.ReadSingle()), BSPMap.ReadBytes(4)));
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

		//surfaces
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.Surfaces].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.Surfaces].Length / 104;
			surfaces = new List<QSurface>(num);
			for (int i = 0; i < num; i++)
			{
				surfaces.Add(new QSurface(i, BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(), BSPMap.ReadInt32(),
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

		//vis data
		{
			BSPMap.BaseStream.Seek(header.Directory[LumpType.VisData].Offset, SeekOrigin.Begin);
			if (header.Directory[LumpType.VisData].Length > 0)
			{
				visData = new QVisData(BSPMap.ReadInt32(), BSPMap.ReadInt32());
				visData.bitSets = BSPMap.ReadBytes(visData.numOfClusters * visData.bytesPerCluster);
			}
		}
		
		LerpColorOnRepeatedVertex();
		GetMapTextures();
		BSPMap.Close();

		return true;
	}

	public static void LerpColorOnRepeatedVertex()
	{
		// Group surfaces by their type
		var sGroups = surfaces.GroupBy(s => new { s.type });

		foreach (var sGroup in sGroups)
		{
			QSurface[] groupsurfaces = sGroup.ToArray();

			// If the array is empty, skip to the next group
			if (groupsurfaces.Length == 0)
				continue;

			// We are only looking for bezier type
			if (sGroup.Key.type != QSurfaceType.Patch)
				continue;

			// Initialize 2 lists (one for test) to hold the vertices of each surface in the group
			List<QVertex> surfVerts = new List<QVertex>();
			List<QVertex> testVerts = new List<QVertex>();

			// Now searh all the vertexes for all the bezier surface
			for (int i = 0; i < groupsurfaces.Length; i++)
			{
				testVerts.Clear();
				int vertStep = groupsurfaces[i].startVertIndex;
				for (int j = 0; j < groupsurfaces[i].numOfVerts; j++)
				{
					testVerts.Add(verts[vertStep]);
					vertStep++;
				}

				// Group all the vertexes by their position, as we want to get the uniques
				var tGroups = testVerts.GroupBy(v => new { v.position.x, v.position.y, v.position.z });
				bool unique = true;

				foreach (var tGroup in tGroups)
				{
					QVertex[] testVerteces = tGroup.ToArray();
					if (testVerteces.Length == 0)
						continue;

					if (testVerteces.Length != 1)
						unique = false;
				}

				// If the vertex is unique, add the test vertices to the surface list
				if (unique)
					surfVerts.AddRange(testVerts);
			}

			//Now we got unique vertexes for each bezier surface, search for common positions
			var vGroups = surfVerts.GroupBy(v => new { v.position.x, v.position.y, v.position.z });

			foreach (var vGroup in vGroups)
			{
				QVertex[] groupVerteces = vGroup.ToArray();

				if (groupVerteces.Length == 0)
					continue;

				// Set the initial color to the color of the first vertex in the group
				// The we will be interpolating the color of every common vertex
				Color color = groupVerteces[0].color;
				for (int i = 1; i < groupVerteces.Length; i++)
					color = Color.Lerp(color, groupVerteces[i].color, 0.5f);


				// Finally set the final color to all the common vertexex
				for (int i = 0; i < groupVerteces.Length; i++)
				{
					int index = groupVerteces[i].vertId;
					verts[index].color = color;
				}
			}
		}
	}

	public static void GenerateMapCollider()
	{
		GameObject MapColliders = new GameObject("MapColliders");
		MapColliders.layer = GameManager.ColliderLayer;
		ColliderGroup = MapColliders.transform;
		ColliderGroup.transform.SetParent(GameManager.Instance.transform);

		for (int i = 0; i < models[0].numBrushes; i++)
		{
			Mesher.GenerateBrushCollider(brushes[models[0].firstBrush + i], ColliderGroup);
		}
	}

	public static void GenerateGeometricCollider(GameObject go, int num)
	{
		for (int i = 0; i < models[num].numBrushes; i++)
		{
			Mesher.GenerateBrushCollider(brushes[models[num].firstBrush + i], ColliderGroup, go, false);
			ContentType contentType = go.GetComponent<ContentType>();
			contentType.JumpPad = true;
			MeshCollider mc = go.GetComponent<MeshCollider>();
			mc.isTrigger = true;
		}
	}

	public static void GenerateJumpPadCollider(GameObject go, int num)
	{
		for (int i = 0; i < models[num].numBrushes; i++)
		{
			Mesher.GenerateBrushCollider(brushes[models[num].firstBrush + i], ColliderGroup, go, false);
			ContentType contentType = go.GetComponent<ContentType>();
			contentType.JumpPad = true;
			MeshCollider mc = go.GetComponent<MeshCollider>();
			Vector3 center = mc.bounds.center;
			Vector3 extents = mc.bounds.extents;
			float max = Mathf.Max(extents.x, extents.y, extents.z);
			GameObject.Destroy(mc);
			SphereCollider sc = go.AddComponent<SphereCollider>();
			sc.radius = max;
			sc.isTrigger = true;
			go.transform.position = center;
		}
	}
	public static void GetMapTextures()
	{
		TextureLoader.LoadJPGTextures(mapTextures);
		TextureLoader.LoadTGATextures(mapTextures);
	}

	public static void GenerateSurfaces()
	{
		MapMesh = new GameObject("MapMeshes");
		MapMesh.layer = GameManager.MapMeshesLayer;
		Transform holder = MapMesh.transform;
		Mesher.MapMeshes = holder;

		holder.transform.SetParent(GameManager.Instance.transform);

		List<QSurface> staticGeometry = new List<QSurface>();
		for (int i = 0; i < models[0].numSurfaces; i++)
			staticGeometry.Add(surfaces[models[0].firstSurface + i]);

		// Each surface group is its own gameobject
		var groups = staticGeometry.GroupBy(x => new { x.type, x.shaderId, x.lightMapID });
		int groupId = 0;
		foreach (var group in groups)
		{
			QSurface[] groupSurfaces = group.ToArray();
			if (groupSurfaces.Length == 0)
				continue;
			
				groupId++;

			switch (group.Key.type)
			{
				case QSurfaceType.Patch:
						Mesher.GenerateBezObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, groupSurfaces);
						break;
				case QSurfaceType.Polygon:
				case QSurfaceType.Mesh:
						Mesher.GeneratePolygonObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, groupSurfaces);
						break;
				case QSurfaceType.Billboard:
//						Mesher.GenerateBillBoardObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, groupSurfaces);
					break;
					

				default:
					Debug.LogWarning("Group "+ groupId + "Skipped surface because it was not a polygon, mesh, or bez patch ("+group.Key.type+").");
					break;
			}
		}

		System.GC.Collect(2, System.GCCollectionMode.Forced);
	}

	public static string PrintFacesInfo()
	{
		StringBuilder blob = new StringBuilder();
		int count = 0;
		foreach (QSurface surface in surfaces)
		{
			blob.AppendLine("Surface " + count + "\t Tex: " + surface.shaderId + "\tType: " + surface.type + "\tVertIndex: " +
							surface.startVertIndex + "\tNumVerts: " + surface.numOfVerts + "\tMeshVertIndex: " + surface.startIndex +
							"\tMeshVerts: " + surface.numOfIndices + "\r\n");
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