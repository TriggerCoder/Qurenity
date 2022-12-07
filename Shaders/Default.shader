Shader "Qurenity/Default"
{
	Properties
	{
		[PerRenderData] _MainTex("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert nodynlightmap
		struct Input
		{
		  float2 uv_MainTex;
		  float4 vertColor;
		};

		sampler2D _MainTex;

		void vert(inout appdata_full v, out Input o) {
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.vertColor = v.color;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			half4 tex = tex2D(_MainTex, IN.uv_MainTex);
			clip(tex.a - 0.5);

			o.Albedo = tex.rgb * IN.vertColor.rgb;
			o.Emission = o.Albedo * unity_AmbientSky;
			o.Alpha = tex.a;
		}
		ENDCG
	}
}