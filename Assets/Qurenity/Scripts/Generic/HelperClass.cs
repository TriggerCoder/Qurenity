using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public struct Vector2Half : INetworkSerializable
{
	public ushort x;
	public ushort y;
	public Vector2Half(Vector2 vec2)
	{
		x = Mathf.FloatToHalf(vec2.x);
		y = Mathf.FloatToHalf(vec2.y);
	}
	public void Set(Vector2 vec2)
	{
		x = Mathf.FloatToHalf(vec2.x);
		y = Mathf.FloatToHalf(vec2.y);
	}
	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref x);
		serializer.SerializeValue(ref y);
	}
}

public struct Vector3Half : INetworkSerializable
{
	public ushort x;
	public ushort y;
	public ushort z;
	public Vector3Half(Vector3 vec3)
	{
		x = Mathf.FloatToHalf(vec3.x);
		y = Mathf.FloatToHalf(vec3.y);
		z = Mathf.FloatToHalf(vec3.z);
	}
	public void Set(Vector3 vec3)
	{
		x = Mathf.FloatToHalf(vec3.x);
		y = Mathf.FloatToHalf(vec3.y);
		z = Mathf.FloatToHalf(vec3.z);
	}
	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref x);
		serializer.SerializeValue(ref y);
		serializer.SerializeValue(ref z);
	}
}