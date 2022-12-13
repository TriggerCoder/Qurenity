using System.Collections;
using System.Collections.Generic;
using System.IO;
using Pathfinding.Ionic.Zip;
using UnityEngine;

public class MD3
{
	public string name;                 // The name of the model
	public int flags;					// The model flags
	public int version;					// The version of the model
	public int numFrames;				// The number of frames in the model
	public int numTags;					// The number of tags in the model
	public int numMeshes;				// The number of meshes in the model
	public int numSkins;				// The number of skins in the model
	public List<MD3Frame> frames;		// The list of frames in the model
	public List<MD3Tag> tags;			// The list of tags in the model
	public List<MD3Mesh> meshes;		// The list of meshes in the model
	public List<MD3Skin> skins;			// The list of skins in the model
	public Vector3 bb_Min;				// The minimum bounds of the model's bounding box
	public Vector3 bb_Max;		         // The maximum bounds of the model's bounding box
	public float bs_Radius;				// The radius of the model's bounding sphere
	public Vector3 origin;				// The origin of the model
	public float scale;                 // The scale factor of the model

	public static MD3 ImportModel(string modelName)
	{
		BinaryReader Md3ModelFile;
		byte[] modelBytes;
		string[] name;

		string path = Application.streamingAssetsPath + "/models/" + modelName + ".md3";
		if (File.Exists(path))
			Md3ModelFile = new BinaryReader(File.Open(path, FileMode.Open));
		else if (PakManager.ZipFiles.ContainsKey(path = ("models/" + modelName + ".md3").ToUpper()))
		{
			string FileName = PakManager.ZipFiles[path];
			FileStream stream = File.Open(FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			ZipFile zip = ZipFile.Read(stream);
			ZipEntry map = zip[path];
			MemoryStream ms = new MemoryStream();
			map.Extract(ms);
			Md3ModelFile = new BinaryReader(ms);
			modelBytes = PakManager.ZipToByteArray(path, ref zip);
		}
		else
			return null;

		Md3ModelFile.BaseStream.Seek(0, SeekOrigin.Begin);
		string header = new string(Md3ModelFile.ReadChars(4)); //4 IDP3
		if (header != "IDP3")
		{
			Debug.LogError(modelName + " not a md3 model");
			return null;
		}

		MD3 md3Model = new MD3();
//		GameObject ObjectRoot = new GameObject(nm);
//		MD3Model model = (MD3Model)ObjectRoot.AddComponent(typeof(MD3Model));
//		model.setup();

		md3Model.version = Md3ModelFile.ReadInt32();

		name = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
		md3Model.name = name[0].Replace("\0", string.Empty);

		md3Model.flags = Md3ModelFile.ReadInt32();
		md3Model.numFrames = Md3ModelFile.ReadInt32();
		md3Model.numTags = Md3ModelFile.ReadInt32();
		md3Model.numMeshes = Md3ModelFile.ReadInt32();
		md3Model.numSkins = Md3ModelFile.ReadInt32();

		int ofsFrames = Md3ModelFile.ReadInt32();
		int ofsTags = Md3ModelFile.ReadInt32();
		int ofsMeshes = Md3ModelFile.ReadInt32();
		int fileSize = Md3ModelFile.ReadInt32();

/*
		List<GameObject> tags = new List<GameObject>();
		GameObject nodeTag = new GameObject("tags");
		nodeTag.transform.parent = model.transform;
		model.maxFrames = numBoneFrames;
*/
		md3Model.tags = new List<MD3Tag>();
		Md3ModelFile.BaseStream.Seek(ofsTags, SeekOrigin.Begin);
		for (int i = 0; i < md3Model.numFrames * md3Model.numTags; i++)
		{
			MD3Tag tag = new MD3Tag();
			name = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
			tag.name = name[0].Replace("\0", string.Empty);
			//	Debug.LogWarning("Tag :"+tag.name);

			float x = Md3ModelFile.ReadSingle();
			float z = Md3ModelFile.ReadSingle();
			float y = Md3ModelFile.ReadSingle();
			tag.origin = new Vector3(x, y, z);

			float matrix0 = Md3ModelFile.ReadSingle();
			float matrix1 = Md3ModelFile.ReadSingle();
			float matrix2 = Md3ModelFile.ReadSingle();

			float matrix3 = Md3ModelFile.ReadSingle();
			float matrix4 = Md3ModelFile.ReadSingle();
			float matrix5 = Md3ModelFile.ReadSingle();

			float ay = Md3ModelFile.ReadSingle();
			float ax = Md3ModelFile.ReadSingle();
			float az = Md3ModelFile.ReadSingle();

			Vector4 column0 = new Vector4(matrix0, matrix3, ay, 0);
			Vector4 column1 = new Vector4(matrix1, matrix4, ax, 0);
			Vector4 column2 = new Vector4(matrix2, matrix5, az, 0);
			Vector4 column3 = new Vector4(0, 0, 0, 1);

			tag.orientation = new Matrix4x4(column0, column1, column2, column3);
			tag.rotation = new Quaternion(ax, 0.0f, -ay, 1 + az);
			tag.rotation.Normalize();
/*
			GameObject node = new GameObject(name);
			node.transform.position = position;
			node.transform.rotation = rotation;
			node.transform.parent = nodeTag.transform;
			tags.Add(node);
			model.tags.Add(node.transform);
*/		
			md3Model.tags.Add(tag);
		}

/*
		//    Debug.LogWarning("create bones frm tags");
		GameObject nodeBone = new GameObject("Bones");
		//    GameObject nodeBone = GameObject.CreatePrimitive(PrimitiveType.Cube);
		nodeBone.transform.parent = model.transform;
		model.numTags = numTags;

		for (int i = 0; i < numTags; i++)
		{
			GameObject tag = tags[0 * numTags + i];
			GameObject node = new GameObject();
			// GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			node.name = tag.name;
			node.transform.parent = nodeBone.transform;
			node.transform.position = tag.transform.position;
			node.transform.rotation = tag.transform.rotation;
			model.bones.Add(node);
		}
*/

/*
		//    Debug.LogWarning("read meshes");

		GameObject meshContainer = new GameObject("MeshContainer");
		meshContainer.transform.parent = ObjectRoot.transform;
*/

		int offset = ofsMeshes;
		md3Model.meshes = new List<MD3Mesh>(md3Model.numMeshes);
		for (int i = 0; i < md3Model.numMeshes; i++)
		{
			Md3ModelFile.BaseStream.Seek(offset, SeekOrigin.Begin);
			MD3Mesh md3Mesh = new MD3Mesh();

			md3Mesh.parseMesh(md3Model.name, Md3ModelFile, offset, md3Model);
			offset += md3Mesh.meshSize;
			md3Model.meshes.Add(md3Mesh);
		}

		Debug.LogWarning("read ok");
		return md3Model;
	}
}
public class MD3Frame
{
	public string name;					// The name of the frame
	public Vector3 bb_Min;				// The minimum bounds of the frame's bounding box
	public Vector3 bb_Max;				// The maximum bounds of the frame's bounding box
	public float bs_Radius;				// The radius of the frame's bounding sphere
	public Matrix4x4 locCoordSys;		// The local coordinate system of the frame
}
public class MD3Tag
{
	public string name;					// The name of the tag
	public Vector3 origin;              // The origin of the tag in 3D space
	public Matrix4x4 orientation;       // The orientation of the tag in 3D space
	public Quaternion rotation;			// The rotation of the tag in 3D space
}
public class MD3Skin
{
	public string name;					// The name of the skin
	public int skinId;					// The index of the skin in the list of skins
	public Texture2D texture;			// The texture map associated with the skin
}

public class MD3Mesh
{
	public string name;					// The name of the surface
	public int meshId;					// The index of the mesh in the list of meshes
	public int flags;					// The flags associated with the surface
	public int numFrames;				// The number of frames in the surface
	public int numSkins;				// The number of skins in the surface
	public int numTriangles;			// The number of triangles in the surface
	public int numVertices;				// The number of vertexes in the surface
	public List<MD3Skin> skins;			// The list of shaders in the surface
	public List<MD3Triangle> triangles;	// The list of triangles in the surface
	public List<Vector3> verts;         // The list of vertexes in the surface
	public List<Vector2> texCoords;		// The texture coordinates of the vertex
	public int meshSize;				// This stores the total mesh size

