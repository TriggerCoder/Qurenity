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
	public static int[] leafRenderLayer;
	public static List<QModel> models;
	public static List<QBrush> brushes;
	public static List<int> leafsBrushes;
	public static List<QBrushSide> brushSides;
	public static List<QShader> mapTextures;
	public static QVisData visData;

	public static HashSet<Collider> noMarks;

	public static GameObject MapMesh;
	public static GameObject MapColliders;

	public static LightMapSize currentLightMapSize = LightMapSize.Q3_QL;
	public enum LightMapSize
	{
		Q3_QL = 128,
		QAA = 512
	}
	public enum LightMapLenght
	{
		Q3_QL = 49152,		//128*128*3
		QAA = 786432		//512*512*3
	}
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
			leafRenderLayer = new int[num];
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

		//We need to determine the max number in order to check lightmap type
		int maxlightMapNum = 0;
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

				if (surfaces[i].lightMapID > maxlightMapNum)
					maxlightMapNum = surfaces[i].lightMapID;
			}
			//Need to count lightmap 0
			maxlightMapNum++;
		}

		//Q3/QL lightmaps (128x128x3)
		//QAA lightmaps (512x512x3)
		{
			//Check lightmap type
			int lightMapLenght = (int)LightMapLenght.QAA;
			if ((maxlightMapNum * lightMapLenght) > header.Directory[LumpType.LightMaps].Length)
				lightMapLenght = (int)LightMapLenght.Q3_QL;
			else
				currentLightMapSize = LightMapSize.QAA;

			BSPMap.BaseStream.Seek(header.Directory[LumpType.LightMaps].Offset, SeekOrigin.Begin);
			int num = header.Directory[LumpType.LightMaps].Length / lightMapLenght;
			lightMaps = new List<Texture2D>(num);
			for (int i = 0; i < num; i++)
			{
				lightMaps.Add(TextureLoader.CreateLightmapTexture(BSPMap.ReadBytes(lightMapLenght)));
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
		// We are only looking for bezier type
		var groupsurfaces = surfaces.Where(s => s.type == QSurfaceType.Patch);

		// Initialize 2 lists (one for test) to hold the vertices of each surface in the group
		List<QVertex> surfVerts = new List<QVertex>();
		List<QVertex> testVerts = new List<QVertex>();

		// Now searh all the vertexes for all the bezier surface
		foreach (var groupsurface in groupsurfaces)
		{
			testVerts.Clear();

			int startVert = groupsurface.startVertIndex;
			for (int j = 0; j < groupsurface.numOfVerts; j++)
				testVerts.Add(verts[startVert + j]);

			//Get number of groups for all the vertexes by their position, as we want to get the uniques it need to match the number of vertex
			int numGroups = testVerts.GroupBy(v => new { v.position.x, v.position.y, v.position.z }).Count();

			// If the verts are unique, add the test vertices to the surface list
			if (numGroups == groupsurface.numOfVerts)
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

	public static void GenerateGeometricSurface(GameObject go, int num)
	{
		Transform holder = go.transform;
		List<QSurface> staticGeometry = new List<QSurface>();
		for (int i = 0; i < models[num].numSurfaces; i++)
			staticGeometry.Add(surfaces[models[num].firstSurface + i]);

		// Each surface group is its own gameobject
		var groups = staticGeometry.GroupBy(x => new { x.type, x.shaderId, x.lightMapID });
		int groupId = 0;
		foreach (var group in groups)
		{
			QSurface[] groupSurfaces = group.ToArray();
			if (groupSurfaces.Length == 0)
				continue;

			GameObject modelObject = null;
			if (groupId == 0)
				modelObject = go;
			groupId++;

			switch (group.Key.type)
			{
				case QSurfaceType.Patch:
					Mesher.GenerateBezObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, holder, modelObject, false, groupSurfaces);
				break;
				case QSurfaceType.Polygon:
				case QSurfaceType.Mesh:
					Mesher.GeneratePolygonObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, holder, modelObject, false, groupSurfaces);
				break;
				case QSurfaceType.Billboard:
//					Mesher.GenerateBillBoardObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, holder, modelObject, groupSurfaces);
				break;
				default:
					Debug.LogWarning("Group " + groupId + "Skipped surface because it was not a polygon, mesh, or bez patch (" + group.Key.type + ").");
				break;
			}
		}
	}

	public static void GenerateGeometricCollider(Transform holder, int num, uint contentFlags = 0, bool isTrigger = false)
	{
		GenerateGeometricCollider(null, holder, num, contentFlags, isTrigger);
	}
	public static void GenerateGeometricCollider(GameObject go, int num, uint contentFlags = 0, bool isTrigger = true)
	{
		Transform holder = go.transform;
		GenerateGeometricCollider(go, holder, num, contentFlags, isTrigger);
	}

	public static void GenerateGeometricCollider(GameObject go, Transform holder, int num, uint contentFlags, bool isTrigger)
	{
		for (int i = 0; i < models[num].numBrushes; i++)
		{
			GameObject modelObject;
			if ((i == 0) && (go != null))
				modelObject = go;
			else
			{
				modelObject = new GameObject("Collider_" + i);
				modelObject.layer = GameManager.ColliderLayer;
				modelObject.transform.SetParent(holder);
				modelObject.transform.localPosition = Vector3.zero;
				modelObject.transform.localRotation = Quaternion.identity;
			}

			if (!Mesher.GenerateBrushCollider(brushes[models[num].firstBrush + i], holder, modelObject, !isTrigger))
				return;

			if (contentFlags != 0)
			{
				ContentType contentType = modelObject.GetComponent<ContentType>();
				contentType.Init(contentType.value | contentFlags);
			}

			if (isTrigger)
			{
				MeshCollider mc = modelObject.GetComponent<MeshCollider>();
				mc.isTrigger = true;
			}
		}
	}

	public static void GenerateJumpPadCollider(GameObject go, int num)
	{
		for (int i = 0; i < models[num].numBrushes; i++)
		{
			if (!Mesher.GenerateBrushCollider(brushes[models[num].firstBrush + i], ColliderGroup, go))
				continue;

			ContentType contentType = go.GetComponent<ContentType>();
			contentType.Init(contentType.value | ContentFlags.JumpPad);
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
					Mesher.GenerateBezObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, holder, groupSurfaces);
				break;
				case QSurfaceType.Polygon:
				case QSurfaceType.Mesh:
					Mesher.GeneratePolygonObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, holder, groupSurfaces);
				break;
				case QSurfaceType.Billboard:
//					Mesher.GenerateBillBoardObject(mapTextures[groupSurfaces[0].shaderId].name, groupSurfaces[0].lightMapID, groupId, holder, groupSurfaces);
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
