using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QShader
{

	public string name;					// The name of the texture w/o the extension 
	public uint surfaceFlags;            // The surface flags
	public uint contentsFlags;			// The content flags
	public QShader(string name, uint surfaceFlags, uint contentsFlags)
	{
		//The string is read as 64 characters, which includes a bunch of null bytes.  We strip them to avoid oddness when printing and using the texture names.
		this.name = name.Replace("\0", string.Empty);
		this.surfaceFlags = surfaceFlags;
		this.contentsFlags = contentsFlags;

		// Remove some common shader modifiers to get normal
		// textures instead. This is kind of a hack, and could
		// bit you if a texture just happens to have any of these
		// in its name but isn't actually a shader texture.
		this.name = this.name.Replace("_hell", string.Empty);
		this.name = this.name.Replace("_trans", string.Empty);
		this.name = this.name.Replace("flat_400", string.Empty);
		this.name = this.name.Replace("_750", string.Empty);
	}
}
public class QNode
{
	public int plane;					// The index into the planes array 
	public int front;					// The child index for the front node 
	public int back;					// The child index for the back node 
	public Vector3 bb_Min;				// The bounding box min position. 
	public Vector3 bb_Max;              // The bounding box max position.
	public QNode(int plane, int front, int back, Vector3Int bb_Min, Vector3Int bb_Max)
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
public class QPlane
{
	public const float EPSILON = 0.00001f;

	public Vector3 normal;              // Plane normal. 
	public float distance;              // The plane distance from origin
	public QPlane(Vector3 normal, float distance)
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

