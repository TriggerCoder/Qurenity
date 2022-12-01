using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
	public int plane;					// The index into the planes array 
	public int front;					// The child index for the front node 
	public int back;					// The child index for the back node 
	public Vector3 bb_Min;				// The bounding box min position. 
	public Vector3 bb_Max;              // The bounding box max position.
	public Node(int plane, int front, int back, Vector3Int bb_Min, Vector3Int bb_Max)
	{
		this.plane = plane;
		this.front = front;
		this.back = back;
		this.front = front;
		this.bb_Min = Vector3.zero;
		this.bb_Max = Vector3.zero;
		this.bb_Min = QuakeToUnityCoordSystem(bb_Min);
		this.bb_Max = QuakeToUnityCoordSystem(bb_Max);
	}
	private Vector3 QuakeToUnityCoordSystem(Vector3Int vect3i)
	{
		Vector3 vect3 = new Vector3(-vect3i.x, vect3i.z, -vect3i.y);
		vect3.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
		return vect3;
	}
}
public class Plane3D
{
	public const float EPSILON = 0.00001f;

	public Vector3 normal;              // Plane normal. 
	public float distance;              // The plane distance from origin
	public Plane3D(Vector3 normal, float distance)
	{
		this.normal = normal;
		this.distance = distance;
		QuakeToUnityCoordSystem();
	}
	private void QuakeToUnityCoordSystem()
	{
		normal = new Vector3(-normal.x, normal.z, -normal.y);
		distance *= GameManager.sizeDividor;
	}
	public bool GetSide(Vector3 vect, CheckPointPlane check = CheckPointPlane.IsOnOrFront)
	{
		float d = Vector3.Dot(normal, vect) - distance;
		switch(check)
		{
			default:
			case CheckPointPlane.IsOnOrFront:
				return (d >= 0);
			break;
			case CheckPointPlane.IsFront:
				return (d > EPSILON);
			break;
			case CheckPointPlane.IsOn:
				return (d == 0);
			break;
		}
	}

	public List<float> IntersectPlanes(Plane3D p2, Plane3D p3)
	{
		Vector3 m1 = new Vector3(normal.x, p2.normal.x, p3.normal.x);
		Vector3 m2 = new Vector3(normal.y, p2.normal.y, p3.normal.y);
		Vector3 m3 = new Vector3(normal.z, p2.normal.z, p3.normal.z);
		Vector3 d = new Vector3(distance, p2.distance, p3.distance);

		Vector3 u = Vector3.Cross(m2, m3);
		Vector3 v = Vector3.Cross(m1, d);

		float denom = Vector3.Dot(m1, u);
		// Planes don't actually intersect in a point
		if (Mathf.Abs(denom) < Mathf.Epsilon)
			return null;

		List<float> intersectPoint = new List<float>(3);
		intersectPoint.Add((Vector3.Dot(d, u) / denom));
		intersectPoint.Add((Vector3.Dot(m3, v) / denom));
		intersectPoint.Add((-Vector3.Dot(m2, v) / denom));

		return intersectPoint;
	}

	public enum CheckPointPlane
	{		
		IsOnOrFront,
		IsFront,
		IsOn
	}
}
public class Leaf
{
	public int cluster;					// The visibility cluster 
	public int area;					// The area portal 
	public Vector3 bb_Min;				// The bounding box min position 
	public Vector3 bb_Max;				// The bounding box max position 
	public int leafFace;				// The first index into the face array 
	public int numOfLeafFaces;			// The number of faces for this leaf 
	public int leafBrush;				// The first index for into the brushes 
	public int numOfLeafBrushes;        // The number of brushes for this leaf
	public Leaf(int cluster, int area, Vector3Int bb_Min, Vector3Int bb_Max, int leafFace, int numOfLeafFaces, 
				int leafBrush, int numOfLeafBrushes)
	{
		this.cluster = cluster;
		this.area = area;
		this.leafFace = leafFace;
		this.numOfLeafFaces = numOfLeafFaces;
		this.leafBrush = leafBrush;
		this.numOfLeafBrushes = numOfLeafBrushes;
		this.bb_Min = Vector3.zero;
		this.bb_Max = Vector3.zero;
		this.bb_Min = QuakeToUnityCoordSystem(bb_Min);
		this.bb_Max = QuakeToUnityCoordSystem(bb_Max);
	}
	private Vector3 QuakeToUnityCoordSystem(Vector3Int vect3i)
	{
		Vector3 vect3 = new Vector3(-vect3i.x, vect3i.z, -vect3i.y);
		vect3.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
		return vect3;
	}
};