	public void parseMesh(string modelName, BinaryReader Md3ModelFile, int MeshOffset, MD3 model)
	{
		meshId = Md3ModelFile.ReadInt32();
		name = new string(Md3ModelFile.ReadChars(68));//68;				// This stores the mesh name (We do care)
		name = name.Replace("\0", string.Empty);
		Debug.LogWarning("read mesh:" + name + "," + meshId);
/*
		GameObject obj = new GameObject("Obj_Mesh");
		obj.transform.parent = meshContainer.transform;

		MD3Body mesh = (MD3Body)obj.AddComponent(typeof(MD3Body));
		mesh.name = "Mesh_" + model.bodyFrames.Count;
		mesh.setup();


		mesh.meshRenderer.sharedMaterial = new Material(Shader.Find("Diffuse"));
		mesh.meshRenderer.sharedMaterial.color = Color.white;
*/

		numFrames = Md3ModelFile.ReadInt32();              // This stores the mesh aniamtion frame count
		numSkins = Md3ModelFile.ReadInt32();                    // This stores the mesh skin count
		numVertices = Md3ModelFile.ReadInt32();                // This stores the mesh vertex count
		numTriangles = Md3ModelFile.ReadInt32();               // This stores the mesh face count
		int triStart = Md3ModelFile.ReadInt32();                    // This stores the starting offset for the triangles
		int headerSize = Md3ModelFile.ReadInt32();                  // This stores the header size for the mesh
		int TexVectorStart = Md3ModelFile.ReadInt32();                  // This stores the starting offset for the UV coordinates
		int vertexStart = Md3ModelFile.ReadInt32();             // This stores the starting offset for the vertex indices
		meshSize = Md3ModelFile.ReadInt32();                   // This stores the total mesh size

		//   Debug.LogWarning("num mesh frames"+ mesh.numMeshFrames);
		//   Debug.LogWarning("num mesh vertex" + mesh.numVertices);
		//   Debug.LogWarning("num mesh tris" + mesh.numTriangles);


		//   Debug.LogWarning("read triangles");

		triangles = new List<MD3Triangle>();
		Md3ModelFile.BaseStream.Seek(MeshOffset + triStart, SeekOrigin.Begin);
		for (int i = 0; i < numTriangles; i++)
		{
			int f0 = Md3ModelFile.ReadInt32();
			int f1 = Md3ModelFile.ReadInt32();
			int f2 = Md3ModelFile.ReadInt32();
			triangles.Add(new MD3Triangle(f0, f1, f2));
		}
		//  Debug.LogWarning("read text coord");

		texCoords = new List<Vector2>();
		Md3ModelFile.BaseStream.Seek(MeshOffset + TexVectorStart, SeekOrigin.Begin);
		for (int i = 0; i < numVertices; i++)
		{
			float u = Md3ModelFile.ReadSingle();
			float v = Md3ModelFile.ReadSingle();
			texCoords.Add(new Vector2(u, 1 * -v));
		}

		//   Debug.LogWarning("red vertices" + mesh.numVertices * mesh.numMeshFrames);
		verts = new List<Vector3>();
		Md3ModelFile.BaseStream.Seek(MeshOffset + vertexStart, SeekOrigin.Begin);
		for (int i = 0; i < numVertices * numFrames; i++)
		{
			float x = Md3ModelFile.ReadInt16() / 64.0f;
			float z = Md3ModelFile.ReadInt16() / 64.0f;
			float y = Md3ModelFile.ReadInt16() / 64.0f;
			float n1 = Md3ModelFile.ReadByte() / 255.0f;
			float n2 = Md3ModelFile.ReadByte() / 255.0f;
			verts.Add(new Vector3(x, y, z));
		}


		//  Debug.LogWarning("build mesh");
//		mesh.buildFrames();
		//   Debug.LogWarning("add mesh to body");
//		model.bodyFrames.Add(mesh);
	}
	/*
	public void buildFrames()
	{

		int uv_count = texCoords.Count;
		Surface surf = new Surface("SURFACE");

		try
		{


			//     Debug.LogWarning("faces total:" + Faces.Count);
			//     Debug.LogWarning("vertex total:" + Vertex.Count);


			for (int i = 0; i < Faces.Count; i++)
			{

				int i1 = Faces[i].v0;
				int i2 = Faces[i].v1;
				int i3 = Faces[i].v2;

				Vector3 v1 = Vertex[i1];
				Vector3 v2 = Vertex[i2];
				Vector3 v3 = Vertex[i3];

				Vector2 uv1 = TexCoords[0 * uv_count + i1];
				Vector2 uv2 = TexCoords[0 * uv_count + i2];
				Vector2 uv3 = TexCoords[0 * uv_count + i3];
				surf.addFace(v1, v2, v3, uv1, uv2, uv3);

			}

			surf.build();
			surf.Optimize();
			surf.RecalculateNormals();
			surf.CulateTangents();
			frame = surf.getMesh();
			meshFilter.sharedMesh = frame;

		}
		catch (ArgumentOutOfRangeException outOfRange)
		{

			Debug.LogError("Error mesh build:" + outOfRange.Message);
		}


	}
	*/
}

// The indexes of the vertexes that make up the triangle
public class MD3Triangle
{
	public int vertex1;
	public int vertex2;
	public int vertex3;
	public MD3Triangle(int vertex1, int vertex2, int vertex3)
	{
		this.vertex1 = vertex1;
		this.vertex2 = vertex2;
		this.vertex3 = vertex3;
	}
}