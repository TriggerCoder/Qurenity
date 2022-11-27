using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Mesher
{
	public static Transform MapMeshes;

	private static List<Vector3> vertsCache = new List<Vector3>();
	private static List<Vector2> uvCache = new List<Vector2>();
	private static List<Vector2> uv2Cache = new List<Vector2>();
	private static List<Vector3> normalsCache = new List<Vector3>();
	private static List<int> indiciesCache = new List<int>();

	public static void ClearMesherCache()
	{
		vertsCache = new List<Vector3>();
		uvCache = new List<Vector2>();
		uv2Cache = new List<Vector2>();
		normalsCache = new List<Vector3>();
		indiciesCache = new List<int>();
		BezierMesh.ClearCaches();
	}
	public static void GenerateBezObject(Material material, params Face[] faces)
	{
		if (faces == null || faces.Length == 0)
			return;

		int[] numPatches = new int[faces.Length];
		int totalPatches = 0;
		for (int i = 0; i < faces.Length; i++)
		{
			int patches = (faces[i].size[0] - 1) / 2 * ((faces[i].size[1] - 1) / 2);
			numPatches[i] = patches;
			totalPatches += patches;
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
		mesh.CombineMeshes(combine, true, false, false);
		//            mesh.CombineMeshes(c, true, false, false);

		GameObject bezObj = new GameObject();
		bezObj.layer = GameManager.MapMeshesLayer;
		bezObj.name = "Bezier";
		bezObj.transform.SetParent(MapMeshes);
		bezObj.AddComponent<MeshFilter>().mesh = mesh;
		MeshRenderer meshRenderer = bezObj.AddComponent<MeshRenderer>();

//Don't use meshColliders		
		MeshCollider mc = bezObj.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;

		meshRenderer.sharedMaterial = material;
#if UNITY_EDITOR
		if (!Application.isPlaying)
			bezObj.isStatic = true;
#endif
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
		int vertStep = face.vertex;
		for (int i = 0; i < face.n_vertexes; i++)
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

		uv.Add(vertGrid[vi, vj].texcoord);
		uv.Add(vertGrid[vi + 1, vj].texcoord);
		uv.Add(vertGrid[vi + 2, vj].texcoord);

		uv2.Add(vertGrid[vi, vj].lmcoord);
		uv2.Add(vertGrid[vi + 1, vj].lmcoord);
		uv2.Add(vertGrid[vi + 2, vj].lmcoord);

		//Middle row
		bverts.Add(vertGrid[vi, vj + 1].position);
		bverts.Add(vertGrid[vi + 1, vj + 1].position);
		bverts.Add(vertGrid[vi + 2, vj + 1].position);

		uv.Add(vertGrid[vi, vj + 1].texcoord);
		uv.Add(vertGrid[vi + 1, vj + 1].texcoord);
		uv.Add(vertGrid[vi + 2, vj + 1].texcoord);

		uv2.Add(vertGrid[vi, vj + 1].lmcoord);
		uv2.Add(vertGrid[vi + 1, vj + 1].lmcoord);
		uv2.Add(vertGrid[vi + 2, vj + 1].lmcoord);

		//Bottom row
		bverts.Add(vertGrid[vi, vj + 2].position);
		bverts.Add(vertGrid[vi + 1, vj + 2].position);
		bverts.Add(vertGrid[vi + 2, vj + 2].position);

		uv.Add(vertGrid[vi, vj + 2].texcoord);
		uv.Add(vertGrid[vi + 1, vj + 2].texcoord);
		uv.Add(vertGrid[vi + 2, vj + 2].texcoord);

		uv2.Add(vertGrid[vi, vj + 2].lmcoord);
		uv2.Add(vertGrid[vi + 1, vj + 2].lmcoord);
		uv2.Add(vertGrid[vi + 2, vj + 2].lmcoord);

		//Now that we have our control grid, it's business as usual
		Mesh bezMesh = new Mesh();
		bezMesh.name = "BSPfacemesh (bez)";
		BezierMesh bezPatch = new BezierMesh(GameManager.Instance.tessellations, bverts, uv, uv2);
		return bezPatch.Mesh;
	}

	public static void GeneratePolygonObject(Material material, params Face[] faces)
	{
		if (faces == null || faces.Length == 0)
		{
			Debug.LogWarning("Failed to create polygon object because there are no faces");
			return;
		}

		GameObject obj = new GameObject();
		obj.layer = GameManager.MapMeshesLayer;
		obj.name = "Mesh";
		obj.transform.SetParent(MapMeshes);
		// Our GeneratePolygonMesh will optimze and add the UVs for us
		CombineInstance[] combine = new CombineInstance[faces.Length];
		for (var i = 0; i < combine.Length; i++)
			combine[i].mesh = GeneratePolygonMesh(faces[i]);

		var mesh = new Mesh();
		mesh.CombineMeshes(combine, true, false, false);

		
		MeshRenderer mr = obj.AddComponent<MeshRenderer>();
		MeshFilter meshFilter = obj.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		//Don't use meshColliders		
		MeshCollider mc = obj.AddComponent<MeshCollider>();
		mc.sharedMesh = mesh;

		mr.sharedMaterial = material;
#if UNITY_EDITOR
		if (!Application.isPlaying)
			obj.isStatic = true;
		obj.hideFlags = HideFlags.DontSave;
#endif
	}

	public static Mesh GeneratePolygonMesh(Face face)
	{
		Mesh mesh = new Mesh();
		mesh.name = "BSPface (poly/mesh)";

		// Rip verts, uvs, and normals
		int vertexCount = face.n_vertexes;
		if (vertsCache.Capacity < vertexCount)
		{
			vertsCache.Capacity = vertexCount;
			uvCache.Capacity = vertexCount;
			uv2Cache.Capacity = vertexCount;
			normalsCache.Capacity = vertexCount;
		}

		if (indiciesCache.Capacity < face.n_meshverts)
			indiciesCache.Capacity = face.n_meshverts;

		vertsCache.Clear();
		uvCache.Clear();
		uv2Cache.Clear();
		normalsCache.Clear();
		indiciesCache.Clear();

		int vstep = face.vertex;
		for (int n = 0; n < face.n_vertexes; n++)
		{
			vertsCache.Add(MapLoader.verts[vstep].position);
			uvCache.Add(MapLoader.verts[vstep].texcoord);
			uv2Cache.Add(MapLoader.verts[vstep].lmcoord);
			normalsCache.Add(MapLoader.verts[vstep].normal);
			vstep++;
		}

		// Rip meshverts / triangles
		int mstep = face.meshvert;
		for (int n = 0; n < face.n_meshverts; n++)
		{
			indiciesCache.Add(MapLoader.meshVerts[mstep]);
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
}
