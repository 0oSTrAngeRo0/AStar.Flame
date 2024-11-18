Shader "Custom/WeightPaint" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_BoneIndex ("BoneIndex", Range(0, 4)) = 0
	}
	SubShader {
		Tags {
			"RenderType"="Opaque"
		}
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			int _BoneIndex;

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 weights : BLENDWEIGHTS;
				int4 bones : BLENDINDICES;
			};

			struct v2f
			{
				float4 position : SV_POSITION;
				float4 color : COLOR;
			};

			float GetBoneWeights(in float4 weights, in int4 indices, int target)
			{
				float weight = 0.0f;
				for (int i = 0; i < 4; i++)
				{
					if (indices[i] != target) continue;
					weight = weights[i];
				}
				return weight;
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.position = UnityObjectToClipPos(v.vertex);

				// 可视化骨骼权重，简单映射到颜色
				const float weight = GetBoneWeights(v.weights, v.bones, _BoneIndex);
				o.color = float4(weight, weight, weight, 1); // 将权重和映射为红色

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color; // 返回权重颜色
			}
			ENDCG
		}
	}
}