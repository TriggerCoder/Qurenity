using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ConvexHull
{
	public const float EPSILON = 0.00001f;

	//Given points and a plane, find the point furthest away from the plane
	private static Vector3 FindPointFurthestAwayFromPlane(List<Vector3> points, NewPlane plane)
	{
		//Cant init by picking the first point in a list because it might be co-planar
		Vector3 bestPoint = default;

		float bestDistance = -Mathf.Infinity;

		foreach (Vector3 p in points)
		{
			float distance = GetSignedDistanceFromPointToPlane(p, plane);

			//Make sure the point is not co-planar
			float epsilon = EPSILON;

			//If distance is around 0
			if (distance > -epsilon && distance < epsilon)
			{
				continue;
			}

			//Make sure distance is positive
			if (distance < 0f)
				distance *= -1f;

			if (distance > bestDistance)
			{
				bestDistance = distance;
				bestPoint = p;
			}
		}

		return bestPoint;
	}


	//Given an edge and a list of points, find the point furthest away from the edge
	private static Vector3 FindPointFurthestFromEdge(Edge edge, List<Vector3> pointsList)
	{
		List<Vector3> points = new List<Vector3>(pointsList);

		//Init with the first point
		Vector3 pointFurthestAway = points[0];

		Vector3 closestPointOnLine = GetClosestPointOnLine(edge, pointFurthestAway, withinSegment: false);

		Vector3 maxOffSet = pointFurthestAway - closestPointOnLine;
		float maxDistSqr = maxOffSet.sqrMagnitude;

		//Try to find a better point
		for (int i = 1; i < points.Count; i++)
		{
			Vector3 thisPoint = points[i];

			//TODO make sure that thisPoint is NOT colinear with the edge because then we wont be able to build a triangle

			closestPointOnLine = GetClosestPointOnLine(edge, thisPoint, withinSegment: false);
			Vector3 offSet = thisPoint - closestPointOnLine;
			float distSqr = offSet.sqrMagnitude;

			if (distSqr > maxDistSqr)
			{
				maxDistSqr = distSqr;
				pointFurthestAway = thisPoint;
			}
		}

		return pointFurthestAway;
	}


	//From a list of points, find the two points that are furthest away from each other
	private static Edge FindEdgeFurthestApart(List<Vector3> pointsList)
	{
		List<Vector3> points = new List<Vector3>(pointsList);


		//Instead of using all points, find the points on the AABB
		Vector3 maxX = points[0]; //Cant use default because default doesnt exist and might be a min point
		Vector3 minX = points[0];
		Vector3 maxY = points[0];
		Vector3 minY = points[0];
		Vector3 maxZ = points[0];
		Vector3 minZ = points[0];

		for (int i = 1; i < points.Count; i++)
		{
			Vector3 p = points[i];

			if (p.x > maxX.x)
			{
				maxX = p;
			}
			if (p.x < minX.x)
			{
				minX = p;
			}

			if (p.y > maxY.y)
			{
				maxY = p;
			}
			if (p.y < minY.y)
			{
				minY = p;
			}

			if (p.z > maxZ.z)
			{
				maxZ = p;
			}
			if (p.z < minZ.z)
			{
				minZ = p;
			}
		}

		//Some of these might be the same point (like minZ and minY)
		//But we have earlier check that the points have a width greater than 0, so we should get the points we need
		List<Vector3> extremePointsList = new List<Vector3>();

		extremePointsList.Add(maxX);
		extremePointsList.Add(minX);
		extremePointsList.Add(maxY);
		extremePointsList.Add(minY);
		extremePointsList.Add(maxZ);
		extremePointsList.Add(minZ);

		points = new List<Vector3>(extremePointsList);


		//Find all possible combinations of edges between all points
		List<Edge> pointCombinations = new List<Edge>();

		for (int i = 0; i < points.Count; i++)
		{
			Vector3 p1 = points[i];

			for (int j = i + 1; j < points.Count; j++)
			{
				Vector3 p2 = points[j];

				Edge e = new Edge(p1, p2);

				pointCombinations.Add(e);
			}
		}


		//Find the edge that is the furthest apart

		//Init by picking the first edge
		Edge eFurthestApart = pointCombinations[0];
		Vector3 edgeOffset = eFurthestApart.p1 - eFurthestApart.p2;
		float maxDistanceBetween = edgeOffset.sqrMagnitude;

		//Try to find a better edge
		for (int i = 1; i < pointCombinations.Count; i++)
		{
			Edge e = pointCombinations[i];

			Vector3 offsetBetween = e.p1 - e.p2;
			float distanceBetween = offsetBetween.sqrMagnitude;

			if (distanceBetween > maxDistanceBetween)
			{
				maxDistanceBetween = distanceBetween;

				eFurthestApart = e;
			}
		}

		return eFurthestApart;
	}

	//Initialize by making 2 triangles by using three points, so its a flat triangle with a face on each side
	//We could use the ideas from Quickhull to make the start triangle as big as possible
	//Then find a point which is the furthest away as possible from these triangles
	//Add that point and you have a tetrahedron (triangular pyramid)
	public static void BuildFirstTetrahedron(List<Vector3> points, HalfEdgeData convexHull)
	{
		//Of all points, find the two points that are furthes away from each other
		Edge eFurthestApart = FindEdgeFurthestApart(points);

		//Remove the two points we found         
		points.Remove(eFurthestApart.p1);
		points.Remove(eFurthestApart.p2);


		//Find a point which is the furthest away from this edge
		Vector3 pointFurthestAway = FindPointFurthestFromEdge(eFurthestApart, points);

		//Remove the point
		points.Remove(pointFurthestAway);


		//Now we can build two triangles
		//It doesnt matter how we build these triangles as long as they are opposite
		//But the normal matters, so make sure it is calculated so the triangles are ordered clock-wise while the normal is pointing out
		Vector3 p1 = eFurthestApart.p1;
		Vector3 p2 = eFurthestApart.p2;
		Vector3 p3 = pointFurthestAway;

		convexHull.AddTriangle(p1, p2, p3);
		convexHull.AddTriangle(p1, p3, p2);

		//Find the point which is furthest away from the triangle (this point cant be co-planar)
		List<HalfEdgeFace> triangles = new List<HalfEdgeFace>(convexHull.faces);

		//Just pick one of the triangles
		HalfEdgeFace triangle = triangles[0];

		//Build a plane
		NewPlane plane = new NewPlane(triangle.edge.v.normal, triangle.edge.v.position);

		//Find the point furthest away from the plane
		Vector3 p4 = FindPointFurthestAwayFromPlane(points, plane);

		//Remove the point
		points.Remove(p4);

		//Now we have to remove one of the triangles == the triangle the point is outside of
		HalfEdgeFace triangleToRemove = triangles[0];
		HalfEdgeFace triangleToKeep = triangles[1];

		//This means the point is inside the triangle-plane, so we have to switch
		//We used triangle #0 to generate the plane
		if (GetSignedDistanceFromPointToPlane(p4, plane) < 0f)
		{
			triangleToRemove = triangles[1];
			triangleToKeep = triangles[0];
		}

		//Delete the triangle 
		convexHull.DeleteFace(triangleToRemove);

		//Build three new triangles

		//The triangle we keep is ordered clock-wise:
		Vector3 p1_opposite = triangleToKeep.edge.v.position;
		Vector3 p2_opposite = triangleToKeep.edge.nextEdge.v.position;
		Vector3 p3_opposite = triangleToKeep.edge.nextEdge.nextEdge.v.position;

		//But we are looking at it from the back-side, 
		//so we add those vertices counter-clock-wise to make the new triangles clock-wise
		convexHull.AddTriangle(p1_opposite, p3_opposite, p4);
		convexHull.AddTriangle(p3_opposite, p2_opposite, p4);
		convexHull.AddTriangle(p2_opposite, p1_opposite, p4);

		//Make sure all opposite edges are connected
		convexHull.ConnectAllEdgesSlow();

	}
	public static Mesh GenerateMeshFromConvexHull(string MeshName, List<Vector3> vertex)
	{
		HalfEdgeData convexHull = GenerateConvexHull(vertex);
		return convexHull.ConvertToMesh(MeshName);
	}

	public static HalfEdgeData GenerateConvexHull(List<Vector3> points)
	{
		HalfEdgeData convexHull = new HalfEdgeData();

		//Step 1. Init by making a tetrahedron (triangular pyramid) and remove all points within the tetrahedron

		BuildFirstTetrahedron(points, convexHull);

		//Step 2. For each other point: 
		// -If the point is within the hull constrcuted so far, remove it
		// - Otherwise, see which triangles are visible to the point and remove them
		//   Then build new triangles from the edges that have no neighbor to the point

		List<Vector3> pointsToAdd = new List<Vector3>(points);

		int removedPointsCounter = 0;

		foreach (Vector3 p in pointsToAdd)
		{
			//Is this point within the tetrahedron
			bool isWithinHull = PointWithinConvexHull(p, convexHull);

			if (isWithinHull)
			{
				points.Remove(p);

				removedPointsCounter += 1;

				continue;
			}

			//Find visible triangles and edges on the border between the visible and invisible triangles
			List<HalfEdgeFace> visibleTriangles = null;
			List<HalfEdge> borderEdges = null;

			FindVisibleTrianglesAndBorderEdgesFromPoint(p, convexHull, out visibleTriangles, out borderEdges);

			//Remove all visible triangles
			foreach (HalfEdgeFace triangle in visibleTriangles)
			{
				convexHull.DeleteFace(triangle);
			}

			//Save all ned edges so we can connect them with an opposite edge
			//To make it faster you can use the ideas in the Valve paper to get a sorted list of newEdges
			List<HalfEdge> newEdges = new List<HalfEdge>();

			foreach (HalfEdge borderEdge in borderEdges)
			{
				//Each edge is point TO a vertex
				Vector3 p1 = borderEdge.prevEdge.v.position;
				Vector3 p2 = borderEdge.v.position;

				//The border edge belongs to a triangle which is invisible
				//Because triangles are oriented clockwise, we have to add the vertices in the other direction
				//to build a new triangle with the point
				HalfEdgeFace newTriangle = convexHull.AddTriangle(p2, p1, p);

				//Connect the new triangle with the opposite edge on the border
				//When we create the face we give it a reference edge which goes to p2
				//So the edge we want to connect is the next edge
				HalfEdge edgeToConnect = newTriangle.edge.nextEdge;

				edgeToConnect.oppositeEdge = borderEdge;
				borderEdge.oppositeEdge = edgeToConnect;

				//Two edges are still not connected, so save those
				HalfEdge e1 = newTriangle.edge;
				//HalfEdge e2 = newTriangle.edge.nextEdge;
				HalfEdge e3 = newTriangle.edge.nextEdge.nextEdge;

				newEdges.Add(e1);
				//newEdges.Add(e2);
				newEdges.Add(e3);
			}

			//Two edges in each triangle are still not connected with an opposite edge
			foreach (HalfEdge e in newEdges)
			{
				if (e.oppositeEdge != null)
				{
					continue;
				}

				convexHull.TryFindOppositeEdge(e, newEdges);
			}

			//Connect all new triangles and the triangles on the border, 
			//so each edge has an opposite edge or flood filling will be impossible
		}

		//
		// Clean up 
		//

		//Merge concave edges according to the paper
		return convexHull;
	}

	public class HalfEdgeData
	{
		public List<HalfEdgeVert> verts;
		public List<HalfEdgeFace> faces;
		public List<HalfEdge> edges;
		public HalfEdgeData()
		{
			this.verts = new List<HalfEdgeVert>();
			this.faces = new List<HalfEdgeFace>();
			this.edges = new List<HalfEdge>();
		}

		//
		// Get a list with unique edges
		//

		//Currently we have two half-edges for each edge, making it time consuming to go through them 
		//But it's also time consuming to create this list so make sure you measure time which is better
		public List<HalfEdge> GetUniqueEdges()
		{
			// Create a list to store the unique edges
			List<HalfEdge> uniqueEdges = new List<HalfEdge>();

			// Create a dictionary to store the unique edges and their corresponding HalfEdge objects
			Dictionary<(Vector3, Vector3), HalfEdge> uniqueEdgesDict = new Dictionary<(Vector3, Vector3), HalfEdge>();

			// Iterate through the list of edges
			foreach (HalfEdge edge in edges)
			{
				// Get the positions of the edge's vertices
				Vector3 p1 = edge.v.position;
				Vector3 p2 = edge.prevEdge.v.position;

				// Check if the edge exists in the dictionary
				if (!uniqueEdgesDict.ContainsKey((p1, p2)))
				{
					// Add the edge to the list and dictionary if it doesn't already exist
					uniqueEdges.Add(edge);
					uniqueEdgesDict.Add((p1, p2), edge);
				}
			}
			return uniqueEdges;
		}



		//
		// Find opposite edge to edge 
		//

		//Connect all edges with each other which means we have all data except opposite edge of each (or just some) edge
		//This should be kinda fast because when we have found an opposite edge, we can at the same time connect the opposite edge to the edge
		//And when it is connected we don't need to test if it's pointing at the vertex when seaching for opposite edges
		public void ConnectAllEdgesSlow()
		{
			foreach (HalfEdge e in edges)
			{
				if (e.oppositeEdge == null)
				{
					TryFindOppositeEdge(e);
				}
			}
		}

		//If we know that the vertex positions were created in the same way (no floating point precision issues) 
		//we can generate a lookup table of all edges which should make it faster to find an opposite edge for each edge
		//This method takes rough 0.1 seconds for the bunny, while the slow method takes 1.6 seconds
		public void ConnectAllEdgesFast()
		{
			//Create the lookup table
			//Important in this case that Edge is a struct
			Dictionary<Edge, HalfEdge> edgeLookup = new Dictionary<Edge, HalfEdge>();

			//We can also maybe create a list of all edges which are not connected, so we don't have to search through all edges again?
			//List<HalfEdge> unconnectedEdges = new List<HalfEdge>();

			foreach (HalfEdge e in edges)
			{
				//Dont add it if its opposite is not null
				//Sometimes we run this method if just a few edges are not connected
				//This means this edge is already connected, so it cant possibly be connected with the edges we want to connect
				if (e.oppositeEdge != null)
				{
					continue;
				}

				//Each edge points TO a vertex
				Vector3 p2 = e.v.position;
				Vector3 p1 = e.prevEdge.v.position;

				edgeLookup.Add(new Edge(p1, p2), e);
			}

			//Connect edges
			foreach (HalfEdge e in edges)
			{
				//This edge is already connected
				//Is faster to first do a null check
				if (e.oppositeEdge != null)
				//if (!(e.oppositeEdge is null)) //Is slightly slower
				{
					continue;
				}

				//Each edge points TO a vertex, so the opposite edge goes in the opposite direction
				Vector3 p1 = e.v.position;
				Vector3 p2 = e.prevEdge.v.position;

				Edge edgeToLookup = new Edge(p1, p2);

				//This is slightly faster than first edgeLookup.ContainsKey(edgeToLookup)
				HalfEdge eOther = null;

				edgeLookup.TryGetValue(edgeToLookup, out eOther);

				if (eOther != null)
				{
					//Connect them with each other
					e.oppositeEdge = eOther;

					eOther.oppositeEdge = e;
				}

				//This edge doesnt exist so opposite edge must be null
			}
		}



		//Connect an edge with an unknown opposite edge which has not been connected
		//If no opposite edge exists, it means it has no neighbor which is possible if there's a hole
		public void TryFindOppositeEdge(HalfEdge e)
		{
			TryFindOppositeEdge(e, edges);
		}


		//An optimization is to have a list of opposite edges, so we don't have to search ALL edges in the entire triangulation
		public void TryFindOppositeEdge(HalfEdge e, List<HalfEdge> otherEdges)
		{
			//We need to find an edge which is: 
			// - going to a position where this edge is coming from
			// - coming from a position this edge points to
			//An edge is pointing to a position
			Vector3 pTo = e.prevEdge.v.position;
			Vector3 pFrom = e.v.position;

			foreach (HalfEdge eOther in otherEdges)
			{
				//Don't need to check edges that have already been connected
				if (eOther.oppositeEdge != null)
				{
					continue;
				}

				//Is this edge pointing from a specific vertex to a specific vertex
				//If so it means we have found an edge going in the other direction
				if (eOther.v.position.Equals(pTo) && eOther.prevEdge.v.position.Equals(pFrom))
				{
					//Connect them with each other
					e.oppositeEdge = eOther;

					eOther.oppositeEdge = e;

					break;
				}
			}
		}



		//
		// Merge this half edge mesh with another half-edge mesh
		//
		public void MergeMesh(HalfEdgeData otherMesh)
		{
			this.verts.AddRange(otherMesh.verts);
			this.faces.AddRange(otherMesh.faces);
			this.edges.AddRange(otherMesh.edges);
		}

		//
		// Convert to mesh
		//

		//We know we have stored triangles in the data structure
		//shareVertices means that we want a smooth surface where some vertices are shared between triangles
		public Mesh ConvertToMesh(string meshName)
		{
			Mesh Mesh = new Mesh();
			Mesh.name = meshName;
			List<Vector3> Vertexes = new List<Vector3>();
			Dictionary<Vector3, int> VertstoIndex= new Dictionary<Vector3, int>();
			//Loop through each triangle
			List<int> Triangles = new List<int>();
			for (int i = 0, j = 0; i < faces.Count; i++)
			{
				//These should have been stored clock-wise
				Vector3 v1 = faces[i].edge.v.position;
				Vector3 v2 = faces[i].edge.nextEdge.v.position;
				Vector3 v3 = faces[i].edge.nextEdge.nextEdge.v.position;

				int currentTriangle;

				//V1
				if (Vertexes.Contains(v1))
					currentTriangle = VertstoIndex[v1];
				else
				{
					Vertexes.Add(v1);
					VertstoIndex.Add(v1, j);
					currentTriangle = j;
					j++;
				}
				Triangles.Add(currentTriangle);

				//V2
				if (Vertexes.Contains(v2))
					currentTriangle = VertstoIndex[v2];
				else
				{
					Vertexes.Add(v2);
					VertstoIndex.Add(v2, j);
					currentTriangle = j;
					j++;
				}
				Triangles.Add(currentTriangle);

				//V3
				if (Vertexes.Contains(v3))
					currentTriangle = VertstoIndex[v3];
				else
				{
					Vertexes.Add(v3);
					VertstoIndex.Add(v3, j);
					currentTriangle = j;
					j++;
				}
				Triangles.Add(currentTriangle);
			}
			Mesh.vertices = Vertexes.ToArray();
			Mesh.triangles = Triangles.ToArray();
			Mesh.RecalculateNormals();
			return Mesh;
		}

		//
		// We have faces, but we also want a list with vertices, edges, etc
		//

		public static HalfEdgeData GenerateHalfEdgeDataFromFaces(List<HalfEdgeFace> faces)
		{
			HalfEdgeData meshData = new HalfEdgeData();

			//What we need to fill
			List<HalfEdge> edges = new List<HalfEdge>();

			List<HalfEdgeVert> verts = new List<HalfEdgeVert>();

			foreach (HalfEdgeFace f in faces)
			{
				//Get all edges in this face
				List<HalfEdge> edgesInFace = f.GetEdges();

				foreach (HalfEdge e in edgesInFace)
				{
					edges.Add(e);
					verts.Add(e.v);
				}
			}

			meshData.faces = faces;
			meshData.edges = edges;
			meshData.verts = verts;

			return meshData;
		}



		//
		// Add a triangle to this mesh
		//

		//We dont have a normal so we have to calculate it, so make sure v1-v2-v3 is clock-wise
		public HalfEdgeFace AddTriangle(Vector3 p1, Vector3 p2, Vector3 p3, bool findOppositeEdge = false)
		{
			Vector3 normal = Vector3.Normalize(Vector3.Cross(p3 - p2, p1 - p2));

			EdgeVertex v1 = new EdgeVertex(p1, normal);
			EdgeVertex v2 = new EdgeVertex(p2, normal);
			EdgeVertex v3 = new EdgeVertex(p3, normal);

			HalfEdgeFace f = AddTriangle(v1, v2, v3);

			return f;
		}

		//v1-v2-v3 should be clock-wise which is Unity standard
		public HalfEdgeFace AddTriangle(EdgeVertex v1, EdgeVertex v2, EdgeVertex v3, bool findOppositeEdge = false)
		{
			//Create three new vertices
			HalfEdgeVert half_v1 = new HalfEdgeVert(v1.position, v1.normal);
			HalfEdgeVert half_v2 = new HalfEdgeVert(v2.position, v2.normal);
			HalfEdgeVert half_v3 = new HalfEdgeVert(v3.position, v3.normal);

			//Create three new half-edges that points TO these vertices
			HalfEdge e_to_v1 = new HalfEdge(half_v1);
			HalfEdge e_to_v2 = new HalfEdge(half_v2);
			HalfEdge e_to_v3 = new HalfEdge(half_v3);

			//Create the face (which is a triangle) which needs a reference to one of the edges
			HalfEdgeFace f = new HalfEdgeFace(e_to_v1);


			//Connect the data:

			//Connect the edges clock-wise
			e_to_v1.nextEdge = e_to_v2;
			e_to_v2.nextEdge = e_to_v3;
			e_to_v3.nextEdge = e_to_v1;

			e_to_v1.prevEdge = e_to_v3;
			e_to_v2.prevEdge = e_to_v1;
			e_to_v3.prevEdge = e_to_v2;

			//Each vertex needs a reference to an edge going FROM that vertex
			half_v1.edge = e_to_v2;
			half_v2.edge = e_to_v3;
			half_v3.edge = e_to_v1;

			//Each edge needs a reference to the face
			e_to_v1.face = f;
			e_to_v2.face = f;
			e_to_v3.face = f;

			//Each edge needs an opposite edge
			//This is slow process 
			//You could do this afterwards when all triangles have been generate
			//Doing it in this method takes 2.7 seconds for the bunny
			//Doing it afterwards takes 0.1 seconds by using the fast method and 1.6 seconds for the slow method
			//The reason is that we keep searching the list for an opposite which doesnt exist yet, so we get more searches even though
			//the list is shorter as we build up the mesh
			//But you could maybe do it here if you just add a new triangle?
			if (findOppositeEdge)
			{
				TryFindOppositeEdge(e_to_v1);
				TryFindOppositeEdge(e_to_v2);
				TryFindOppositeEdge(e_to_v3);
			}


			//Save the data
			this.verts.Add(half_v1);
			this.verts.Add(half_v2);
			this.verts.Add(half_v3);

			this.edges.Add(e_to_v1);
			this.edges.Add(e_to_v2);
			this.edges.Add(e_to_v3);

			this.faces.Add(f);

			return f;
		}



		//
		// Delete a face from this data structure
		//

		public void DeleteFace(HalfEdgeFace f)
		{
			//Get all edges belonging to this face
			//TODO: This creates garbage because we create a list for each face, so maybe better to move the loop to here...
			List<HalfEdge> edgesToRemove = f.GetEdges();

			if (edgesToRemove == null)
			{
				Debug.LogWarning("This face can't be deleted because the edges are not fully connected");

				return;
			}

			foreach (HalfEdge edgeToRemove in edgesToRemove)
			{
				//The opposite edge to this edge is referencing this edges, so remove that connection
				if (edgeToRemove.oppositeEdge != null)
				{
					edgeToRemove.oppositeEdge.oppositeEdge = null;
				}

				//Remove the edge and the vertex the edge points to from the list of all vertices and edges
				this.edges.Remove(edgeToRemove);
				this.verts.Remove(edgeToRemove.v);

				//Set face reference to null, which is needed for some other methods
				edgeToRemove.face = null;
			}

			//Remove the face from the list of all faces
			this.faces.Remove(f);
		}



		//
		// Contract an edge if we know we are dealing only with triangles
		//

		//Returns all edge pointing to the new vertex
		public List<HalfEdge> ContractTriangleHalfEdge(HalfEdge e, Vector3 mergePos, System.Diagnostics.Stopwatch timer = null)
		{
			//Step 1. Get all edges pointing to the vertices we will merge
			//And edge is going TO a vertex, so this edge goes from v1 to v2
			HalfEdgeVert v1 = e.prevEdge.v;
			HalfEdgeVert v2 = e.v;

			//timer.Start();
			//It's better to get these before we remove triangles because then we will get a messed up half-edge system? 
			List<HalfEdge> edgesGoingToVertex_v1 = v1.GetEdgesPointingToVertex(this);
			List<HalfEdge> edgesGoingToVertex_v2 = v2.GetEdgesPointingToVertex(this);
			//timer.Stop();

			//Step 2. Remove the triangles, which will create a hole, 
			//and the edges on the opposite sides of the hole are connected
			RemoveTriangleAndConnectOppositeSides(e);

			//We might also have an opposite triangle, so we may have to delete a total of two triangles
			if (e.oppositeEdge != null)
			{
				RemoveTriangleAndConnectOppositeSides(e.oppositeEdge);
			}


			//Step 3. Move the vertices to the merge position
			//Some of these edges belong to the triangles we removed, but it doesnt matter because this operation is fast

			//We can at the same time find the edges pointing to the new vertex
			List<HalfEdge> edgesPointingToVertex = new List<HalfEdge>();

			if (edgesGoingToVertex_v1 != null)
			{
				foreach (HalfEdge edgeToV in edgesGoingToVertex_v1)
				{
					//This edge belonged to one of the faces we removed
					if (edgeToV.face == null)
					{
						continue;
					}

					edgeToV.v.position = mergePos;

					edgesPointingToVertex.Add(edgeToV);
				}
			}
			if (edgesGoingToVertex_v2 != null)
			{
				foreach (HalfEdge edgeToV in edgesGoingToVertex_v2)
				{
					//This edge belonged to one of the faces we removed
					if (edgeToV.face == null)
					{
						continue;
					}

					edgeToV.v.position = mergePos;

					edgesPointingToVertex.Add(edgeToV);
				}
			}


			return edgesPointingToVertex;
		}

		//Help method to above
		private void RemoveTriangleAndConnectOppositeSides(HalfEdge e)
		{
			//AB is the edge we want to contract
			HalfEdge e_AB = e;
			HalfEdge e_BC = e.nextEdge;
			HalfEdge e_CA = e.nextEdge.nextEdge;

			//The triangle belonging to this edge
			HalfEdgeFace f_ABC = e.face;

			//Delete the triangle (which will also delete the vertices and edges belonging to that face)
			//We have to do it before we re-connect the edges because it sets all opposite edges to null, making a hole
			DeleteFace(f_ABC);

			//Connect the opposite edges of the edges which are not a part of the edge we want to delete
			//This will connect the opposite sides of the hole
			//We move vertices in another method
			if (e_BC.oppositeEdge != null)
			{
				//The edge on the opposite side of BC should have its opposite edge connected with the opposite edge of CA
				e_BC.oppositeEdge.oppositeEdge = e_CA.oppositeEdge;
			}
			if (e_CA.oppositeEdge != null)
			{
				e_CA.oppositeEdge.oppositeEdge = e_BC.oppositeEdge;
			}
		}
	}
	public class HalfEdgeVert
    {
        //The position of the vertex
        public Vector3 position;
        //In 3d space we also need a normal, which should maybe be a class so it can be null
        //Instead of storing normals, uvs, etc for each vertex, some people are using a data structure called "wedge"
        //A wedge includes the same normal, uv, etc for the vertices that's sharing this data. 
        //For example, some normals are the same to get a smooth edge and then they all have the same wedge
        //So if the wedge is not the same for two vertices with the same position, we know we have to add both vertices because we have an hard edge
        public Vector3 normal;

        //Each vertex references an half-edge that starts at this point
        //Might seem strange because each halfEdge references a vertex the edge is going to?
        public HalfEdge edge;



        public HalfEdgeVert(Vector3 position)
        {
            this.position = position;
        }

        public HalfEdgeVert(Vector3 position, Vector3 normal)
        {
            this.position = position;

            this.normal = normal;
        }



        //Return all edges going to this vertex = all edges that references this vertex position
        //meshData is needed if we cant spring around this vertex because there might be a hole in the mesh
        //If so we have to search through all edges in the entire mesh
        public List<HalfEdge> GetEdgesPointingToVertex(HalfEdgeData meshData)
        {
            List<HalfEdge> allEdgesGoingToVertex = new List<HalfEdge>();

            //This is the edge that goes to this vertex
            HalfEdge currentEdge = this.edge.prevEdge;

            //if (currentEdge == null) Debug.Log("Edge is null");

            int safety = 0;

            do
            {
                allEdgesGoingToVertex.Add(currentEdge);

                //This edge is going to the vertex but in another triangle
                HalfEdge oppositeEdge = currentEdge.oppositeEdge;

                if (oppositeEdge == null)
                {
                    Debug.LogWarning("We cant rotate around this vertex because there are holes in the mesh");

                    //Better to clear than to null or we have to create a new List when filling it the brute force way 
                    allEdgesGoingToVertex.Clear();

                    break;
                }

                //if (oppositeEdge == null) Debug.Log("Opposite edge is null");

                currentEdge = oppositeEdge.prevEdge;

                safety += 1;

                if (safety > 1000)
                {
                    Debug.LogWarning("Stuck in infinite loop when getting all edges around a vertex");

                    allEdgesGoingToVertex.Clear();

                    break;
                }
            }
            while (currentEdge != this.edge.prevEdge);



            //If there are holes in the triangulation around the vertex, 
            //we have to use the brute force approach and look at edges
            if (allEdgesGoingToVertex.Count == 0 && meshData != null)
            {
                List<HalfEdge> edges = meshData.edges;

                foreach (HalfEdge e in edges)
                {
                    //An edge points TO a vertex
                    if (e.v.position.Equals(position))
                    {
                        allEdgesGoingToVertex.Add(e);
                    }
                }
            }


            return allEdgesGoingToVertex;
        }
    }



    //This face could be a triangle or whatever we need
    public class HalfEdgeFace
    {
        //Each face references one of the halfedges bounding it
        //If you need the vertices, you can use this edge
        public HalfEdge edge;



        public HalfEdgeFace(HalfEdge edge)
        {
            this.edge = edge;
        }



        //Get all edges that make up this face
        //If you need all vertices you can use this method because each edge points to a vertex
        public List<HalfEdge> GetEdges()
        {
            List<HalfEdge> allEdges = new List<HalfEdge>();
        
            HalfEdge currentEdge = this.edge;

            int safety = 0;

            do
            {
                allEdges.Add(currentEdge);

                currentEdge = currentEdge.nextEdge;

                safety += 1;

                if (safety > 100000)
                {
                    Debug.LogWarning("Stuck in infinite loop when getting all edges from a face");

                    return null;
                }
            }
            while (currentEdge != this.edge);

            return allEdges;
        }
    }



    //An edge going in a direction
    public class HalfEdge
    {
        //The vertex it points TO
        //This vertex also has an edge reference, which is NOT this edge, but and edge going FROM this vertex
        public HalfEdgeVert v;

        //The face it belongs to
        public HalfEdgeFace face;

        //The next half-edge inside the face (ordered clockwise)
        //The document says counter-clockwise but clockwise is easier because that's how Unity is displaying triangles
        public HalfEdge nextEdge;

        //The opposite half-edge belonging to the neighbor (if there's a neighbor, otherwise its just null)
        public HalfEdge oppositeEdge;

        //(optionally) the previous halfedge in the face
        //If we assume the face is closed, then we could identify this edge by walking forward until we reach it
        public HalfEdge prevEdge;



        public HalfEdge(HalfEdgeVert v)
        {
            this.v = v;
        }



        //The length of this edge
        public float Length()
        {
            //The edge points TO a vertex
            Vector3 p2 = v.position;
            Vector3 p1 = prevEdge.v.position;

            float length = Vector3.Distance(p1, p2);

            return length;
        }

        public float SqrLength()
        {
            //The edge points TO a vertex
			Vector3 offset = prevEdge.v.position - v.position;

			float length = offset.sqrMagnitude;

            return length;
        }
    }

	//And edge between two vertices in 3d space
	public struct Edge
	{
		public Vector3 p1;
		public Vector3 p2;

		//Is this edge intersecting with another edge?
		//public bool isIntersecting = false;

		public Edge(Vector3 p1, Vector3 p2)
		{
			this.p1 = p1;
			this.p2 = p2;
		}
	}

	public struct EdgeVertex
	{
		public Vector3 position;
		public Vector3 normal;

		public EdgeVertex(Vector3 position, Vector3 normal)
		{
			this.position = position;
			this.normal = normal;
		}
	}

	public class NewPlane
	{
		public Vector3 pos;
		public Vector3 normal;

		public NewPlane(Vector3 normal, Vector3 pos)
		{
			this.pos = pos;
			this.normal = normal;
		}
	}

	//The signed distance from a point to a plane
	//- Positive distance denotes that the point p is outside the plane (in the direction of the plane normal)
	//- Negative means it's inside

	//3d
	public static float GetSignedDistanceFromPointToPlane(Vector3 pointPos, NewPlane plane)
	{
		float distance = Vector3.Dot(plane.normal, pointPos - plane.pos);

		return distance;
	}

	//Outside means in the planes normal direction
	public static bool IsPointOutsidePlane(Vector3 pointPos, NewPlane plane)
	{
		float distance = GetSignedDistanceFromPointToPlane(pointPos, plane);

		//To avoid floating point precision issues we can add a small value
		float epsilon = EPSILON;

		if (distance > 0f + epsilon)
		{
			return true;
		}
		else
		{
			return false;
		}
	}

	//Find a visible triangle from a point
	private static HalfEdgeFace FindVisibleTriangleFromPoint(Vector3 p, List<HalfEdgeFace> triangles)
	{
		HalfEdgeFace visibleTriangle = null;

		foreach (HalfEdgeFace triangle in triangles)
		{
			//A triangle is visible from a point the point is outside of a plane formed with the triangles position and normal 
			NewPlane plane = new NewPlane(triangle.edge.v.normal, triangle.edge.v.position);

			bool isPointOutsidePlane = IsPointOutsidePlane(p, plane);

			//We have found a triangle which is visible from the point and should be removed
			if (isPointOutsidePlane)
			{
				visibleTriangle = triangle;

				break;
			}
		}

		return visibleTriangle;
	}


	//Find all visible triangles from a point
	//Also find edges on the border between invisible and visible triangles
	public static void FindVisibleTrianglesAndBorderEdgesFromPoint(Vector3 p, HalfEdgeData convexHull, out List<HalfEdgeFace> visibleTriangles, out List<HalfEdge> borderEdges)
	{
		//Flood-fill from the visible triangle to find all other visible triangles
		//When you cross an edge from a visible triangle to an invisible triangle, 
		//save the edge because thhose edge should be used to build triangles with the point
		//These edges should belong to the triangle which is not visible
		borderEdges = new List<HalfEdge>();

		//Store all visible triangles here so we can't visit triangles multiple times
		visibleTriangles = new List<HalfEdgeFace>();


		//Start the flood-fill by finding a triangle which is visible from the point
		//A triangle is visible if the point is outside the plane formed at the triangles
		//Another sources is using the signed volume of a tetrahedron formed by the triangle and the point
		HalfEdgeFace visibleTriangle = FindVisibleTriangleFromPoint(p, convexHull.faces);

		//If we didn't find a visible triangle, we have some kind of edge case and should move on for now
		if (visibleTriangle == null)
		{
			Debug.LogWarning("Couldn't find a visible triangle so will ignore the point");

			return;
		}


		//The queue which we will use when flood-filling
		Queue<HalfEdgeFace> trianglesToFloodFrom = new Queue<HalfEdgeFace>();

		//Add the first triangle to init the flood-fill 
		trianglesToFloodFrom.Enqueue(visibleTriangle);

		List<HalfEdge> edgesToCross = new List<HalfEdge>();

		int safety = 0;

		while (true)
		{
			//We have visited all visible triangles
			if (trianglesToFloodFrom.Count == 0)
			{
				break;
			}

			HalfEdgeFace triangleToFloodFrom = trianglesToFloodFrom.Dequeue();

			//This triangle is always visible and should be deleted
			visibleTriangles.Add(triangleToFloodFrom);

			//Investigate bordering triangles
			edgesToCross.Clear();

			edgesToCross.Add(triangleToFloodFrom.edge);
			edgesToCross.Add(triangleToFloodFrom.edge.nextEdge);
			edgesToCross.Add(triangleToFloodFrom.edge.nextEdge.nextEdge);

			//Jump from this triangle to a bordering triangle
			foreach (HalfEdge edgeToCross in edgesToCross)
			{
				HalfEdge oppositeEdge = edgeToCross.oppositeEdge;

				if (oppositeEdge == null)
				{
					Debug.LogWarning("Found an opposite edge which is null");

					break;
				}

				HalfEdgeFace oppositeTriangle = oppositeEdge.face;

				//Have we visited this triangle before (only test visible triangles)?
				if (trianglesToFloodFrom.Contains(oppositeTriangle) || visibleTriangles.Contains(oppositeTriangle))
				{
					continue;
				}

				//Check if this triangle is visible
				//A triangle is visible from a point the point is outside of a plane formed with the triangles position and normal 
				NewPlane plane = new NewPlane(oppositeTriangle.edge.v.normal, oppositeTriangle.edge.v.position);

				bool isPointOutsidePlane = IsPointOutsidePlane(p, plane);

				//This triangle is visible so save it so we can flood from it
				if (isPointOutsidePlane)
				{
					trianglesToFloodFrom.Enqueue(oppositeTriangle);
				}
				//This triangle is invisible. Since we only flood from visible triangles, 
				//it means we crossed from a visible triangle to an invisible triangle, so save the crossing edge
				else
				{
					borderEdges.Add(oppositeEdge);
				}
			}


			safety += 1;

			if (safety > 50000)
			{
				Debug.Log("Stuck in infinite loop when flood-filling visible triangles");

				break;
			}
		}
	}


	// Is a point within a convex hull?
	//If the point is on the hull it's "inside"
	public static bool PointWithinConvexHull(Vector3 point, HalfEdgeData convexHull)
	{
		bool isInside = true;

		float epsilon = EPSILON;

		//We know a point is within the hull if the point is inside all planes formed by the faces of the hull
		foreach (HalfEdgeFace triangle in convexHull.faces)
		{
			//Build a plane
			NewPlane plane = new NewPlane(triangle.edge.v.normal, triangle.edge.v.position);

			//Find the distance to the plane from the point
			//The distance is negative if the point is inside the plane
			float distance = GetSignedDistanceFromPointToPlane(point, plane);

			//This point is outside, which means we don't need to test more planes
			if (distance > 0f + epsilon)
			{
				isInside = false;

				break;
			}
		}

		return isInside;
	}
	public static Vector3 GetClosestPointOnLine(Edge e, Vector3 p, bool withinSegment)
	{
		Vector3 a = e.p1;
		Vector3 b = e.p2;

		//Assume the line goes from a to b
		Vector3 ab = b - a;
		//Vector from start of the line to the point outside of line
		Vector3 ap = p - a;

		//The normalized "distance" from a to the closest point, so [0,1] if we are within the line segment
		float distance = Vector3.Dot(ap, ab) / Vector3.SqrMagnitude(ab);


		///This point may not be on the line segment, if so return one of the end points
		float epsilon = EPSILON;

		if (withinSegment && distance < 0f - epsilon)
		{
			return a;
		}
		else if (withinSegment && distance > 1f + epsilon)
		{
			return b;
		}
		else
		{
			//This works because a_b is not normalized and distance is [0,1] if distance is within ab
			return a + ab * distance;
		}
	}

}