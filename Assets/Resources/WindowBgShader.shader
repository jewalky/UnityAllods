﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/WindowBgShader"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Lightness("Lightness", Float) = 0.5
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

			half2 tuv = half2(1.0 / (_ScreenParams.xy));
			half4 col = half4(0, 0, 0, 0);
			for (int y = -1; y <= 1; y++)
			{
				for (int x = -1; x <= 1; x++)
				{
					half2 lpos = IN.texcoord + tuv.xy*half2(x, y);
					half4 lcol = tex2D(_MainTex, lpos);
					half len2 = length(IN.texcoord.xy - lpos) * _ScreenParams.xy;
					col += lcol * ((8-len2)/8) * 0.5;
				}
			}
				half4 texcol = col * IN.color;
				texcol.rgb *= _Lightness * 2;
				texcol.a = 1;
				return texcol;
			}
			ENDCG
		}
	}
	Fallback "Sprites/Default"
}
