Shader "Qurenity/Debug"
{
	Properties
	{
		[PerRenderData] _Color("Base (RGB)", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Lambert nodynlightmap

		struct Input
		{
		  float4 vertColor;
		};

		uniform float4 _Color;

		void surf(Input IN, inout SurfaceOutput o)
		{
			o.Albedo = _Color;
			o.Emission = o.Albedo;
		}
		ENDCG
	}
}