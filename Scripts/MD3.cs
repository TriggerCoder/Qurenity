using System.Collections;
using System.Collections.Generic;
using System.IO;
using Pathfinding.Ionic.Zip;
using UnityEngine;

public class MD3UnityConverted
{
	public GameObject go;
	public int numMeshes;
	public dataMeshes[] data;
	public struct dataMeshes
	{
		public MeshFilter meshFilter;
		public MeshRenderer meshRenderer;
	}
}
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
	public List<Mesh> readyMeshes = new List<Mesh>();				// This is the processed Unity Mesh
	public List<Material> readyMaterials = new List<Material>();		// This is the processed Material
	public static MD3 ImportModel(string modelName, bool forceSkinAlpha)
	{
		BinaryReader Md3ModelFile;
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

		
		md3Model.frames = new List<MD3Frame>();
		Md3ModelFile.BaseStream.Seek(ofsFrames, SeekOrigin.Begin);
		for (int i = 0; i < md3Model.numFrames * md3Model.numTags; i++)
		{
			MD3Frame frame = new MD3Frame();

			float x = Md3ModelFile.ReadSingle();
			float y = Md3ModelFile.ReadSingle();
			float z = Md3ModelFile.ReadSingle();
			frame.bb_Min = new Vector3(x, y, z);

			x = Md3ModelFile.ReadSingle();
			y = Md3ModelFile.ReadSingle();
			z = Md3ModelFile.ReadSingle();
			frame.bb_Max = new Vector3(x, y, z);

			frame.bs_Radius = Md3ModelFile.ReadSingle();

			x = Md3ModelFile.ReadSingle();
			y = Md3ModelFile.ReadSingle();
			z = Md3ModelFile.ReadSingle();
			frame.locOrigin = new Vector3(x, y, z);

			name = (new string(Md3ModelFile.ReadChars(16))).Split('\0');
			frame.name = name[0].Replace("\0", string.Empty);

			frame.QuakeToUnityCoordSystem();
			md3Model.frames.Add(frame);
		}

		md3Model.tags = new List<MD3Tag>();
		Md3ModelFile.BaseStream.Seek(ofsTags, SeekOrigin.Begin);
		for (int i = 0; i < md3Model.numFrames * md3Model.numTags; i++)
		{
			MD3Tag tag = new MD3Tag();
			name = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
			tag.name = name[0].Replace("\0", string.Empty);

			float x = Md3ModelFile.ReadSingle();
			float y = Md3ModelFile.ReadSingle();
			float z = Md3ModelFile.ReadSingle();
			tag.origin = new Vector3(x, y, z);

			float m00 = Md3ModelFile.ReadSingle();
			float m01 = Md3ModelFile.ReadSingle();
			float m02 = Md3ModelFile.ReadSingle();

			float m10 = Md3ModelFile.ReadSingle();
			float m11 = Md3ModelFile.ReadSingle();
			float m12 = Md3ModelFile.ReadSingle();

			float m20 = Md3ModelFile.ReadSingle();
			float m21 = Md3ModelFile.ReadSingle();
			float m22 = Md3ModelFile.ReadSingle();

			Vector4 column0 = new Vector4(m00, m10, m20, 0);
			Vector4 column1 = new Vector4(m01, m11, m21, 0);
			Vector4 column2 = new Vector4(m02, m12, m22, 0);
			Vector4 column3 = new Vector4(0, 0, 0, 1);

			tag.orientation = new Matrix4x4(-1 * column0, column2, -1 * column1, column3);
			tag.rotation = new Quaternion(m21, 0.0f, -m20, 1 + m22);
			tag.rotation.Normalize();

			tag.QuakeToUnityCoordSystem();
			md3Model.tags.Add(tag);
		}

		int offset = ofsMeshes;
		md3Model.meshes = new List<MD3Mesh>(md3Model.numMeshes);
		for (int i = 0; i < md3Model.numMeshes; i++)
		{
			Md3ModelFile.BaseStream.Seek(offset, SeekOrigin.Begin);
			MD3Mesh md3Mesh = new MD3Mesh();

			md3Mesh.parseMesh(i, md3Model.name, Md3ModelFile, offset, forceSkinAlpha);
			offset += md3Mesh.meshSize;
			md3Model.meshes.Add(md3Mesh);
		}
		return md3Model;
	}
}
public class MD3Frame
{
	public string name;					// The name of the frame
	public Vector3 bb_Min;				// The minimum bounds of the frame's bounding box
	public Vector3 bb_Max;				// The maximum bounds of the frame's bounding box
	public float bs_Radius;				// The radius of the frame's bounding sphere
	public Vector3 locOrigin;           // The local origin of the frame
	public void QuakeToUnityCoordSystem()
	{
		bb_Min = new Vector3(-bb_Min.x, bb_Min.z, -bb_Min.y);
		bb_Max = new Vector3(-bb_Max.x, bb_Max.z, -bb_Max.y);
		locOrigin = new Vector3(-locOrigin.x, locOrigin.z, -locOrigin.y);

		bb_Min.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
		bb_Max.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
		locOrigin.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));

		bs_Radius *= GameManager.sizeDividor;
	}
}
public class MD3Tag
{
	public string name;					// The name of the tag
	public Vector3 origin;              // The origin of the tag in 3D space
	public Matrix4x4 orientation;       // The orientation of the tag in 3D space
	public Quaternion rotation;         // The rotation of the tag in 3D space
	public void QuakeToUnityCoordSystem()
	{
		origin = new Vector3(-origin.x, origin.z, -origin.y);
		origin.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
	}
}
public class MD3Skin
{
	public string name;					// The name of the skin
	public int skinId;					// The index of the skin in the list of skins
	public MD3Skin(int skinId, string name)
	{
		this.skinId = skinId;
		this.name = name;
	}
}

