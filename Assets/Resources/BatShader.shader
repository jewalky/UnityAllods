Shader "Custom/BatShader"
{
	Properties
	{
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

		GrabPass { "_GrabTexture" }

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
				float4 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord  : TEXCOORD0;
				float2 texcoordG  : TEXCOORD1;
				float4 vertexS  : TEXCOORD2;
			};

			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.vertexS = ComputeScreenPos(OUT.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.texcoordG = ComputeGrabScreenPos(OUT.vertex);
				OUT.color = IN.color * _Color;
#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
#endif

				return OUT;
			}

			sampler2D _GrabTexture;
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

				// IN.texcoord.xy = x/y from 0 to 1
				float PI = 3.1415926535;

				float aperture = 178;
				float apertureHalf = 0.5 * aperture * (PI / 180.0);
				float maxFactor = sin(apertureHalf);

				float2 uv;
				float2 xy = 2.0 * IN.texcoord.xy - 1.0;
				float d = length(xy);
				if (d < (2.0 - maxFactor))
				{
					d = length(xy * maxFactor);
					float z = sqrt(1.0 - d * d);
					float r = atan2(d, z) / PI;
					float phi = atan2(xy.y, xy.x);

					uv.x = r * cos(phi);
					uv.y = r * sin(phi);
				}
				else
				{
					//uv = IN.texcoord;
					discard;
				}
				
				half4 texcol = half4(tex2D(_GrabTexture, IN.texcoordG + uv / _ScreenParams.xy * 16).rgb, 1);
				return texcol;
			}
			ENDCG
		}
	}
	Fallback "Sprites/Default"
}
