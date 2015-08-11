Shader "FX/Ripple GrabPass"
{
	Properties
	{
		//_MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		_DistortTex("Distortion Map", 2D) = "white"
		_Intensity("Intensity (XY) and Scroll (ZW)", Vector) = (0.1, 0.1, 1, 1)
	}

	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent+1000" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha

		GrabPass{ "_WaterGrab" }

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				half2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				half2 texcoord  : TEXCOORD0;
				fixed4 color    : COLOR;
				half2 screenuv  : TEXCOORD1;
				half2 maskuv    : TEXCOORD2;
			};
			
			fixed4 _Color;
			sampler2D _WaterGrab;
			sampler2D _DistortTex;
			float4 _Intensity;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.texcoord += _Time.g * _Intensity.zw;
				OUT.color = IN.color * _Color;

				half4 screen = ComputeGrabScreenPos(OUT.vertex);
				OUT.screenuv = screen.xy / screen.w;

				OUT.maskuv = IN.texcoord;

				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				half2 distort = tex2D(_DistortTex, IN.texcoord).rg;
				half mask = tex2D(_DistortTex, IN.maskuv).b;
				half2 offset = (distort * 2 - 1) * _Intensity * mask;

				fixed4 grab = tex2D(_WaterGrab, IN.screenuv + offset) * IN.color;
				//grab.a = IN.color.a;
				return grab;
			}
		ENDCG
		}
	}
}
