Shader "Custom/MainShaderPaletted"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Palette("Sprite Palette", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Lightness("Lightness", Float) = 0.5
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 1
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Fog{ Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha
		ZTest LEqual

		Pass
		{
			CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma multi_compile DUMMY PIXELSNAP_ON
#include "UnityCG.cginc"

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				half2 texcoord  : TEXCOORD0;
			};

			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
#endif

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _Palette;
			float _Lightness;

			fixed4 frag(v2f IN) : COLOR
			{
				fixed4 paletteMapColor = tex2D(_MainTex, IN.texcoord);

				// The alpha channel of the palette map points to UVs in the palette key.
				float paletteX = paletteMapColor.r;
				float2 paletteUV = float2(paletteX, 0.0f);
				// Get the palette's UV accounting for texture tiling and offset
				float2 paletteUVTransformed = paletteUV;// TRANSFORM_TEX(paletteUV, _Palette);

				// Get the color from the palette key
				fixed4 outColor = fixed4(tex2D(_Palette, paletteUVTransformed).rgb, paletteMapColor.g);

				// Apply the tint to the final color
				outColor *= IN.color;
				outColor *= float4(_Lightness * 2, _Lightness * 2, _Lightness * 2, 1);
				return outColor;
			}
			ENDCG
		}
	}
	Fallback "Sprites/Default"
}
