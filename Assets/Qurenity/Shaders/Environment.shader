Shader "Qurenity/Environment"
{
	Properties
	{
	  _MainTex("Texture", 2D) = "white" {TexGen SphereMap}
	}

	SubShader
	{
	  SeparateSpecular On
		   Pass
		   {
			   Name "BASE"
			   ZWrite on
			   Blend One One
			   BindChannels {
			   Bind "Vertex", vertex
			   Bind "normal", normal
		   }

		  SetTexture[_MainTex]
		  {
			   combine texture
		  }
	   }
	}
	Fallback "Legacy Shaders/Transparent/Diffuse"
}