public class MD3Mesh
{
	public string name;                 // The name of the surface
	public int meshNum;					// The index num of the mesh in the model
	public int meshId;					// The index of the mesh in the list of meshes
	public int flags;					// The flags associated with the surface
	public int numFrames;				// The number of frames in the surface
	public int numSkins;				// The number of skins in the surface
	public int numTriangles;			// The number of triangles in the surface
	public int numVertices;				// The number of vertexes in the surface
	public List<MD3Skin> skins;			// The list of shaders in the surface
	public List<MD3Triangle> triangles;	// The list of triangles in the surface
	public List<Vector3>[] verts;		// The list of vertexes in the surface
	public List<Vector2> texCoords;		// The texture coordinates of the vertex
	public int meshSize;                // This stores the total mesh size
	public void parseMesh(int MeshNum, string modelName, BinaryReader Md3ModelFile, int MeshOffset, bool forceSkinAlpha)
	{
		string[] fullName;
		meshNum = MeshNum;
		meshId = Md3ModelFile.ReadInt32();
		fullName = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
		name = fullName[0].Replace("\0", string.Empty);

//		Debug.Log("Loading Mesh: " + name + " , " + meshId);

		flags = Md3ModelFile.ReadInt32();
		numFrames = Md3ModelFile.ReadInt32();              // This stores the mesh aniamtion frame count
		numSkins = Md3ModelFile.ReadInt32();                    // This stores the mesh skin count
		numVertices = Md3ModelFile.ReadInt32();                // This stores the mesh vertex count
		numTriangles = Md3ModelFile.ReadInt32();               // This stores the mesh face count
		int ofsTriangles = Md3ModelFile.ReadInt32();                    // This stores the starting offset for the triangles
		int ofsSkins = Md3ModelFile.ReadInt32();                  // This stores the header size for the mesh
		int ofsTexCoords = Md3ModelFile.ReadInt32();                  // This stores the starting offset for the UV coordinates
		int ofsVerts = Md3ModelFile.ReadInt32();             // This stores the starting offset for the vertex indices
		meshSize = Md3ModelFile.ReadInt32();                   // This stores the total mesh size

		skins = new List<MD3Skin>();
		List<string> skinList = new List<string>();

		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsSkins, SeekOrigin.Begin);
		for (int i = 0; i < numSkins; i++)
		{
			fullName = (new string(Md3ModelFile.ReadChars(64))).Split('\0');
			string skinName = fullName[0].Replace("\0", string.Empty);
			//Need to strip extension
			fullName = skinName.Split('.');

			int num = Md3ModelFile.ReadInt32();

			//Some skins are mentioned more than once
			if (skinList.Contains(fullName[0]))
				continue;
			
			TextureLoader.AddNewTexture(fullName[0], forceSkinAlpha);

			skins.Add(new MD3Skin(num, fullName[0]));
			skinList.Add(fullName[0]);
		}
		//Update Number of skins as some are repeated
		numSkins = skins.Count;

		triangles = new List<MD3Triangle>();
		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsTriangles, SeekOrigin.Begin);
		for (int i = 0; i < numTriangles; i++)
		{
			int f0 = Md3ModelFile.ReadInt32();
			int f1 = Md3ModelFile.ReadInt32();
			int f2 = Md3ModelFile.ReadInt32();
			triangles.Add(new MD3Triangle(i, f0, f1, f2));
		}

		texCoords = new List<Vector2>();
		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsTexCoords, SeekOrigin.Begin);
		for (int i = 0; i < numVertices; i++)
		{
			float u = Md3ModelFile.ReadSingle();
			float v = Md3ModelFile.ReadSingle();
			texCoords.Add(new Vector2(u, 1 * -v));
		}

		verts = new List<Vector3>[numFrames];
		for (int i = 0; i < numFrames; i++)
			verts[i] = new List<Vector3>();

		Md3ModelFile.BaseStream.Seek(MeshOffset + ofsVerts, SeekOrigin.Begin);
		for (int i = 0, j = 0; i < numVertices * numFrames; i++)
		{
			float x = Md3ModelFile.ReadInt16() * GameManager.modelScale;
			float y = Md3ModelFile.ReadInt16() * GameManager.modelScale;
			float z = Md3ModelFile.ReadInt16() * GameManager.modelScale;
			float n1 = Md3ModelFile.ReadByte();
			float n2 = Md3ModelFile.ReadByte();

			Vector3 position = new Vector3(-x, z, -y);
			position.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
			verts[j].Add(position);

			if (((i + 1) % numVertices) == 0)
				j++;
		}
	}

}

// The indexes of the vertexes that make up the triangle
public class MD3Triangle
{
	public int triId;
	public int vertex1;
	public int vertex2;
	public int vertex3;
	public MD3Triangle(int triId, int vertex1, int vertex2, int vertex3)
	{
		this.triId = triId;
		this.vertex1 = vertex1;
		this.vertex2 = vertex2;
		this.vertex3 = vertex3;
	}
}