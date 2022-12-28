Shader "Qurenity/DefaultLightmapped"
{
	Properties
	{
		[PerRenderData]_MainTex("Base (RGB)", 2D) = "white" {}
		[PerRenderData]_LightMap("Lightmap (RGB)", 2D) = "black" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert nodynlightmap
		struct Input
		{
		  float2 uv_MainTex;
		  float2 uv2_LightMap;
		  float4 vertColor;
		};

		sampler2D _MainTex;
		sampler2D _LightMap;
	
		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vertColor = v.color;
		}
	
		void surf(Input IN, inout SurfaceOutput o)
		{
			half4 tex = tex2D(_MainTex, IN.uv_MainTex);
			half4 lm = tex2D(_LightMap, IN.uv2_LightMap);

//			o.Albedo = IN.vertColor.rgb;
			o.Albedo = tex.rgb * IN.vertColor.rgb;
//			o.Emission = o.Albedo * unity_AmbientSky;
			o.Emission = lm.rgb * tex.rgb;
			o.Alpha = tex.a;
		}
		ENDCG
	}
}