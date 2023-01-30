Shader "Qurenity/SkyHole"
{
	Properties
	{
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	}
		SubShader
	{
		Tags{ "Queue" = "Geometry-10" }

		ColorMask 0
		ZWrite On

		Pass{}
	}
}