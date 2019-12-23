Shader "Custom/OctahedralImposter"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
		_Tiles("Tiles", Int) = 6
		[Toggle] _Debug("Debug", Float) = 0
	}

	SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#pragma shader_feature _DEBUG_ON

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 uvDir : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			int _Tiles;

			fixed4 clerp(fixed4 a, fixed4 b, float ratio)
			{
				a.rgb = lerp(b.rgb, a.rgb, a.a);
				b.rgb = lerp(a.rgb, b.rgb, b.a);
				return lerp(a, b, ratio);
			}

			float2x2 rotate(float angle)
			{
				float s = sin(angle), c = cos(angle);
				return float2x2(c, s, -s, c);
			}

			v2f vert(appdata v)
			{
				v2f o;

				// Billboard
				float3 viewPos = UnityObjectToViewPos(float3(0, 0, 0));
				float viewUpInverse = lerp(-1, 1, (0 <= UNITY_MATRIX_V._m11));
				float3 scaleRotatePos = mul((float3x3)unity_ObjectToWorld, v.vertex * viewUpInverse);
				viewPos += float3(scaleRotatePos.xy, -scaleRotatePos.z);
				o.vertex = mul(UNITY_MATRIX_P, float4(viewPos, 1));
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				float3 camDir = -UNITY_MATRIX_V._m20_m21_m22;
				float3 viewDir = unity_ObjectToWorld._m03_m13_m23 - _WorldSpaceCameraPos.xyz;
				//viewDir = normalize(viewDir);
				viewDir = normalize(viewDir + camDir * dot(camDir, viewDir)); // about..

				float oneFrameRatio = 1.0f / max(max(abs(viewDir.x), abs(viewDir.z)), 1e-5);
				float maxMagnitude = max(length(viewDir.xz * oneFrameRatio), 1.0);
				viewDir.y = sin(viewDir.y * UNITY_PI * 0.5f); // about..
				viewDir = normalize(viewDir);
				o.uvDir = viewDir * maxMagnitude;

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float tiles = _Tiles + 1;
				float2 uvDir = float2(i.uvDir.x, -i.uvDir.z);
				float2 uv = (float2(0.5, 0.5) - uvDir * 0.5);
				int2   gridID = (int2)floor(uv * tiles);
				float2 gridPos = frac(uv * tiles);

				int gridCorner = (gridID.x == gridID.y) || ((gridID.x + gridID.y) == (tiles - 1));
				int which = abs(uvDir.x) < abs(uvDir.y);
				float  gridSign = lerp(sign(uvDir.x), sign(uvDir.y), which);
				float2 gridH = lerp(float2(gridSign, 0.0), float2(0.0, gridSign), (float)which); // horizontal
				float2 gridV = lerp(float2(0.0, gridSign), float2(gridSign, 0.0), (float)which); // vertical
				gridV = lerp(gridV, sign(uvDir), (float)gridCorner);

				float gridRatioH = distance(float2(0.5, 0.5) * gridH, gridPos * gridH); // 0で余計な軸成分を消す
				float gridRatioV = distance(float2(0.5, 0.5) * gridV, gridPos * gridV); // 0で余計な軸成分を消す
				float2 gridRatio = (float2(0.5, 0.5) - gridPos) * 2.0; // -1 .. 1

				gridH *= sign(gridRatio) * -gridSign;
				gridV *= sign(gridRatio) * -gridSign;

				float2 viewUp = UNITY_MATRIX_V._m10_m12 * lerp(-1, 1, (0 <= UNITY_MATRIX_V._m11));
				float uvRot = clamp(atan2(viewUp.x, viewUp.y), -UNITY_PI, UNITY_PI);
				float2 uvOrg = i.uv;
				float2 uvOrgRot = mul(rotate(uvRot), i.uv - 0.5) + 0.5;

				int2 gridIDC = gridID;
				int2 gridIDH = gridID + gridH;
				int2 gridIDV = min(abs(gridID + gridV), _Tiles);
				int gridCenter = 1;
				int enableRotate = (_Tiles % 2 == 0);

				gridCenter = (gridIDC.x == _Tiles / 2) && (gridIDC.x == gridIDC.y);
				float2 uvC = lerp(uvOrg, uvOrgRot, enableRotate * gridCenter) + gridIDC;
				gridCenter = (gridIDH.x == _Tiles / 2) && (gridIDH.x == gridIDH.y);
				float2 uvH = lerp(uvOrg, uvOrgRot, enableRotate * gridCenter) + gridIDH;
				gridCenter = (gridIDV.x == _Tiles / 2) && (gridIDV.x == gridIDV.y);
				float2 uvV = lerp(uvOrg, uvOrgRot, enableRotate * gridCenter) + gridIDV;

				fixed4 color = fixed4(0,0,0,0);
				fixed4 colorC = tex2D(_MainTex, uvC / tiles); // current
				fixed4 colorH = tex2D(_MainTex, uvH / tiles); // horizontal
				fixed4 colorV = tex2D(_MainTex, uvV / tiles); // vertical

				float4 colorMixH = clerp(colorC, colorH, gridRatioH);
				float4 colorMixV = clerp(colorC, colorV, gridRatioV);
				color = lerp(colorMixH, colorMixV, colorMixH.a < colorMixV.a);

#ifdef _DEBUG_ON
				// debug
				color = clerp(colorC, tex2D(_MainTex, i.uv), 0.5);
				color = lerp(color, float4(1, 0, 0, 1), step(distance(i.uv, uv), 0.01));
#endif
				
				return color;
			}
			ENDCG
		}
	}
}
