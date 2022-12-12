using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class Mesher
{
	public static Transform MapMeshes;

	private static List<Vector3> vertsCache = new List<Vector3>();
	private static List<Vector2> uvCache = new List<Vector2>();
	private static List<Vector2> uv2Cache = new List<Vector2>();
	private static List<Vector3> normalsCache = new List<Vector3>();
	private static List<Color> vertsColor = new List<Color>();
	private static List<int> indiciesCache = new List<int>();

	public const float APROX_ERROR = 0.001f;
	public const uint MaskSolid = ContentFlags.Solid;
	public const uint MaskPlayerSolid = ContentFlags.Solid | ContentFlags.PlayerClip | ContentFlags.Body;
	public const uint MaskDeadSolid = ContentFlags.Solid | ContentFlags.PlayerClip;
	public const uint MaskWater = ContentFlags.Water | ContentFlags.Lava | ContentFlags.Slime;
	public const uint MaskOpaque = ContentFlags.Solid | ContentFlags.Lava | ContentFlags.Slime;
	public const uint MaskShot = ContentFlags.Solid | ContentFlags.Body | ContentFlags.Corpse;

	public static void ClearMesherCache()
	{
		vertsCache = new List<Vector3>();
		uvCache = new List<Vector2>();
		uv2Cache = new List<Vector2>();
		normalsCache = new List<Vector3>();
		indiciesCache = new List<int>();
		vertsColor = new List<Color>();
		BezierMesh.ClearCaches();
	}
	public static void GenerateBezObject(string textureName, int lmIndex, int indexId, params QSurface[] surfaces)
	{
		if (surfaces == null || surfaces.Length == 0)
			return;

		string Name = "Bezier_Surfaces";
		int[] numPatches = new int[surfaces.Length];
		int totalPatches = 0;
		for (int i = 0; i < surfaces.Length; i++)
		{
			int patches = (surfaces[i].size[0] - 1) / 2 * ((surfaces[i].size[1] - 1) / 2);
			numPatches[i] = patches;
			totalPatches += patches;
			Name += "_" + surfaces[i].surfaceId;
		}

		CombineInstance[] combine = new CombineInstance[totalPatches];
		int index = 0;
		for (int i = 0; i < surfaces.Length; i++)
		{
			for (int n = 0; n < numPatches[i]; n++)
			{
				combine[index].mesh = GenerateBezMesh(surfaces[i], n);
				index++;
			}
		}

		int p = (surfaces[0].size[0] - 1) / 2 * ((surfaces[0].size[1] - 1) / 2);
		CombineInstance[] c = new CombineInstance[p];
		for (int i = 0; i < p; i++)
		{
			c[i].mesh = GenerateBezMesh(surfaces[0], i);
		}


		Mesh mesh = new Mesh();
		mesh.name = Name;
		mesh.CombineMeshes(combine, true, false, false);
//		mesh.CombineMeshes(c, true, false, false);

		GameObject bezObj = new GameObject();
		bezObj.layer = GameManager.MapMeshesLayer;
		bezObj.name = "Bezier_" + indexId;
		bezObj.transform.SetParent(MapMeshes);

		//PVS
		ClusterPVSController cluster = bezObj.AddComponent<ClusterPVSController>();
		cluster.RegisterClusterAndSurfaces(surfaces);

		bezObj.AddComponent<MeshFilter>().mesh = mesh;
		MeshRenderer meshRenderer = bezObj.AddComponent<MeshRenderer>();

		Material material = MaterialManager.GetMaterials(textureName, lmIndex);
		meshRenderer.sharedMaterial = material;
	}

	public static Mesh GenerateBezMesh(QSurface surface, int patchNumber)
	{
		//Calculate how many patches there are using size[]
		//There are n_patchesX by n_patchesY patches in the grid, each of those
		//starts at a vert (i,j) in the overall grid
		//We don't actually need to know how many are on the Y length
		//but the forumla is here for historical/academic purposes
		int n_patchesX = (surface.size[0] - 1) / 2;
		//int n_patchesY = ((surface.size[1]) - 1) / 2;


		//Calculate what [n,m] patch we want by using an index
		//called patchNumber  Think of patchNumber as if you 
		//numbered the patches left to right, top to bottom on
		//the grid in a piece of paper.
		int pxStep = 0;
		int pyStep = 0;
		for (int i = 0; i < patchNumber; i++)
		{
			pxStep++;
			if (pxStep == n_patchesX)
			{
				pxStep = 0;
				pyStep++;
			}
		}

		//Create an array the size of the grid, which is given by
		//size[] on the surface object.
		QVertex[,] vertGrid = new QVertex[surface.size[0], surface.size[1]];

		//Read the verts for this surface into the grid, making sure
		//that the final shape of the grid matches the size[] of
		//the surface.
		int gridXstep = 0;
		int gridYstep = 0;
		int vertStep = surface.startVertIndex;
		for (int i = 0; i < surface.numOfVerts; i++)
		{
			vertGrid[gridXstep, gridYstep] = MapLoader.verts[vertStep];
			vertStep++;
			gridXstep++;
			if (gridXstep == surface.size[0])
			{
				gridXstep = 0;
				gridYstep++;
			}
		}

		//We now need to pluck out exactly nine vertexes to pass to our
		//teselate function, so lets calculate the starting vertex of the
		//3x3 grid of nine vertexes that will make up our patch.
		//we already know how many patches are in the grid, which we have
		//as n and m.  There are n by m patches.  Since this method will
		//create one gameobject at a time, we only need to be able to grab
		//one.  The starting vertex will be called vi,vj think of vi,vj as x,y
		//coords into the grid.
		int vi = 2 * pxStep;
		int vj = 2 * pyStep;
		//Now that we have those, we need to get the vert at [vi,vj] and then
		//the two verts at [vi+1,vj] and [vi+2,vj], and then [vi,vj+1], etc.
		//the ending vert will at [vi+2,vj+2]

		int capacity = 3 * 3;
		List<Vector3> bverts = new List<Vector3>(capacity);

		//read texture/lightmap coords while we're at it
		//they will be tessellated as well.
		List<Vector2> uv = new List<Vector2>(capacity);
		List<Vector2> uv2 = new List<Vector2>(capacity);
		List<Color> color = new List<Color>(capacity);

		//Top row
		bverts.Add(vertGrid[vi, vj].position);
		bverts.Add(vertGrid[vi + 1, vj].position);
		bverts.Add(vertGrid[vi + 2, vj].position);

		uv.Add(vertGrid[vi, vj].textureCoord);
		uv.Add(vertGrid[vi + 1, vj].textureCoord);
		uv.Add(vertGrid[vi + 2, vj].textureCoord);

		uv2.Add(vertGrid[vi, vj].lightmapCoord);
		uv2.Add(vertGrid[vi + 1, vj].lightmapCoord);
		uv2.Add(vertGrid[vi + 2, vj].lightmapCoord);

		color.Add(vertGrid[vi, vj].color);
		color.Add(vertGrid[vi + 1, vj].color);
		color.Add(vertGrid[vi + 2, vj].color);

		//Middle row
		bverts.Add(vertGrid[vi, vj + 1].position);
		bverts.Add(vertGrid[vi + 1, vj + 1].position);
		bverts.Add(vertGrid[vi + 2, vj + 1].position);

		uv.Add(vertGrid[vi, vj + 1].textureCoord);
		uv.Add(vertGrid[vi + 1, vj + 1].textureCoord);
		uv.Add(vertGrid[vi + 2, vj + 1].textureCoord);

		uv2.Add(vertGrid[vi, vj + 1].lightmapCoord);
		uv2.Add(vertGrid[vi + 1, vj + 1].lightmapCoord);
		uv2.Add(vertGrid[vi + 2, vj + 1].lightmapCoord);

		color.Add(vertGrid[vi, vj + 1].color);
		color.Add(vertGrid[vi + 1, vj + 1].color);
		color.Add(vertGrid[vi + 2, vj + 1].color);

		//Bottom row
		bverts.Add(vertGrid[vi, vj + 2].position);
		bverts.Add(vertGrid[vi + 1, vj + 2].position);
		bverts.Add(vertGrid[vi + 2, vj + 2].position);

		uv.Add(vertGrid[vi, vj + 2].textureCoord);
		uv.Add(vertGrid[vi + 1, vj + 2].textureCoord);
		uv.Add(vertGrid[vi + 2, vj + 2].textureCoord);

		uv2.Add(vertGrid[vi, vj + 2].lightmapCoord);
		uv2.Add(vertGrid[vi + 1, vj + 2].lightmapCoord);
		uv2.Add(vertGrid[vi + 2, vj + 2].lightmapCoord);

		color.Add(vertGrid[vi, vj + 2].color);
		color.Add(vertGrid[vi + 1, vj + 2].color);
		color.Add(vertGrid[vi + 2, vj + 2].color);

/*
		for (int i = 0; i < bverts.Count; i++)
		{
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = "Spere_" + patchNumber + "_Surface_"+ (surface.startVertIndex)+ "_bverts" + i;
			sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			sphere.transform.position = bverts[i];
			MeshRenderer mr = sphere.GetComponent<MeshRenderer>();
			Material mat = Material.Instantiate(MaterialManager.Instance.debug);
			mat.color = color[i];
			mr.sharedMaterial = mat;
		}
*/
		//Now that we have our control grid, it's business as usual
		BezierMesh bezPatch = new BezierMesh(GameManager.Instance.tessellations, patchNumber, bverts, uv, uv2, color);
		return bezPatch.Mesh;
	}
	public static void GeneratePolygonObject(string textureName, int lmIndex, int indexId, params QSurface[] surfaces)
	{
		if (surfaces == null || surfaces.Length == 0)
		{
			Debug.LogWarning("Failed to create polygon object because there are no surfaces");
			return;
		}

		string Name = "Mesh_Surfaces_";

		// Our GeneratePolygonMesh will optimze and add the UVs for us
		CombineInstance[] combine = new CombineInstance[surfaces.Length];
		for (var i = 0; i < combine.Length; i++)
		{
			combine[i].mesh = GeneratePolygonMesh(surfaces[i]);
			Name += "_" + surfaces[i].surfaceId;
		}

		GameObject obj = new GameObject();
		obj.layer = GameManager.MapMeshesLayer;
		obj.name = "Mesh_"+indexId;
		obj.transform.SetParent(MapMeshes);

		//PVS
		ClusterPVSController cluster = obj.AddComponent<ClusterPVSController>();
		cluster.RegisterClusterAndSurfaces(surfaces);

		var mesh = new Mesh();
		mesh.name = Name;
		mesh.CombineMeshes(combine, true, false, false);

		
		MeshRenderer mr = obj.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		Material material = null;
		if (MaterialManager.GetOverrideMaterials(textureName, lmIndex, ref material, ref obj))
		{
			Debug.LogWarning("Found Material");
		}
		else
			material = MaterialManager.GetMaterials(textureName, lmIndex);

		mr.sharedMaterial = material;
	}

	public static void GenerateBillBoardObject(string textureName, int lmIndex, int indexId, params QSurface[] surfaces)
	{
		if (surfaces == null || surfaces.Length == 0)
		{
			Debug.LogWarning("Failed to create billboard object because there are no surfaces");
			return;
		}
		
		GameObject obj = new GameObject();
		obj.layer = GameManager.MapMeshesLayer;
		obj.name = "Billboard_" + indexId;
		Transform holder = obj.transform;
		holder.SetParent(MapMeshes);

		for (var i = 0; i < surfaces.Length; i++)
		{
			GameObject billboard = new GameObject();
			billboard.layer = GameManager.MapMeshesLayer;
			billboard.name = "Billboard_Surface" + surfaces[i].surfaceId;
			billboard.transform.SetParent(holder);

			Mesh mesh = GeneratePolygonMesh(surfaces[i]);
			MeshRenderer mr = billboard.AddComponent<MeshRenderer>();
			MeshFilter meshFilter = billboard.AddComponent<MeshFilter>();
			meshFilter.mesh = mesh;
			Material material = MaterialManager.GetMaterials(textureName, lmIndex);
			mr.sharedMaterial = material;
		}

		//PVS
		ClusterPVSController cluster = obj.AddComponent<ClusterPVSController>();
		cluster.RegisterClusterAndSurfaces(surfaces);
	}
	public static Mesh GeneratePolygonMesh(QSurface surface)
	{
		Mesh mesh = new Mesh();
		mesh.name = "BSPface (poly/mesh)";

		// Rip verts, uvs, and normals
		int vertexCount = surface.numOfVerts;
		if (vertsCache.Capacity < vertexCount)
		{
			vertsCache.Capacity = vertexCount;
			uvCache.Capacity = vertexCount;
			uv2Cache.Capacity = vertexCount;
			normalsCache.Capacity = vertexCount;
			vertsColor.Capacity = vertexCount;
		}

		if (indiciesCache.Capacity < surface.numOfIndices)
			indiciesCache.Capacity = surface.numOfIndices;

		vertsCache.Clear();
		uvCache.Clear();
		uv2Cache.Clear();
		normalsCache.Clear();
		indiciesCache.Clear();
		vertsColor.Clear();

		int vstep = surface.startVertIndex;
		for (int n = 0; n < surface.numOfVerts; n++)
		{
			vertsCache.Add(MapLoader.verts[vstep].position);
			uvCache.Add(MapLoader.verts[vstep].textureCoord);
			uv2Cache.Add(MapLoader.verts[vstep].lightmapCoord);
			normalsCache.Add(MapLoader.verts[vstep].normal);
			vertsColor.Add(MapLoader.verts[vstep].color);
			vstep++;
		}

		// Rip meshverts / triangles
		int mstep = surface.startIndex;
		for (int n = 0; n < surface.numOfIndices; n++)
		{
			indiciesCache.Add(MapLoader.vertIndices[mstep]);
			mstep++;
		}

		// add the verts, uvs, and normals we ripped to the gameobjects mesh filter
		mesh.SetVertices(vertsCache);
		mesh.SetNormals(normalsCache);

		// Add the texture co-ords (or UVs) to the surface/mesh
		mesh.SetUVs(0, uvCache);
		mesh.SetUVs(1, uv2Cache);

		// Add the vertex color
		mesh.SetColors(vertsColor);

		// add the meshverts to the object being built
		mesh.SetTriangles(indiciesCache, 0);

		// Let Unity do some heavy lifting for us
		mesh.RecalculateBounds();
		//            mesh.RecalculateNormals();
		//            mesh.Optimize();

		return mesh;
	}

	public static void GenerateBrushCollider(QBrush brush, Transform holder)
	{
		//Remove brushed used for BSP Generations and for Details
		uint type = MapLoader.mapTextures[brush.shaderId].contentsFlags;

		if (((type & ContentFlags.Details) != 0) || ((type & ContentFlags.Structural) != 0))
		{
//			Debug.Log("brushSide: " + brush.brushSide + " Not used for collisions, Content Type is: " + type);
			return;
		}

		GameObject objCollider = new GameObject(brush.brushSide + "_collider");
		objCollider.layer = GameManager.ColliderLayer;
		objCollider.transform.SetParent(holder);

		List<Vector3> possibleIntersectPoint = new List<Vector3>();
		List<Vector3> intersectPoint = new List<Vector3>();
		for (int i = 0; i < brush.numOfBrushSides; i++)
		{
			int planeIndex = MapLoader.brushSides[brush.brushSide + i].plane;
			QPlane p1 = MapLoader.planes[planeIndex];

			for (int j = i + 1; j < brush.numOfBrushSides; j++)
			{
				planeIndex = MapLoader.brushSides[brush.brushSide + j].plane;
				QPlane p2 = MapLoader.planes[planeIndex];
				for (int k = j + 1; k < brush.numOfBrushSides; k++)
				{
					planeIndex = MapLoader.brushSides[brush.brushSide + k].plane;
					QPlane p3 = MapLoader.planes[planeIndex];
					List<float> intersect = p1.IntersectPlanes(p2, p3);
					if (intersect != null)
						possibleIntersectPoint.Add(new Vector3(intersect[0], intersect[1], intersect[2]));
				}
			}
		}

		for (int i = 0; i < possibleIntersectPoint.Count; i++)
		{
			bool inside = true;
			for (int j = 0; j < brush.numOfBrushSides; j++)
			{
				int planeIndex = MapLoader.brushSides[brush.brushSide + j].plane;
				QPlane plane = MapLoader.planes[planeIndex];
				if (plane.GetSide(possibleIntersectPoint[i], QPlane.CheckPointPlane.IsFront))
				{
					inside = false;
					j = brush.numOfBrushSides;
				}
			}
			if (inside)
			{
				if (!intersectPoint.Contains(possibleIntersectPoint[i]))			
					intersectPoint.Add(possibleIntersectPoint[i]);
			}
		}

		intersectPoint = RemoveDuplicatedVectors(intersectPoint);

		Mesh mesh = ConvexHull.GenerateMeshFromConvexHull("brushSide: " + brush.brushSide,intersectPoint);
		MeshCollider mc = objCollider.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;
		mc.convex = true;
		Rigidbody rb = objCollider.AddComponent<Rigidbody>();
		rb.isKinematic = true;
		rb.useGravity = false;
		rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

		ContentType contentType = objCollider.AddComponent<ContentType>();
		contentType.Init(type);

		if ((contentType.value & MaskPlayerSolid) == 0)
			mc.isTrigger = true;

		if ((contentType.value & ContentFlags.PlayerClip) == 0)
			objCollider.layer = GameManager.InvisibleBlockerLayer;

		type = MapLoader.mapTextures[brush.shaderId].surfaceFlags;
		SurfaceType surfaceType = objCollider.AddComponent<SurfaceType>();
		surfaceType.Init(type);
//		if ((type & SurfaceFlags.NonSolid) != 0)
//			Debug.Log("brushSide: " + brush.brushSide + " Surface Type is: " + type);


	}

	public static List<Vector3> RemoveDuplicatedVectors(List<Vector3> test)
	{
		List<Vector3> uniqueVector = new List<Vector3>();
		for (int i = 0; i < test.Count; i++)
		{
			bool isUnique = true;
			for (int j = i + 1; j < test.Count; j++)
			{
				if (FloatAprox(test[i].x, test[j].x))
					if (FloatAprox(test[i].y, test[j].y))
						if (FloatAprox(test[i].z, test[j].z))
							isUnique = false;
			}
			if (isUnique)
				uniqueVector.Add(new Vector3(RoundUp4Decimals(test[i].x), RoundUp4Decimals(test[i].y), RoundUp4Decimals(test[i].z)));
		}
		return uniqueVector;
	}
	public static bool FloatAprox(float f1, float f2)
	{
		float d = f1 - f2;

		if (Mathf.Abs(d) > APROX_ERROR)
			return false;
		return true;
	}
	public static float RoundUp4Decimals(float f)
	{
		float d = Mathf.CeilToInt(f * 10000) / 10000.0f;
		return d;
	}

}
