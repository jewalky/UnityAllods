﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TerrainShader"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_LightTex("Light texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		[MaterialToggle] DoClip("Clipping enabled", Float) = 0
		ClipArea("Clipping area", Vector) = (0,0,0,0)
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
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
		ZTest Always

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
				float2 texcoord2 : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				half2 texcoord  : TEXCOORD0;
				half2 texcoord2 : TEXCOORD1;
				float4 vertexS  : TEXCOORD2; // we don't use this anyway
			};

			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.vertexS = ComputeScreenPos(OUT.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.texcoord2 = IN.texcoord2;
				OUT.color = IN.color * _Color;
#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
#endif

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _LightTex;
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

				half4 texcol = tex2D(_MainTex, IN.texcoord);
				half4 texcol2 = tex2D(_LightTex, IN.texcoord2);
				return texcol * half4(texcol2.a*2, texcol2.a*2, texcol2.a*2, 1) * IN.color;
			}
			ENDCG
		}
	}
	Fallback "Sprites/Default"
}
