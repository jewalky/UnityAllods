// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/MainShaderPaletted"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Palette("Sprite Palette", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Lightness("Lightness", Float) = 0.5
		[MaterialToggle] DoClip("Clipping enabled", Float) = 0
		ClipArea("Clipping area", Vector) = (0,0,0,0)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0

		// for UI
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
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

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		ColorMask[_ColorMask]

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
				float4 vertexS  : TEXCOORD2;
			};

			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.vertexS = ComputeScreenPos(OUT.vertex);
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
			float DoClip;
			float4 ClipArea;

			fixed4 frag(v2f IN) : COLOR
			{
				if (DoClip > 0)
				{
					float2 wcoord = (IN.vertexS.xy / IN.vertexS.w * _ScreenParams.xy);
					wcoord.y = _ScreenParams.y - wcoord.y;
					if (wcoord.x < ClipArea[0] ||
						wcoord.y < ClipArea[1] ||
						wcoord.x >= ClipArea[0] + ClipArea[2] ||
						wcoord.y >= ClipArea[1] + ClipArea[3]) discard;
				}

				fixed4 paletteMapColor = tex2D(_MainTex, IN.texcoord);

				// The alpha channel of the palette map points to UVs in the palette key.
				float paletteX = paletteMapColor.r / 256 * 255;
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
