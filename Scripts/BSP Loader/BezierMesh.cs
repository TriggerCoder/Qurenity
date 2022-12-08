using System.Collections.Generic;
using UnityEngine;

public class BezierMesh
{
    private static List<Vector3> vertexCache = new List<Vector3>();
    private static List<Vector2> uvCache = new List<Vector2>();
    private static List<Vector2> uv2Cache = new List<Vector2>();
	private static List<Color> colorCache = new List<Color>();
	private static List<int> indiciesCache = new List<int>();

    static private List<Vector3> p0sCache = new List<Vector3>();
    static private List<Vector2> p0suvCache = new List<Vector2>();
    static private List<Vector2> p0suv2Cache = new List<Vector2>();
	static private List<Color> p0scolorCache = new List<Color>();

	static private List<Vector3> p1sCache = new List<Vector3>();
    static private List<Vector2> p1suvCache = new List<Vector2>();
    static private List<Vector2> p1suv2Cache = new List<Vector2>();
	static private List<Color> p1scolorCache = new List<Color>();

	static private List<Vector3> p2sCache = new List<Vector3>();
    static private List<Vector2> p2suvCache = new List<Vector2>();
    static private List<Vector2> p2suv2Cache = new List<Vector2>();
	static private List<Color> p2scolorCache = new List<Color>();

	public static void ClearCaches()
    {
        vertexCache = new List<Vector3>();
        uvCache = new List<Vector2>();
        uv2Cache = new List<Vector2>();
        indiciesCache = new List<int>();
		colorCache = new List<Color>();

		p0sCache = new List<Vector3>();
        p0suvCache = new List<Vector2>();
        p0suv2Cache = new List<Vector2>();
		p0scolorCache = new List<Color>();

		p1sCache = new List<Vector3>();
        p1suvCache = new List<Vector2>();
        p1suv2Cache = new List<Vector2>();
		p1scolorCache = new List<Color>();

		p2sCache = new List<Vector3>();
        p2suvCache = new List<Vector2>();
        p2suv2Cache = new List<Vector2>();
		p2scolorCache = new List<Color>();
	}

    // Where the magic happens.
    public BezierMesh(int level, int patchNumber, List<Vector3> control, List<Vector2> controlUvs, List<Vector2> controlUv2s, List<Color> controlColor)
	{
		// The mesh we're building
		Mesh patchMesh = new Mesh();
		patchMesh.name = "Bezier_Patch_" + patchNumber;

		// We'll use these two to hold our verts, tris, and uvs
		int capacity = level * level + (2 * level);
		if (vertexCache.Capacity < capacity)
		{
			vertexCache.Capacity = capacity;
			uv2Cache.Capacity = capacity;
			uvCache.Capacity = capacity;
			indiciesCache.Capacity = capacity;
			colorCache.Capacity = capacity;
		}

		vertexCache.Clear();
		uvCache.Clear();
		uv2Cache.Clear();
		indiciesCache.Clear();
		colorCache.Clear();


		p0sCache.Clear();
		p0suvCache.Clear();
		p0suv2Cache.Clear();
		p0scolorCache.Clear();

		p1sCache.Clear();
		p1suvCache.Clear();
		p1suv2Cache.Clear();
		p1scolorCache.Clear();

		p2sCache.Clear();
		p2suvCache.Clear();
		p2suv2Cache.Clear();
		p2scolorCache.Clear();

		// The incoming list is 9 entires, 
		// referenced as p0 through p8 here.

		// Generate extra rows to tessellate
		// each row is three control points
		// start, curve, end
		// The "lines" go as such
		// p0s from p0 to p3 to p6 ''
		// p1s from p1 p4 p7
		// p2s from p2 p5 p8

		Tessellate(level, control[0], control[3], control[6], p0sCache);
		TessellateUV(level, controlUvs[0], controlUvs[3], controlUvs[6], p0suvCache);
		TessellateUV(level, controlUv2s[0], controlUv2s[3], controlUv2s[6], p0suv2Cache);
		TessellateColor(level, controlColor[0], controlColor[3], controlColor[6], p0scolorCache);

		Tessellate(level, control[1], control[4], control[7], p1sCache);
		TessellateUV(level, controlUvs[1], controlUvs[4], controlUvs[7], p1suvCache);
		TessellateUV(level, controlUv2s[1], controlUv2s[4], controlUv2s[7], p1suv2Cache);
		TessellateColor(level, controlColor[1], controlColor[4], controlColor[7], p1scolorCache);

		Tessellate(level, control[2], control[5], control[8], p2sCache);
		TessellateUV(level, controlUvs[2], controlUvs[5], controlUvs[8], p2suvCache);
		TessellateUV(level, controlUv2s[2], controlUv2s[5], controlUv2s[8], p2suv2Cache);
		TessellateColor(level, controlColor[2], controlColor[5], controlColor[8], p2scolorCache);

		// Tessellate all those new sets of control points and pack
		// all the results into our vertex array, which we'll return.
		// Make our uvs list while we're at it.
		for (int i = 0; i <= level; i++)
		{
			Tessellate(level, p0sCache[i], p1sCache[i], p2sCache[i], vertexCache);
			TessellateUV(level, p0suvCache[i], p1suvCache[i], p2suvCache[i], uvCache);
			TessellateUV(level, p0suv2Cache[i], p1suv2Cache[i], p2suv2Cache[i], uv2Cache);
			TessellateColor(level, p0scolorCache[i], p1scolorCache[i], p2scolorCache[i], colorCache);
		}

		// This will produce (tessellationLevel + 1)^2 verts
		int numVerts = (level + 1) * (level + 1);

		// Computer triangle indexes for forming a mesh.
		// The mesh will be tessellationlevel + 1 verts
		// wide and tall.
		int xStep = 1;
		int width = level + 1;
		for (int i = 0; i < numVerts - width; i++)
		{
			//on left edge
			if (xStep == 1)
			{
				indiciesCache.Add(i);
				indiciesCache.Add(i + width);
				indiciesCache.Add(i + 1);

				xStep++;
			}
			else if (xStep == width) //on right edge
			{
				indiciesCache.Add(i);
				indiciesCache.Add(i + (width - 1));
				indiciesCache.Add(i + width);

				xStep = 1;
			}
			else // not on an edge, so add two
			{
				indiciesCache.Add(i);
				indiciesCache.Add(i + (width - 1));
				indiciesCache.Add(i + width);


				indiciesCache.Add(i);
				indiciesCache.Add(i + width);
				indiciesCache.Add(i + 1);

				xStep++;
			}
		}

		// Add the verts and tris
		patchMesh.SetVertices(vertexCache);
		patchMesh.SetTriangles(indiciesCache, 0, true);
		patchMesh.SetUVs(0, uvCache);
		patchMesh.SetUVs(2, uv2Cache);
		patchMesh.SetColors(colorCache);
		
		// Dunno if these are needed, but why not?
		// They're actually pretty cheap, considering.
		patchMesh.RecalculateNormals();
		patchMesh.Optimize();

		//Return the mesh! Shazam!
		Mesh = patchMesh;
	}

