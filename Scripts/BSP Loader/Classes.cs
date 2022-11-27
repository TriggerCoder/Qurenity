using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Plane
{
	public Vector3 normal;
	public float dist;

	public Plane(Vector3 normal, float distance)
	{
		this.normal = normal;
		this.dist = distance * GameManager.sizeDividor;
	}
}
public struct Vertex
{
	public Vector3 position;
	public Vector3 normal;
	public byte[] color;

	// These are texture coords, or UVs
	public Vector2 texcoord;
	public Vector2 lmcoord;

	public Vertex(Vector3 position, float texX, float texY, float lmX, float lmY, Vector3 normal, byte[] color)
	{
		this.position = position;
		this.normal = normal;

		// Color data doesn't get used
		this.color = color;

		// Invert the texture coords, to account for
		// the difference in the way Unity and Quake3
		// handle them.
		texcoord.x = texX;
		texcoord.y = -texY;

		// Lightmaps aren't used for now, but store the
		// data for them anyway.  Inverted, same as above.
		lmcoord.x = lmX;
		lmcoord.y = lmY;

		QuakeToUnityCoordSystem();
	}

	private void QuakeToUnityCoordSystem()
	{
		float tempz = position.z;
		float tempy = position.y;
		position.y = tempz;
		position.z = -tempy;
		position.x = -position.x;

		tempz = normal.z;
		tempy = normal.y;

		normal.y = tempz;
		normal.z = -tempy;
		normal.x = -normal.x;

		// Quake3 also uses an odd scale where 0.03 units is about 1 meter, so scale it down
		position.Scale(new Vector3(GameManager.sizeDividor, GameManager.sizeDividor, GameManager.sizeDividor));
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
	public const short LeafBushes = 6;
	public const short Models = 7;
	public const short Brushes = 8;
	public const short BrushSides = 9;
	public const short Vertexes = 10;
	public const short MeshVerts = 11;
	public const short Effects = 12;
	public const short Faces = 13;
	public const short LightMaps = 14;
	public const short LightVols = 15;
	public const short VisData = 16;
}

public struct Face
{
	// The fields in this class are kind of obtuse.  I recommend looking up the Q3 .bsp map spec for full understanding.

	public int texture;
	public int effect;
	public int type;
	public int vertex;
	public int n_vertexes;
	public int meshvert;
	public int n_meshverts;
	public int lm_index;
	public int[] lm_start;
	public int[] lm_size;
	public Vector3 lm_origin;
	public Vector3[] lm_vecs;
	public Vector3 normal;
	public int[] size;

	public Face(int texture, int effect, int type, int vertex, int n_vertexes, int meshvert, int n_meshverts,
		int lm_index, int[] lm_start, int[] lm_size, Vector3 lm_origin, Vector3[] lm_vecs, Vector3 normal,
		int[] size)
	{
		this.texture = texture;
		this.effect = effect;
		this.type = type;
		this.vertex = vertex;
		this.n_vertexes = n_vertexes;
		this.meshvert = meshvert;
		this.n_meshverts = n_meshverts;
		this.lm_index = lm_index;
		this.lm_start = lm_start;
		this.lm_size = lm_size;
		this.lm_origin = lm_origin;
		this.lm_vecs = lm_vecs;
		this.normal = normal;
		this.size = size;
	}
}