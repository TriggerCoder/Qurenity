using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExtensionMethods
{
	public static class MatrixExtensions
	{
		//https://gamedev.stackexchange.com/questions/203073/how-to-convert-a-4x4-matrix-transformation-to-another-coordinate-system
		//We want to map +x to -x (-1 ,  0 ,  0)
		//We want to map +y to +z ( 0 ,  0 , -1)
		//We want to map +z to -y ( 0 , -1 ,  0)
		//We want the fourth, homogenous coordinate to survive unchanged (0, 0, 0, 1)
		//If we left-multiply this matrix by any homogeneous vector in our old coordinate system,
		//it converts it to the corresponding vector in the new coordinate system:
		//Vnew = T*Vold
		public static Matrix4x4 QuakeToUnityConversion(this Matrix4x4 matrix)
		{
			Vector4 column0 = new Vector4(-1, 0, 0, 0);
			Vector4 column1 = new Vector4(0, 0, -1, 0);
			Vector4 column2 = new Vector4(0, 1, 0, 0);
			Vector4 column3 = new Vector4(0, 0, 0, 1);

			return new Matrix4x4(column0, column1, column2, column3);
		}

		//http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
		public static Quaternion ExtractRotation(this Matrix4x4 matrix)
		{
			Quaternion rotation = new Quaternion();
			rotation.w = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 + matrix.m11 + matrix.m22)) / 2;
			rotation.x = Mathf.Sqrt(Mathf.Max(0, 1 + matrix.m00 - matrix.m11 - matrix.m22)) / 2;
			rotation.y = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 + matrix.m11 - matrix.m22)) / 2;
			rotation.z = Mathf.Sqrt(Mathf.Max(0, 1 - matrix.m00 - matrix.m11 + matrix.m22)) / 2;
			rotation.x *= Mathf.Sign(rotation.x * (matrix.m21 - matrix.m12));
			rotation.y *= Mathf.Sign(rotation.y * (matrix.m02 - matrix.m20));
			rotation.z *= Mathf.Sign(rotation.z * (matrix.m10 - matrix.m01));

			return rotation;
		}
		public static Vector3 ExtractPosition(this Matrix4x4 matrix)
		{
			Vector3 position;
			position.x = matrix.m03;
			position.y = matrix.m13;
			position.z = matrix.m23;
			return position;
		}

		public static Vector3 ExtractScale(this Matrix4x4 matrix)
		{
			Vector3 scale;
			scale.x = new Vector4(matrix.m00, matrix.m10, matrix.m20, matrix.m30).magnitude;
			scale.y = new Vector4(matrix.m01, matrix.m11, matrix.m21, matrix.m31).magnitude;
			scale.z = new Vector4(matrix.m02, matrix.m12, matrix.m22, matrix.m32).magnitude;
			return scale;
		}
	}
}