	public Mesh Mesh { get; }

    // Calculate UVs for our tessellated vertices 
    private Vector2 BezCurveUV(float t, Vector2 p0, Vector2 p1, Vector2 p2)
    {
        Vector2 bezPoint = new Vector2();

        float a = 1f - t;
        float tt = t * t;

        float[] tPoints = new float[2];
        for (int i = 0; i < 2; i++) tPoints[i] = a * a * p0[i] + 2 * a * (t * p1[i]) + tt * p2[i];

        bezPoint.Set(tPoints[0], tPoints[1]);

        return bezPoint;
    }

	// This time for colors
	private Color BezCurveColor(float t, Color p0, Color p1, Color p2)
	{
		float a = 1f - t;
		float tt = t * t;

		float[] tPoints = new float[4];
		for (int i = 0; i < 3; i++) tPoints[i] = a * a * p0[i] + 2 * a * (t * p1[i]) + tt * p2[i];
		
		Color bezPoint = new Color (tPoints[0], tPoints[1], tPoints[2], tPoints[3]);
		return bezPoint; 
	}

	// Calculate a vector3 at point t on a bezier curve between
	// p0 and p2 via p1.  
	private Vector3 BezCurve(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 bezPoint = new Vector3();

        float a = 1f - t;
        float tt = t * t;

        float[] tPoints = new float[3];
        for (int i = 0; i < 3; i++) tPoints[i] = a * a * p0[i] + 2 * a * (t * p1[i]) + tt * p2[i];

        bezPoint.Set(tPoints[0], tPoints[1], tPoints[2]);

        return bezPoint;
    }

    // This takes a tessellation level and three vector3
    // p0 is start, p1 is the midpoint, p2 is the endpoint
    // The returned list begins with p0, ends with p2, with
    // the tessellated verts in between.
    private void Tessellate(int level, Vector3 p0, Vector3 p1, Vector3 p2, List<Vector3> appendList = null)
    {
        if (appendList == null)
            appendList = new List<Vector3>(level + 1);

        float stepDelta = 1.0f / level;
        float step = stepDelta;

        appendList.Add(p0);
        for (int i = 0; i < level - 1; i++)
        {
            appendList.Add(BezCurve(step, p0, p1, p2));
            step += stepDelta;
        }

        appendList.Add(p2);
    }

    // Same as above, but for UVs
    private void TessellateUV(int level, Vector2 p0, Vector2 p1, Vector2 p2, List<Vector2> appendList = null)
    {
        if (appendList == null)
            appendList = new List<Vector2>(level + 2);

        float stepDelta = 1.0f / level;
        float step = stepDelta;

        appendList.Add(p0);
        for (int i = 0; i < level - 1; i++)
        {
            appendList.Add(BezCurveUV(step, p0, p1, p2));
            step += stepDelta;
        }

        appendList.Add(p2);
    }

	// Same, but this time for colors
	private void TessellateColor(int level, Color p0, Color p1, Color p2, List<Color> appendList = null)
	{
		if (appendList == null)
			appendList = new List<Color>(level + 1);

		float stepDelta = 1.0f / level;
		float step = stepDelta;

		appendList.Add(p0);
		for (int i = 0; i < level - 1; i++)
		{
			appendList.Add(BezCurveColor(step, p0, p1, p2));
			step += stepDelta;
		}

		appendList.Add(p2);
	}
}