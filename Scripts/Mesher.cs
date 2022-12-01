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
	private static List<int> indiciesCache = new List<int>();

	public const float EPSILON = 0.00001f;
	public static void ClearMesherCache()
	{
		vertsCache = new List<Vector3>();
		uvCache = new List<Vector2>();
		uv2Cache = new List<Vector2>();
		normalsCache = new List<Vector3>();
		indiciesCache = new List<int>();
		BezierMesh.ClearCaches();
	}
	public static void GenerateBezObject(Material material, int indexId, params Face[] faces)
	{
		if (faces == null || faces.Length == 0)
			return;

		string Name = "Bezier_Faces";
		int[] numPatches = new int[faces.Length];
		int totalPatches = 0;
		for (int i = 0; i < faces.Length; i++)
		{
			int patches = (faces[i].size[0] - 1) / 2 * ((faces[i].size[1] - 1) / 2);
			numPatches[i] = patches;
			totalPatches += patches;
			Name += "_" + faces[i].faceId;
		}

		CombineInstance[] combine = new CombineInstance[totalPatches];
		int index = 0;
		for (int i = 0; i < faces.Length; i++)
		{
			for (int n = 0; n < numPatches[i]; n++)
			{
				combine[index].mesh = GenerateBezMesh(faces[i], n);
				index++;
			}
		}

		int p = (faces[0].size[0] - 1) / 2 * ((faces[0].size[1] - 1) / 2);
		CombineInstance[] c = new CombineInstance[p];
		for (int i = 0; i < p; i++)
		{
			c[i].mesh = GenerateBezMesh(faces[0], i);
		}


		Mesh mesh = new Mesh();
		mesh.name = Name;
		mesh.CombineMeshes(combine, true, false, false);
		//            mesh.CombineMeshes(c, true, false, false);

		GameObject bezObj = new GameObject();
		bezObj.layer = GameManager.MapMeshesLayer;
		bezObj.name = "Bezier_" + indexId;
		bezObj.transform.SetParent(MapMeshes);

		//PVS
		ClusterPVSController cluster = bezObj.AddComponent<ClusterPVSController>();
		cluster.RegisterClusterAndFaces(faces);

		bezObj.AddComponent<MeshFilter>().mesh = mesh;
		MeshRenderer meshRenderer = bezObj.AddComponent<MeshRenderer>();

//Don't use meshColliders		
		MeshCollider mc = bezObj.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;

		meshRenderer.sharedMaterial = material;
	}

	public static Mesh GenerateBezMesh(Face face, int patchNumber)
	{
		//Calculate how many patches there are using size[]
		//There are n_patchesX by n_patchesY patches in the grid, each of those
		//starts at a vert (i,j) in the overall grid
		//We don't actually need to know how many are on the Y length
		//but the forumla is here for historical/academic purposes
		int n_patchesX = (face.size[0] - 1) / 2;
		//int n_patchesY = ((face.size[1]) - 1) / 2;


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
		//size[] on the face object.
		Vertex[,] vertGrid = new Vertex[face.size[0], face.size[1]];

		//Read the verts for this face into the grid, making sure
		//that the final shape of the grid matches the size[] of
		//the face.
		int gridXstep = 0;
		int gridYstep = 0;
		int vertStep = face.startVertIndex;
		for (int i = 0; i < face.numOfVerts; i++)
		{
			vertGrid[gridXstep, gridYstep] = MapLoader.verts[vertStep];
			vertStep++;
			gridXstep++;
			if (gridXstep == face.size[0])
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

		//Now that we have our control grid, it's business as usual
		BezierMesh bezPatch = new BezierMesh(GameManager.Instance.tessellations, patchNumber, bverts, uv, uv2);
		return bezPatch.Mesh;
	}

	public static void GeneratePolygonObject(Material material, int indexId, params Face[] faces)
	{
		if (faces == null || faces.Length == 0)
		{
			Debug.LogWarning("Failed to create polygon object because there are no faces");
			return;
		}

		string Name = "Mesh_Faces_";

		// Our GeneratePolygonMesh will optimze and add the UVs for us
		CombineInstance[] combine = new CombineInstance[faces.Length];
		for (var i = 0; i < combine.Length; i++)
		{
			combine[i].mesh = GeneratePolygonMesh(faces[i]);
			Name += "_" + faces[i].faceId;
		}

		GameObject obj = new GameObject();
		obj.layer = GameManager.MapMeshesLayer;
		obj.name = "Mesh_"+indexId;
		obj.transform.SetParent(MapMeshes);

		//PVS
		ClusterPVSController cluster = obj.AddComponent<ClusterPVSController>();
		cluster.RegisterClusterAndFaces(faces);

		var mesh = new Mesh();
		mesh.name = Name;
		mesh.CombineMeshes(combine, true, false, false);

		
		MeshRenderer mr = obj.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		//Don't use meshColliders		
		MeshCollider mc = obj.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;

		mr.sharedMaterial = material;
	}

	public static Mesh GeneratePolygonMesh(Face face)
	{
		Mesh mesh = new Mesh();
		mesh.name = "BSPface (poly/mesh)";

		// Rip verts, uvs, and normals
		int vertexCount = face.numOfVerts;
		if (vertsCache.Capacity < vertexCount)
		{
			vertsCache.Capacity = vertexCount;
			uvCache.Capacity = vertexCount;
			uv2Cache.Capacity = vertexCount;
			normalsCache.Capacity = vertexCount;
		}

		if (indiciesCache.Capacity < face.numOfIndices)
			indiciesCache.Capacity = face.numOfIndices;

		vertsCache.Clear();
		uvCache.Clear();
		uv2Cache.Clear();
		normalsCache.Clear();
		indiciesCache.Clear();

		int vstep = face.startVertIndex;
		for (int n = 0; n < face.numOfVerts; n++)
		{
			vertsCache.Add(MapLoader.verts[vstep].position);
			uvCache.Add(MapLoader.verts[vstep].textureCoord);
			uv2Cache.Add(MapLoader.verts[vstep].lightmapCoord);
			normalsCache.Add(MapLoader.verts[vstep].normal);
			vstep++;
		}

		// Rip meshverts / triangles
		int mstep = face.startIndex;
		for (int n = 0; n < face.numOfIndices; n++)
		{
			indiciesCache.Add(MapLoader.vertIndices[mstep]);
			mstep++;
		}

		// add the verts, uvs, and normals we ripped to the gameobjects mesh filter
		mesh.SetVertices(vertsCache);
		mesh.SetNormals(normalsCache);

		// Add the texture co-ords (or UVs) to the face/mesh
		mesh.SetUVs(0, uvCache);
		mesh.SetUVs(1, uv2Cache);

		// add the meshverts to the object being built
		mesh.SetTriangles(indiciesCache, 0);

		// Let Unity do some heavy lifting for us
		mesh.RecalculateBounds();
		//            mesh.RecalculateNormals();
		//            mesh.Optimize();

		return mesh;
	}

	public static void GenerateColliderBox(Brush brush, Transform holder)
	{
//		GameObject mc = new GameObject(brush.brushSide + "_collider");
//		mc.transform.SetParent(holder);

		Vector3 center = Vector3.zero;
		Vector3 normal = Vector3.zero;
		List<Vector3> possibleIntersectPoint = new List<Vector3>();
		List<Vector3> intersectPoint = new List<Vector3>();
		for (int i = 0; i < brush.numOfBrushSides; i++)
		{
			int planeIndex = MapLoader.brushSides[brush.brushSide + i].plane;
			Plane3D p1 = MapLoader.planes[planeIndex];
/*
			{
				GameObject planeObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
				planeObj.name = "Plane_" + planeIndex;
				planeObj.transform.position = p1.normal * p1.distance;
				planeObj.transform.up = p1.normal;
				planeObj.transform.localScale = new Vector3(10f, 10f, 10f);

				planeObj.transform.SetParent(holder);
			}
*/
			for (int j = i + 1; j < brush.numOfBrushSides; j++)
			{
				planeIndex = MapLoader.brushSides[brush.brushSide + j].plane;
				Plane3D p2 = MapLoader.planes[planeIndex];
				for (int k = j + 1; k < brush.numOfBrushSides; k++)
				{
					planeIndex = MapLoader.brushSides[brush.brushSide + k].plane;
					Plane3D p3 = MapLoader.planes[planeIndex];
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
				Plane3D plane = MapLoader.planes[planeIndex];
				if (plane.GetSide(possibleIntersectPoint[i], Plane3D.CheckPointPlane.IsFront))
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
		
		{
			possibleIntersectPoint = intersectPoint.ToArray().ToList();
			intersectPoint.Clear();
			for (int i = 0; i < possibleIntersectPoint.Count; i++)
			{
				bool isUnique = true;
				for (int j = i + 1; j < possibleIntersectPoint.Count; j++)
				{
					if (FloatAprox(possibleIntersectPoint[i].x, possibleIntersectPoint[j].x))
						if (FloatAprox(possibleIntersectPoint[i].y, possibleIntersectPoint[j].y))
							if (FloatAprox(possibleIntersectPoint[i].z, possibleIntersectPoint[j].z))
								isUnique = false;
				}
				if (isUnique)
					intersectPoint.Add(new Vector3(RoundUp4Decimals(possibleIntersectPoint[i].x), RoundUp4Decimals(possibleIntersectPoint[i].y), RoundUp4Decimals(possibleIntersectPoint[i].z)));
			}
		}


		for (int i = 0; i < intersectPoint.Count; i++)
		{
			GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			sphere.name = "Spere_" + brush.brushSide + "_collider" + i;
			sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			sphere.transform.position = intersectPoint[i];
			sphere.transform.SetParent(holder);
		}
		if ((intersectPoint.Count & 1) != 0)
			Debug.LogWarning("brushSide: " + brush.brushSide + " intersectPoint " + intersectPoint.Count);		
/*
		mc.transform.position = center / 2f;
		mc.transform.forward = normal;
		BoxCollider bc = mc.AddComponent<BoxCollider>();
		bc.center = Vector3.zero;
//		bc.size = new Vector3((s.Line.start.Position - s.Line.end.Position).magnitude, (max - min), .01f);
		Rigidbody rb = mc.AddComponent<Rigidbody>();
		rb.isKinematic = true;
		rb.useGravity = false;
		rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
*/	}

	public static bool FloatAprox(float f1, float f2)
	{
		float d = f1 - f2;

//		return Mathf.Approximately(f1, f2);

		if (Mathf.Abs(d) > EPSILON)
			return false;
		return true;
	}

	public static float RoundUp4Decimals(float f)
	{
		float d = Mathf.CeilToInt(f * 10000) / 10000.0f;
		return d;
	}

}