	public List<float> IntersectPlanes(QPlane p2, QPlane p3)
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
public class QLeaf
{
	public int cluster;					// The visibility cluster 
	public int area;					// The area portal 
	public Vector3 bb_Min;				// The bounding box min position 
	public Vector3 bb_Max;				// The bounding box max position 
	public int leafSurface;				// The first index into the surface array 
	public int numOfLeafFaces;			// The number of faces for this leaf 
	public int leafBrush;				// The first index for into the brushes 
	public int numOfLeafBrushes;        // The number of brushes for this leaf
	public QLeaf(int cluster, int area, Vector3Int bb_Min, Vector3Int bb_Max, int leafSurface, int numOfLeafFaces, 
				int leafBrush, int numOfLeafBrushes)
	{
		this.cluster = cluster;
		this.area = area;
		this.leafSurface = leafSurface;
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

public class QBrush
{
	public int brushSide;              // The starting brush side for the brush 
	public int numOfBrushSides;        // Number of brush sides for the brush
	public int shaderId;              // The shader index for the brush
	public QBrush(int brushSide, int numOfBrushSides, int shaderId)
	{
		this.brushSide = brushSide;
		this.numOfBrushSides = numOfBrushSides;
		this.shaderId = shaderId;
	}
};

public class QBrushSide
{
	public int plane;                  // The plane index
	public int shaderId;              // The shader index
	public QBrushSide(int plane, int shaderId)
	{
		this.plane = plane;
		this.shaderId = shaderId;
	}
};

public class QVisData
{
	public int numOfClusters;			// The number of clusters
	public int bytesPerCluster;			// The amount of bytes (8 bits) in the cluster's bitset
	public byte[] bitSets;              // The array of bytes that holds the cluster bitsets

	public QVisData(int numOfClusters, int bytesPerCluster)
	{
		this.numOfClusters = numOfClusters;
		this.bytesPerCluster = bytesPerCluster;
	}
};
public class QVertex
{
	public int vertId;					// The index of this vertex
	public Vector3 position;			// (x, y, z) position. 
	public Vector2 textureCoord;		// (u, v) texture coordinate
	public Vector2 lightmapCoord;		// (u, v) lightmap coordinate
	public Vector3 normal;				// (x, y, z) normal vector
	public Color color;				// RGBA color for the vertex 

	public QVertex(int vertId, Vector3 position, float texX, float texY, float lmX, float lmY, Vector3 normal, byte[] color)
	{
		this.vertId = vertId;
		this.position = position;
		this.normal = normal;

		this.color = new Color32(color[0], color[1], color[2], color[3]);

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
public class QSurface
{
	public int surfaceId;               // The index of this surface
	public int shaderId;				// The index into the shader array 
	public int effect;                  // The index for the effects (or -1 = n/a) 
	public int type;                    // 1=polygon, 2=patch, 3=mesh, 4=billboard 
	public int startVertIndex;          // The starting index into this surface's first vertex 
	public int numOfVerts;              // The number of vertices for this surface 
	public int startIndex;              // The starting index into the indices array for this surface
	public int numOfIndices;            // The number of indices for this surface
	public int lightMapID;              // The texture index for the lightmap 
	public int[] lm_Corner;             // The surface's lightmap corner in the image 
	public int[] lm_Size;               // The size of the lightmap section 
	public Vector3 lm_Origin;           // The 3D origin of lightmap. 
	public Vector3[] lm_vecs;           // The 3D space for s and t unit vectors. 
	public Vector3 normal;              // The surface normal. 
	public int[] size;                  // The bezier patch dimensions. 

	public QSurface(int surfaceId, int shaderId, int effect, int type, int startVertIndex, int numOfVerts, int startIndex, int numOfIndices,
		int lightMapID, int[] lm_Corner, int[] lm_Size, Vector3 lm_Origin, Vector3[] lm_vecs, Vector3 normal,
		int[] size)
	{
		this.surfaceId = surfaceId;
		this.shaderId = shaderId;
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

		QuakeToUnityCoordSystem();
	}
	private void QuakeToUnityCoordSystem()
	{
		lm_Origin = new Vector3(-lm_Origin.x, lm_Origin.z, -lm_Origin.y);
		normal = new Vector3(-normal.x, normal.z, -normal.y);

		lm_Origin.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
	}
}
public class QSurfaceType
{
	public const short None = 0;
	public const short Polygon = 1;
	public const short Patch = 2;
	public const short Mesh = 3;
	public const short Billboard = 4;
}

public class LumpType
{
	public const short Entities		= 0;
	public const short Shaders		= 1;
	public const short Planes		= 2;
	public const short Nodes		= 3;
	public const short Leafs		= 4;
	public const short LeafSurfaces = 5;
	public const short LeafBrushes	= 6;
	public const short Models		= 7;
	public const short Brushes		= 8;
	public const short BrushSides	= 9;
	public const short Vertexes		= 10;
	public const short VertIndices	= 11;
	public const short Effects		= 12;
	public const short Surfaces		= 13;
	public const short LightMaps	= 14;
	public const short LightGrid	= 15;
	public const short VisData		= 16;
}

public class ContentFlags
{
	public const uint Solid			= 0x000001;		// Blocking surface
	public const uint Lava			= 0x000008;		// Block and lava effects
	public const uint Slime			= 0x000010;     // Block and slime effects
	public const uint Water			= 0x000020;     // Non Blocking change physics
	public const uint Fog			= 0x000040;     // Non Blocking Fog effect
	public const uint AreaPortal	= 0x008000;		// Trigger Teleporter
	public const uint PlayerClip	= 0x010000;      
	public const uint MonsterClip	= 0x020000;
	public const uint Teleporter	= 0x040000;		// Bots info
	public const uint JumpPad		= 0x080000;		// Jump Pad
	public const uint ClusterPortal	= 0x100000;		// Bots info
	public const uint BotsNotEnter	= 0x200000;     // Restricter area for Bots
	public const uint Origin		= 0x1000000;
	public const uint Body			= 0x2000000;    // Never on BSP
	public const uint Corpse		= 0x4000000;    
	public const uint Details		= 0x8000000;   // Not used on BSP
	public const uint Structural	= 0x10000000;  // Used on BSP 
	public const uint Translucent	= 0x20000000;
	public const uint Trigger		= 0x40000000;
	public const uint NoDrop		= 0x80000000;	//Leave not bodies or items
}
public class SurfaceFlags
{
	public const int NoDamage		= 0x00001;		// Don't give falling damage
	public const int Slick			= 0x00002;      // Affects game physics
	public const int Sky			= 0x00004;      // Lighting from environment map
	public const int Ladder			= 0x00008;		// Surface is climbable
	public const int NoImpact		= 0x00010;      // No missile explosions
	public const int NoMarks		= 0x00020;		// No missile marks
	public const int Flesh			= 0x00040;      // Flesh sounds and effects
	public const int NoDraw			= 0x00080;		// Don't generate a drawsurface at all
	public const int Hint			= 0x00100;
	public const int Skip			= 0x00200;      // Ignore, allowing non-closed brushes
	public const int NoLightMap		= 0x00400;      // Don't add lightmap to surface
	public const int PointLight		= 0x00800;      // Generate lighting info at verts
	public const int MetalSteps		= 0x01000;		// Metal sounds and effects
	public const int NoSteps		= 0x02000;		// No step sounds
	public const int NonSolid		= 0x04000;		// No Collision
	public const int LightFilter	= 0x08000;      // Act as a light filter during map compiling
	public const int AlphaShadow	= 0x10000;		// Map compiling do per-pixel light shadow casting 
	public const int NoDynLight		= 0x20000;      // Don't add dynamic lights
}