// Unlit shader. Simplest possible textured shader.
// - SUPPORTS lightmap
// - no lighting
// - no per-material color

Shader "Custom/Dimmable Unlit (Supports Lightmap)" 
{
	Properties 
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		// Non-lightmapped
		Pass 
		{
			Tags { "LightMode" = "Vertex" }
			Lighting Off
			ZWrite On
			Cull Back

			SetTexture [_MainTex] 
			{
				constantColor[_Color]
				combine texture * constant
			}
		}
	}
}