public class Brush
{
	public int brushSide;              // The starting brush side for the brush 
	public int numOfBrushSides;        // Number of brush sides for the brush
	public int textureID;              // The texture index for the brush
	public Brush(int brushSide, int numOfBrushSides, int textureID)
	{
		this.brushSide = brushSide;
		this.numOfBrushSides = numOfBrushSides;
		this.textureID = textureID;
	}
};

public class BrushSide
{
	public int plane;                  // The plane index
	public int textureID;              // The texture index
	public BrushSide(int plane, int textureID)
	{
		this.plane = plane;
		this.textureID = textureID;
	}
};

public class VisData
{
	public int numOfClusters;			// The number of clusters
	public int bytesPerCluster;			// The amount of bytes (8 bits) in the cluster's bitset
	public byte[] bitSets;              // The array of bytes that holds the cluster bitsets

	public VisData(int numOfClusters, int bytesPerCluster)
	{
		this.numOfClusters = numOfClusters;
		this.bytesPerCluster = bytesPerCluster;
	}
};
public class Vertex
{
	public Vector3 position;			// (x, y, z) position. 
	public Vector2 textureCoord;		// (u, v) texture coordinate
	public Vector2 lightmapCoord;		// (u, v) lightmap coordinate
	public Vector3 normal;				// (x, y, z) normal vector
	public byte[] color;				// RGBA color for the vertex 

	public Vertex(Vector3 position, float texX, float texY, float lmX, float lmY, Vector3 normal, byte[] color)
	{
		this.position = position;
		this.normal = normal;

		// Color data doesn't get used
		this.color = color;

		// Invert the texture coords, to account for
		// the difference in the way Unity and Quake3
		// handle them.
		textureCoord.x = texX;
		textureCoord.y = -texY;

		// Lightmaps are created dynamically
		lightmapCoord.x = lmX;
		lightmapCoord.y = lmY;

		QuakeToUnityCoordSystem();
	}

	private void QuakeToUnityCoordSystem()
	{
		position = new Vector3(-position.x, position.z, -position.y);
		normal = new Vector3(-normal.x, normal.z, -normal.y);

		position.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
	}
}
public class Face
{
	public int faceId;                  // The index of this face
	public int textureID;               // The index into the texture array 
	public int effect;                  // The index for the effects (or -1 = n/a) 
	public int type;                    // 1=polygon, 2=patch, 3=mesh, 4=billboard 
	public int startVertIndex;          // The starting index into this face's first vertex 
	public int numOfVerts;              // The number of vertices for this face 
	public int startIndex;              // The starting index into the indices array for this face
	public int numOfIndices;            // The number of indices for this face
	public int lightMapID;              // The texture index for the lightmap 
	public int[] lm_Corner;             // The face's lightmap corner in the image 
	public int[] lm_Size;               // The size of the lightmap section 
	public Vector3 lm_Origin;           // The 3D origin of lightmap. 
	public Vector3[] lm_vecs;           // The 3D space for s and t unit vectors. 
	public Vector3 normal;              // The face normal. 
	public int[] size;                  // The bezier patch dimensions. 

	public Face(int faceId, int textureID, int effect, int type, int startVertIndex, int numOfVerts, int startIndex, int numOfIndices,
		int lightMapID, int[] lm_Corner, int[] lm_Size, Vector3 lm_Origin, Vector3[] lm_vecs, Vector3 normal,
		int[] size)
	{
		this.faceId = faceId;
		this.textureID = textureID;
		this.effect = effect;
		this.type = type;
		this.startVertIndex = startVertIndex;
		this.numOfVerts = numOfVerts;
		this.startIndex = startIndex;
		this.numOfIndices = numOfIndices;
		this.lightMapID = lightMapID;
		this.lm_Corner = lm_Corner;
		this.lm_Size = lm_Size;
		this.lm_Origin = lm_Origin;
		this.lm_vecs = lm_vecs;
		this.normal = normal;
		this.size = size;
	}
}
public class FaceType
{
	public const short None = 0;
	public const short Polygon = 1;
	public const short Patch = 2;
	public const short Mesh = 3;
	public const short Billboard = 4;
}

public class LumpType
{
	public const short Entities = 0;
	public const short Textures = 1;
	public const short Planes = 2;
	public const short Nodes = 3;
	public const short Leafs = 4;
	public const short LeafFaces = 5;
	public const short LeafBrushes = 6;
	public const short Models = 7;
	public const short Brushes = 8;
	public const short BrushSides = 9;
	public const short Vertexes = 10;
	public const short VertIndices = 11;
	public const short Effects = 12;
	public const short Faces = 13;
	public const short LightMaps = 14;
	public const short LightVols = 15;
	public const short VisData = 16;
}
