Shader "Qurenity/SkyHole"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}

	SubShader
	{
		Tags
		{
			"RenderType" = "Opaque"
			"LAV" = "Sky"
			"AutoMap" = "None"
		}

		CGPROGRAM
		#pragma surface surf Standard noambient nolightmap noshadow noforwardadd nodirlightmap novertexlights

		struct Input
		{
			float4 screenPos;
		};

		sampler2D _MainTex;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
			o.Albedo = tex2D(_MainTex, screenUV).rgb;
			o.Emission = o.Albedo;
		}
		ENDCG
	}
}