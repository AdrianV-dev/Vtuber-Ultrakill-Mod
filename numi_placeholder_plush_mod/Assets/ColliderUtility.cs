using System.Collections.Generic;
using NewBlood.Rendering;
using ULTRAKILL.Cheats;
using UnityEngine;

public static class ColliderUtility
{
	private static readonly List<Vector3> s_Vertices = new List<Vector3>();

	private static readonly List<int> s_Triangles = new List<int>();

	private static Triangle<Vector3> GetTriangle(int index)
	{
		Vector3 index2 = s_Vertices[s_Triangles[3 * index]];
		Vector3 index3 = s_Vertices[s_Triangles[3 * index + 1]];
		Vector3 index4 = s_Vertices[s_Triangles[3 * index + 2]];
		return new Triangle<Vector3>(index2, index3, index4);
	}

	private static Plane GetTrianglePlane(Transform collider, int index)
	{
		return GetTrianglePlane(GetTriangle(index));
	}

	private static Plane GetTrianglePlane(Triangle<Vector3> source)
	{
		return new Plane(source.Index0, source.Index1, source.Index2);
	}

	private static bool InTriangle(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
	{
		Vector3 vector = b - a;
		Vector3 vector2 = c - a;
		Vector3 rhs = p - a;
		float num = Vector3.Dot(vector, vector);
		float num2 = Vector3.Dot(vector, vector2);
		float num3 = Vector3.Dot(vector, rhs);
		float num4 = Vector3.Dot(vector2, vector2);
		float num5 = Vector3.Dot(vector2, rhs);
		float num6 = 1f / (num * num4 - num2 * num2);
		float num7 = (num4 * num3 - num2 * num5) * num6;
		float num8 = (num * num5 - num2 * num3) * num6;
		if (num7 >= 0f && num8 >= 0f)
		{
			return num7 + num8 < 1f;
		}
		return false;
	}

	public static Vector3 FindClosestPoint(Collider collider, Vector3 position)
	{
		return FindClosestPoint(collider, position, ignoreVerticalTriangles: false);
	}

	public static Vector3 FindClosestPoint(Collider collider, Vector3 position, bool ignoreVerticalTriangles)
	{
		if (NonConvexJumpDebug.Active)
		{
			NonConvexJumpDebug.Reset();
		}
		if (collider is MeshCollider { convex: false, sharedMesh: var sharedMesh } meshCollider)
		{
			sharedMesh.GetVertices(s_Vertices);
			Vector3 position2 = Vector3.zero;
			float num = float.PositiveInfinity;
			Transform transform = meshCollider.transform;
			position = transform.InverseTransformPoint(position);
			Vector3 rhs = transform.InverseTransformDirection(Vector3.up);
			for (int i = 0; i < sharedMesh.subMeshCount; i++)
			{
				sharedMesh.GetTriangles(s_Triangles, i);
				int j = 0;
				for (int num2 = s_Triangles.Count / 3; j < num2; j++)
				{
					Triangle<Vector3> triangle = GetTriangle(j);
					Vector3 vector = Vector3.Normalize(Vector3.Cross(triangle.Index1 - triangle.Index0, triangle.Index2 - triangle.Index0));
					float num3 = 0f - Vector3.Dot(vector, triangle.Index0);
					if (ignoreVerticalTriangles && Mathf.Abs(Vector3.Dot(vector, rhs)) >= 0.9f)
					{
						continue;
					}
					float num4 = Vector3.Dot(vector, position) + num3;
					float num5 = Mathf.Abs(num4);
					if (!(num5 >= num))
					{
						Vector3 vector2 = position - vector * num4;
						bool flag = InTriangle(triangle.Index0, triangle.Index1, triangle.Index2, vector2);
						if (NonConvexJumpDebug.Active)
						{
							Triangle<Vector3> triangle2 = new Triangle<Vector3>(transform.TransformPoint(triangle.Index0), transform.TransformPoint(triangle.Index1), transform.TransformPoint(triangle.Index2));
							NonConvexJumpDebug.CreateTri(vector, triangle2, flag ? new Color(0f, 0f, 1f) : new Color(0f, 0f, Random.Range(0.1f, 0.4f)));
						}
						if (flag)
						{
							position2 = vector2;
							num = num5;
						}
					}
				}
			}
			position2 = transform.TransformPoint(position2);
			if (NonConvexJumpDebug.Active)
			{
				NonConvexJumpDebug.CreateBall(Color.green, position2);
			}
			return position2;
		}
		return collider.ClosestPoint(position);
	}
}
